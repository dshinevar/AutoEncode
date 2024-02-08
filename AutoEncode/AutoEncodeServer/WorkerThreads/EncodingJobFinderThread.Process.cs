using AutoEncodeServer.Data;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.WorkerThreads
{
    public partial class EncodingJobFinderThread
    {
        private readonly object _lock = new();

        private readonly ManualResetEvent _buildingSourceFilesEvent = new(false);
        private Dictionary<string, SearchDirectory> SearchDirectories { get; set; }
        private ConcurrentDictionary<string, (bool IsShows, List<SourceFileData> Files)> SourceFiles { get; set; } = new();
        private Dictionary<Guid, (string Directory, SourceFileData SourceFiles)> SourceFilesByGuid { get; set; }

        protected void ThreadLoop(object data)
        {
            CancellationToken shutdownToken = data is CancellationToken token ? token : throw new Exception("ThreadLoop not given CancellationToken.");

            while (shutdownToken.IsCancellationRequested is false)
            {
                try
                {
                    if (_directoryUpdate) UpdateSearchDirectories(SearchDirectories);

                    // Don't do anything if the queue is full
                    if (EncodingJobManager.Count < State.GlobalJobSettings.MaxNumberOfJobsInQueue)
                    {
                        BuildSourceFiles();

                        shutdownToken.ThrowIfCancellationRequested();

                        // Add encoding jobs for automated search directories and files not encoded
                        foreach (KeyValuePair<string, (bool IsShows, List<SourceFileData> Files)> entry in SourceFiles)
                        {
                            if (SearchDirectories[entry.Key].Automated is true)
                            {
                                List<SourceFileData> filesToEncode = entry.Value.Files.Where(x => x.Encoded is false).ToList();
                                filesToEncode.ForEach(x => CreateEncodingJob(x, SearchDirectories[entry.Key], shutdownToken));
                            }
                        }
                    }

                    Sleep();
                }
                catch (OperationCanceledException)
                {
                    return; // If cancelled, just end loop
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Error during looking for encoding jobs. Thread stopping.", ThreadName,
                        details: new { EncodingJobQueueCount = EncodingJobManager.Count, SearchDirectoriesCount = SearchDirectories.Count });
                    return;
                }
            }
        }

        #region PRIVATE FUNCTIONS
        private void UpdateSearchDirectories(Dictionary<string, SearchDirectory> searchDirectories)
        {
            searchDirectories = State.Directories.ToDictionary(x => x.Key, x => x.Value.DeepClone());

            // Remove any old directories (keys) in source files
            List<string> deleteKeys = SourceFiles.Keys.Except(searchDirectories.Keys).ToList();
            deleteKeys.ForEach(x => SourceFiles.Remove(x, out _));

            _directoryUpdate = false;
        }

        /// <summary> Builds out SourceFiles from the search directories </summary>
        private void BuildSourceFiles()
        {
            _buildingSourceFilesEvent.Reset();

            // Clear Guid lookup
            SourceFilesByGuid = null;

            Parallel.ForEach(SearchDirectories, entry =>
            {
                try
                {
                    if (Directory.Exists(entry.Value.Source))
                    {
                        IEnumerable<string> sourceFilePaths = Directory.GetFiles(entry.Value.Source, "*.*", SearchOption.AllDirectories)
                            .Where(file => ValidSourceFile(file));
                        HashSet<string> destinationFiles = Directory.GetFiles(entry.Value.Destination, "*.*", SearchOption.AllDirectories)
                            .Where(file => State.JobFinderSettings.VideoFileExtensions.Any(file.ToLower().EndsWith))
                            .Select(file => Path.GetFileNameWithoutExtension(file))
                            .ToHashSet();

                        // Ensure we find any source files
                        if (sourceFilePaths.Any())
                        {
                            List<SourceFile> sourceFiles = [];

                            foreach (string sourceFilePath in sourceFilePaths)
                            {
                                if (File.Exists(sourceFilePath) is false) continue;

                                SourceFile sourceFile = new()
                                {
                                    FullPath = sourceFilePath,
                                    DestinationFullPath = sourceFilePath.Replace(entry.Value.Source, entry.Value.Destination),
                                    Encoded = destinationFiles.Contains(Path.GetFileNameWithoutExtension(sourceFilePath)) &&
                                                (EncodingJobManager.IsEncodingByFileName(Path.GetFileName(sourceFilePath)) is false)
                                };

                                sourceFiles.Add(sourceFile);
                            }

                            if (SourceFiles.ContainsKey(entry.Key) is false)
                            {
                                SourceFiles[entry.Key] = (entry.Value.TVShowStructure, new List<SourceFileData>());
                            }

                            var sourceFilesForDirectory = SourceFiles[entry.Key];
                            UpdateSourceFiles(ref sourceFilesForDirectory, sourceFiles);
                        }
                    }
                    else
                    {
                        Logger.LogError($"{entry.Value.Source} does not exist.", ThreadName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error building source files for source directory {entry.Key}", ThreadName,
                        new { SourceDirName = entry.Key, entry.Value.Source, entry.Value.Destination, entry.Value.Automated, entry.Value.TVShowStructure });
                    return;
                }
            });

            // Rebuild the guid lookup
            SourceFilesByGuid = SourceFiles.SelectMany(kvp => kvp.Value.Files, (kvp, file) => (kvp.Key, file)).ToDictionary(f => f.file.Guid);

            _buildingSourceFilesEvent.Set();
        }

        private bool CreateEncodingJob(SourceFileData sourceFileData, SearchDirectory searchDirectory)
        {
            bool jobCreated = false;
            try
            {
                lock (_lock)
                {
                    // Don't create encoding job if we are at max count
                    if (EncodingJobManager.Count < State.GlobalJobSettings.MaxNumberOfJobsInQueue)
                    {
                        // Only add encoding job if file is ready.
                        if (CheckFileReady(sourceFileData.FullPath))
                        {
                            PostProcessingSettings searchDirectoryPostProcessingSettings = searchDirectory.PostProcessing;
                            // Prep Data for creating job
                            List<string> updatedCopyFilePaths = null;
                            if ((searchDirectoryPostProcessingSettings?.CopyFilePaths?.Count ?? -1) > 0 is true)
                            {
                                // Update copy file paths with full destination directory (for extras and shows with subdirectories)
                                updatedCopyFilePaths = [];
                                foreach (string oldPath in searchDirectoryPostProcessingSettings.CopyFilePaths)
                                {
                                    updatedCopyFilePaths.Add($"{oldPath}{sourceFileData.FullPath.Replace(searchDirectory.Source, "")}");
                                }
                            }

                            PostProcessingSettings updatedPostProcessingSettings = new()
                            {
                                CopyFilePaths = updatedCopyFilePaths,
                                DeleteSourceFile = searchDirectoryPostProcessingSettings?.DeleteSourceFile ?? false
                            };

                            ulong? newJobId = EncodingJobManager.CreateEncodingJob(sourceFileData, updatedPostProcessingSettings);
                            if (newJobId is not null)
                            {
                                jobCreated = true;
                                Logger.LogInfo($"(JobID: {newJobId}) {sourceFileData.FileName} added to encoding job queue.", ThreadName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error creating encoding job for {sourceFileData.FileName}.", ThreadName, new { sourceFileData });
            }

            return jobCreated;
        }

        private bool CreateEncodingJob(SourceFileData sourceFileData, SearchDirectory searchDirectory, CancellationToken shutdownToken)
        {
            shutdownToken.ThrowIfCancellationRequested();
            return CreateEncodingJob(sourceFileData, searchDirectory);
        }

        /// <summary> Checks if a file is valid for being considered a source file.
        /// <para>Currently 2 checks:</para>
        /// <para>1. Valid file extension</para>
        /// <para>2. File doesn't contain the secondary skip extension</para>
        /// </summary>
        /// <param name="filePath">Path of the file</param>
        /// <returns>True if valid; False, otherwise</returns>
        private bool ValidSourceFile(string filePath)
        {
            // Check if it's an allowed file extension
            bool valid = State.JobFinderSettings.VideoFileExtensions.Any(filePath.ToLower().EndsWith);

            // If valid and the config has a secondary skip extension
            if (valid is true && string.IsNullOrWhiteSpace(State.JobFinderSettings.SecondarySkipExtension) is false)
            {
                string fileSecondExtension = Path.GetExtension(Path.GetFileNameWithoutExtension(filePath)).Trim('.');

                // If there even is a secondary extension, check if it's the skip extension
                if (string.IsNullOrWhiteSpace(fileSecondExtension) is false)
                {
                    // File is NOT valid if the extension equals the skip extension
                    valid &= !fileSecondExtension.Equals(State.JobFinderSettings.SecondarySkipExtension, StringComparison.OrdinalIgnoreCase);
                }
            }

            return valid;
        }

        /// <summary>Check if file size is changing.</summary>
        /// <param name="filePath"></param>
        /// <returns>True if file is ready; False, otherwise</returns>
        private static bool CheckFileReady(string filePath)
        {
            List<long> fileSizes = [];
            FileInfo fileInfo = new(filePath);
            fileSizes.Add(fileInfo.Length);

            Thread.Sleep(TimeSpan.FromSeconds(4));

            // If still able to access, check to see if file size is changing
            fileInfo = new(filePath);
            fileSizes.Add(fileInfo.Length);

            Thread.Sleep(TimeSpan.FromSeconds(4));

            fileInfo = new(filePath);
            fileSizes.Add(fileInfo.Length);

            return fileSizes.All(x => x.Equals(fileSizes.First()));
        }
        #endregion PRIVATE FUNCTIONS

        private void UpdateSourceFiles(ref (bool IsShows, List<SourceFileData> Files) sourceFiles, IEnumerable<SourceFile> newSourceFiles)
        {
            bool isShows = sourceFiles.IsShows;

            IEnumerable<SourceFileData> sourceFilesToRemove = sourceFiles.Files.Except(newSourceFiles, (s, n) => string.Equals(s.FullPath, n.FullPath, StringComparison.OrdinalIgnoreCase));
            sourceFiles.Files.RemoveRange(sourceFilesToRemove);

            IEnumerable<SourceFileData> sourceFilesToAdd = newSourceFiles.Except(sourceFiles.Files, (n, s) => string.Equals(n.FullPath, s.FullPath, StringComparison.OrdinalIgnoreCase))
                .Select(x => isShows is true ? new ShowSourceFileData(x) : new SourceFileData(x));
            sourceFiles.Files.AddRange(sourceFilesToAdd);

            Debug.Assert(sourceFiles.Files.Count == newSourceFiles.Count(), "Number of incoming source files should match outgoing number of source files.");

            sourceFiles.Files.Sort(SourceFileData.CompareByFileName);
        }
    }
}
