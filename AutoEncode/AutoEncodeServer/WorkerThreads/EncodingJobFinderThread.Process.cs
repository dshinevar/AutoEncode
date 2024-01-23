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

        private readonly AutoResetEvent _buildingSourceFilesEvent = new(false);
        private Dictionary<string, SearchDirectory> SearchDirectories { get; set; }
        private ConcurrentDictionary<string, List<SourceFileData>> MovieSourceFiles { get; set; } = new();
        private ConcurrentDictionary<string, List<ShowSourceFileData>> ShowSourceFiles { get; set; } = new();

        protected void ThreadLoop(object data)
        {
            CancellationToken shutdownToken = data is CancellationToken token ? token : throw new Exception("ThreadLoop not given CancellationToken.");

            while (shutdownToken.IsCancellationRequested is false)
            {
                try
                {
                    ThreadStatus = AEWorkerThreadStatus.Processing;
                    if (DirectoryUpdate) UpdateSearchDirectories(SearchDirectories);

                    // Don't do anything if the queue is full
                    if (EncodingJobQueue.Count < State.GlobalJobSettings.MaxNumberOfJobsInQueue)
                    {
                        BuildSourceFiles();

                        shutdownToken.ThrowIfCancellationRequested();

                        // Add encoding jobs for automated search directories and files not encoded
                        foreach (KeyValuePair<string, List<SourceFileData>> entry in MovieSourceFiles)
                        {
                            if (SearchDirectories[entry.Key].Automated is true)
                            {
                                List<SourceFileData> moviesToEncode = entry.Value.Where(x => x.Encoded is false).ToList();
                                moviesToEncode.ForEach(x => CreateEncodingJob(x, SearchDirectories[entry.Key].PostProcessing, SearchDirectories[entry.Key].Source, shutdownToken));
                            }
                        }
                        foreach (KeyValuePair<string, List<ShowSourceFileData>> entry in ShowSourceFiles)
                        {
                            if (SearchDirectories[entry.Key].Automated is true)
                            {
                                List<ShowSourceFileData> episodesToEncode = entry.Value.Where(x => x.Encoded is false).ToList();
                                episodesToEncode.ForEach(x => CreateEncodingJob(x, SearchDirectories[entry.Key].PostProcessing, SearchDirectories[entry.Key].Source, shutdownToken));
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
                        details: new { ThreadStatus, EncodingJobQueueCount = EncodingJobQueue.Count, SearchDirectoriesCount = SearchDirectories.Count });
                    return;
                }
            }
        }

        #region PRIVATE FUNCTIONS
        private void UpdateSearchDirectories(Dictionary<string, SearchDirectory> searchDirectories)
        {
            searchDirectories = State.Directories.ToDictionary(x => x.Key, x => x.Value.DeepClone());

            // Remove any old directories (keys) in source files
            List<string> deleteKeys = ShowSourceFiles.Keys.Except(searchDirectories.Keys).ToList();
            deleteKeys.ForEach(x => ShowSourceFiles.Remove(x, out _));

            deleteKeys = MovieSourceFiles.Keys.Except(searchDirectories.Keys).ToList();
            deleteKeys.ForEach(x => MovieSourceFiles.Remove(x, out _));

            DirectoryUpdate = false;
        }

        /// <summary> Builds out SourceFiles from the search directories </summary>
        private void BuildSourceFiles()
        {
            Parallel.ForEach(SearchDirectories, entry =>
            {
                try
                {
                    if (Directory.Exists(entry.Value.Source))
                    {
                        List<SourceFile> sourceFiles = new();

                        IEnumerable<string> sourceFilePaths = Directory.GetFiles(entry.Value.Source, "*.*", SearchOption.AllDirectories)
                            .Where(file => ValidSourceFile(file));
                        HashSet<string> destinationFiles = Directory.GetFiles(entry.Value.Destination, "*.*", SearchOption.AllDirectories)
                            .Where(file => State.JobFinderSettings.VideoFileExtensions.Any(file.ToLower().EndsWith))
                            .Select(file => Path.GetFileNameWithoutExtension(file))
                            .ToHashSet();

                        foreach (string sourceFilePath in sourceFilePaths)
                        {
                            if (File.Exists(sourceFilePath) is false) continue;

                            SourceFile sourceFile = new()
                            {
                                FullPath = sourceFilePath,
                                DestinationFullPath = sourceFilePath.Replace(entry.Value.Source, entry.Value.Destination),
                                Encoded = destinationFiles.Contains(Path.GetFileNameWithoutExtension(sourceFilePath)) &&
                                            (EncodingJobQueue.IsEncodingByFileName(Path.GetFileName(sourceFilePath)) is false)
                            };

                            sourceFiles.Add(sourceFile);
                        }

                        if (entry.Value.TVShowStructure is true)
                        {
                            if (ShowSourceFiles.ContainsKey(entry.Key) is false)
                            {
                                ShowSourceFiles[entry.Key] = new List<ShowSourceFileData>();
                            }

                            ShowSourceFiles[entry.Key].UpdateShowSourceFiles(sourceFiles);
                        }
                        else
                        {
                            if (MovieSourceFiles.ContainsKey(entry.Key) is false)
                            {
                                MovieSourceFiles[entry.Key] = new List<SourceFileData>();
                            }

                            MovieSourceFiles[entry.Key].UpdateSourceFiles(sourceFiles);
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

            _buildingSourceFilesEvent.Set();
        }

        private bool CreateEncodingJob(SourceFileData sourceFileData, PostProcessingSettings postProcessingSettings, string sourceDirectoryPath)
        {
            bool jobCreated = false;
            try
            {
                lock (_lock)
                {
                    // Don't create encoding job if we are at max count
                    if (EncodingJobQueue.Count < State.GlobalJobSettings.MaxNumberOfJobsInQueue)
                    {
                        // Only add encoding job if file is ready.
                        if (CheckFileReady(sourceFileData.FullPath))
                        {
                            // Prep Data for creating job
                            List<string> updatedCopyFilePaths = null;
                            if (postProcessingSettings?.CopyFilePaths?.Any() ?? false)
                            {
                                // Update copy file paths with full destination directory (for extras and shows with subdirectories)
                                updatedCopyFilePaths = new List<string>();
                                foreach (string oldPath in postProcessingSettings.CopyFilePaths)
                                {
                                    updatedCopyFilePaths.Add($"{oldPath}{sourceFileData.FullPath.Replace(sourceDirectoryPath, "")}");
                                }
                            }

                            PostProcessingSettings updatedPostProcessingSettings = new()
                            {
                                CopyFilePaths = updatedCopyFilePaths,
                                DeleteSourceFile = postProcessingSettings?.DeleteSourceFile ?? false
                            };

                            ulong? newJobId = EncodingJobQueue.CreateEncodingJob(sourceFileData, updatedPostProcessingSettings);
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

        private bool CreateEncodingJob(SourceFileData sourceFileData, PostProcessingSettings postProcessingSettings, string sourceDirectoryPath, CancellationToken shutdownToken)
        {
            shutdownToken.ThrowIfCancellationRequested();
            return CreateEncodingJob(sourceFileData, postProcessingSettings, sourceDirectoryPath);
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
            List<long> fileSizes = new();
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

        private void UpdateSourceFiles(List<SourceFileData> sourceFiles, IEnumerable<SourceFile> newSourceFiles)
        {
            IEnumerable<SourceFileData> sourceFilesToRemove = sourceFiles.Except(newSourceFiles, (s, n) => string.Equals(s.FullPath, n.FullPath, StringComparison.OrdinalIgnoreCase));
            sourceFiles.RemoveRange(sourceFilesToRemove);

            IEnumerable<SourceFileData> sourceFilesToAdd = newSourceFiles.Except(sourceFiles, (n, s) => string.Equals(n.FullPath, s.FullPath, StringComparison.OrdinalIgnoreCase))
                .Select(x => new SourceFileData(x));
            sourceFiles.AddRange(sourceFilesToAdd);

            Debug.Assert(sourceFiles.Count == newSourceFiles.Count(), "Number of incoming source files should match outgoing number of source files.");

            sourceFiles.Sort(SourceFileData.CompareByFileName);
        }
    }
}
