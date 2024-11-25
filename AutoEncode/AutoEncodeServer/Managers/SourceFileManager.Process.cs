using AutoEncodeServer.Communication;
using AutoEncodeServer.Data;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeServer.Models;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.Managers;

// PROCESS
public partial class SourceFileManager : ISourceFileManager
{
    private readonly ManualResetEvent _sleepMRE = new(false);
    private readonly ManualResetEvent _updatingSourceFilesMRE = new(false);

    private Dictionary<string, SearchDirectory> _searchDirectories;

    private readonly ConcurrentDictionary<Guid, ISourceFileModel> _sourceFiles = [];

    private readonly ConcurrentBag<ISourceFileModel> _potentialNewEncodingJobs = [];

    protected override void Process()
    {
        CancellationToken shutdownToken = ShutdownCancellationTokenSource.Token;
        _potentialNewEncodingJobs.Clear();

        while (shutdownToken.IsCancellationRequested is false)
        {
            try
            {
                IEnumerable<SourceFile> foundSourceFiles = BuildSourceFiles();

                _updatingSourceFilesMRE.Reset();
                IEnumerable<SourceFileUpdateData> sourceFileUpdates = UpdateSourceFiles(foundSourceFiles);
                _updatingSourceFilesMRE.Set();

                shutdownToken.ThrowIfCancellationRequested();

                // Send out updates if any
                if (sourceFileUpdates.Any())
                {
                    (string topic, CommunicationMessage<ClientUpdateType> message) = ClientUpdateMessageFactory.CreateSourceFileUpdate(sourceFileUpdates);
                    ClientUpdatePublisher.AddClientUpdateRequest(topic, message);
                }

                // Add request to encode all potential encoding jobs
                foreach (ISourceFileModel sourceFile in _potentialNewEncodingJobs.OrderBy(sf => sf.FullPath))
                {
                    _encodingJobManager.AddCreateEncodingJobRequest(sourceFile);
                }
                _potentialNewEncodingJobs.Clear();
            }
            catch (OperationCanceledException)
            {
                return; // If cancelled, just end
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Exception occurred during building source files / looking for files to encode.", nameof(SourceFileManager), new { SearchDirectoriesCount = _searchDirectories.Count });
                _updatingSourceFilesMRE.Set();  // Go ahead and set this if an error occurred so other processes can proceed
            }

            Sleep();
        }
    }

    /// <summary>Finds / builds all source files</summary>
    /// <returns>Returns list of <see cref="SourceFile"/></returns>
    private IEnumerable<SourceFile> BuildSourceFiles()
    {
        ConcurrentBag<SourceFile> foundSourceFiles = [];

        Parallel.ForEach(_searchDirectories,
            new ParallelOptions() { MaxDegreeOfParallelism = 4, CancellationToken = ShutdownCancellationTokenSource.Token },
            entry =>
        {
            try
            {
                string searchDirectoryName = entry.Key;
                SearchDirectory searchDirectory = entry.Value;
                if (Directory.Exists(searchDirectory.Source) is true)
                {
                    IEnumerable<string> sourceFilePaths = Directory.EnumerateFiles(entry.Value.Source, "*.*", SearchOption.AllDirectories)
                        .Where(file => ValidSourceFile(file));
                    HashSet<string> destinationFiles = Directory.EnumerateFiles(entry.Value.Destination, "*.*", SearchOption.AllDirectories)
                        .Where(file => State.VideoFileExtensions.Any(file.ToLower().EndsWith))
                        .Select(file => Path.GetFileNameWithoutExtension(file))
                        .ToHashSet();

                    if (sourceFilePaths.Any() is true)
                    {
                        foreach (string sourceFilePath in sourceFilePaths)
                        {
                            if (File.Exists(sourceFilePath) is false) continue;

                            SourceFile sourceFile = new()
                            {
                                FullPath = sourceFilePath,
                                DestinationFullPath = sourceFilePath.Replace(entry.Value.Source, entry.Value.Destination),
                                SearchDirectoryName = searchDirectoryName,
                                SourceDirectory = searchDirectory.Source,
                                IsEpisode = searchDirectory.EpisodeNaming
                            };
                            sourceFile.EncodingStatus = DetermineSourceFileEncodingStatus(sourceFile.Filename, destinationFiles);

                            foundSourceFiles.Add(sourceFile);
                        }
                    }
                }
                else
                {
                    Logger.LogError($"{searchDirectory.Source} does not exist.", nameof(SourceFileManager));
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error building source files for source directory {entry.Key}", nameof(SourceFileManager),
                    new { DirectoryName = entry.Key, DirectoryDetails = entry.Value });
                return;
            }
        });

        return foundSourceFiles;
    }

    private IEnumerable<SourceFileUpdateData> UpdateSourceFiles(IEnumerable<SourceFile> newSourceFiles)
    {
        List<SourceFileUpdateData> sourceFileUpdates = [];

        // Remove Source Files
        IEnumerable<ISourceFileModel> sourceFilesToRemove = _sourceFiles.Values.Except(newSourceFiles, (s, n) => string.Equals(s.FullPath, n.FullPath, StringComparison.OrdinalIgnoreCase));
        foreach (ISourceFileModel sourceFile in sourceFilesToRemove)
        {
            if (_sourceFiles.Remove(sourceFile.Guid, out _))
            {
                sourceFileUpdates.Add(new(SourceFileUpdateType.Remove, sourceFile.ToData()));
                SourceFileModelFactory.Release(sourceFile);
            }           
        }

        // Update / Add from new
        foreach (SourceFile newSourceFile in newSourceFiles)
        {
            ISourceFileModel model = _sourceFiles.Values.FirstOrDefault(f => f.FullPath.Equals(newSourceFile.FullPath));
            if (model is not null)
            {
                if (model.UpdateEncodingStatus(newSourceFile.EncodingStatus) is true)
                {
                    sourceFileUpdates.Add(new(SourceFileUpdateType.Update, model.ToData()));
                }
            }
            else
            {
                try
                {
                    model = SourceFileModelFactory.Create(newSourceFile);
                    if (_sourceFiles.TryAdd(model.Guid, model) is true)
                    {
                        sourceFileUpdates.Add(new(SourceFileUpdateType.Add, model.ToData()));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error creating {nameof(SourceFileModel)} for {newSourceFile.FullPath}", nameof(SourceFileManager), new { newSourceFile });
                }
            }

            // If it's an automated directory and the file is not encoded, add to potential new encoding jobs
            if ((_searchDirectories.TryGetValue(newSourceFile.SearchDirectoryName, out SearchDirectory searchDirectory) is true) &&
                (searchDirectory.Automated is true) &&
                (model.EncodingStatus is SourceFileEncodingStatus.NOT_ENCODED))
            {
                _potentialNewEncodingJobs.Add(model);
            }
        }

        return sourceFileUpdates;
    }

    /// <summary> Checks if a file is valid for being considered a source file.
    /// <para>Currently 2 checks:</para>
    /// <para>1. Valid file extension</para>
    /// <para>2. File doesn't contain the secondary skip extension</para>
    /// </summary>
    /// <param name="filePath">Path of the file</param>
    /// <returns>True if valid; False, otherwise</returns>
    private static bool ValidSourceFile(string filePath)
    {
        // Check if it's an allowed file extension
        bool valid = State.VideoFileExtensions.Any(e => filePath.EndsWith(e, StringComparison.OrdinalIgnoreCase));

        // If valid and the config has a secondary skip extension
        if (valid is true && string.IsNullOrWhiteSpace(State.SecondarySkipExtension) is false)
        {
            string fileSecondExtension = Path.GetExtension(Path.GetFileNameWithoutExtension(filePath)).Trim('.');

            // If there even is a secondary extension, check if it's the skip extension
            if (string.IsNullOrWhiteSpace(fileSecondExtension) is false)
            {
                // File is NOT valid if the extension equals the skip extension
                valid &= !fileSecondExtension.Equals(State.SecondarySkipExtension, StringComparison.OrdinalIgnoreCase);
            }
        }

        return valid;
    }

    private SourceFileEncodingStatus DetermineSourceFileEncodingStatus(string sourceFileFilename, HashSet<string> destinationFiles)
    {
        EncodingJobStatus? encodingJobStatus = _encodingJobManager.GetEncodingJobStatusByFileName(sourceFileFilename);
        bool destinationFileExists = destinationFiles.Contains(Path.GetFileNameWithoutExtension(sourceFileFilename));

        if (destinationFileExists)
        {
            if (encodingJobStatus.HasValue)
            {
                return TranslateEncodingJobStatusToSourceFileEncodingStatus(encodingJobStatus.Value);
            }
            else
            {
                return SourceFileEncodingStatus.ENCODED;
            }
        }
        else
        {
            return SourceFileEncodingStatus.NOT_ENCODED;
        }
    }

    private static SourceFileEncodingStatus TranslateEncodingJobStatusToSourceFileEncodingStatus(EncodingJobStatus encodingJobStatus)
    {
        if (encodingJobStatus >= EncodingJobStatus.ENCODED)
        {
            return SourceFileEncodingStatus.ENCODED;
        }
        else
        {
            return SourceFileEncodingStatus.IN_QUEUE;
        }
    }

    private void Wake() => _sleepMRE.Set();

    private void Sleep()
    {
        if (ShutdownCancellationTokenSource.IsCancellationRequested is false)
        {
            _sleepMRE.Reset();
            _sleepMRE.WaitOne(TimeSpan.FromMinutes(1));
        }
    }
}
