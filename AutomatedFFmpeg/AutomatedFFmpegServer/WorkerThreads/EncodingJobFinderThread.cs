using AutomatedFFmpegServer.Base;
using AutomatedFFmpegUtilities;
using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;

namespace AutomatedFFmpegServer.WorkerThreads
{
    public class EncodingJobFinderThread : AFWorkerThreadBase
    {
        private bool Shutdown = false;
        private bool DirectoryUpdate = false;
 
        private readonly object _videoSourceFileLock = new object();
        private readonly object _showSourceFileLock = new object();
        private Dictionary<string, SearchDirectory> SearchDirectories { get; set; }
        private Dictionary<string, List<VideoSourceData>> _videoSourceFiles { get; set; } = new Dictionary<string, List<VideoSourceData>>();
        private Dictionary<string, List<ShowSourceData>> _showSourceFiles { get; set; } = new Dictionary<string, List<ShowSourceData>>();

        /// <summary>Constructor</summary>
        /// <param name="mainThread">AFServerMainThread</param>
        /// <param name="serverConfig">AFServerConfig</param>
        /// <param name="encodingJobs">EncodingJobs</param>
        public EncodingJobFinderThread(AFServerMainThread mainThread, AFServerConfig serverConfig, EncodingJobs encodingJobs)
            : base("EncodingJobFinderThread", mainThread, serverConfig, encodingJobs)
        {
            SearchDirectories = Config.Directories.ToDictionary(x => x.Key, x => (SearchDirectory)x.Value.Clone());
        }

        #region PUBLIC FUNCTIONS
        public override void Start(params object[] threadObjects) => base.Start(SearchDirectories);

        public override void Stop()
        {
            Shutdown = true;
            base.Stop();
        }

        /// <summary>Signal to thread to update directories to search for jobs.</summary>
        public void UpdateSearchDirectories() => DirectoryUpdate = true;

        /// <summary>Gets a copy of video source files </summary>
        /// <returns></returns>
        public Dictionary<string, List<VideoSourceData>> GetVideoSourceFiles()
        {
            lock (_videoSourceFileLock)
            {
                return _videoSourceFiles.ToDictionary(x => x.Key, x => x.Value.Select(v => new VideoSourceData(v)).ToList());
            }
        }

        /// <summary>Gets a copy of show source files</summary>
        /// <returns></returns>
        public Dictionary<string, List<ShowSourceData>> GetShowSourceFiles()
        {
            lock (_showSourceFileLock)
            {
                return _showSourceFiles.ToDictionary(x => x.Key, x => x.Value.Select(s => s.DeepClone()).ToList());
            }
        }
        #endregion PUBLIC FUNCTIONS

        protected override void ThreadLoop(EncodingJobs encodingJobs, object[] threadObjects)
        {
            Dictionary<string, SearchDirectory> searchDirectories = (Dictionary<string, SearchDirectory>)threadObjects[0];

            while (Shutdown == false)
            {
                try
                {
                    Status = AFWorkerThreadStatus.PROCESSING;
                    if (DirectoryUpdate) UpdateSearchDirectories(searchDirectories);

                    bool bFoundEncodingJob = false;
                    foreach (KeyValuePair<string, SearchDirectory> entry in searchDirectories)
                    {
                        if (Directory.Exists(entry.Value.Source))
                        {
                            // TV Show structured directories
                            if (entry.Value.TVShowStructure)
                            {
                                List<VideoSourceData> newEncodingJobs = new List<VideoSourceData>();
                                lock (_showSourceFileLock)
                                {
                                    _showSourceFiles[entry.Key] = new List<ShowSourceData>();
                                    List<string> sourceShows = Directory.GetDirectories(entry.Value.Source).ToList();
                                    List<string> destinationFiles = Directory.GetFiles(entry.Value.Destination, "*.*", SearchOption.AllDirectories).ToList()
                                        .Where(file => Config.ServerSettings.VideoFileExtensions.Any(file.ToLower().EndsWith)).Select(file => file = Path.GetFileNameWithoutExtension(file)).ToList();
                                    // Show
                                    foreach (string showPath in sourceShows)
                                    {
                                        string showName = showPath.Replace(entry.Value.Source, "").RemoveLeadingSlash();
                                        ShowSourceData showData = new ShowSourceData(showName);
                                        List<string> seasons = Directory.GetDirectories(showPath).ToList();
                                        // Season
                                        foreach (string seasonPath in seasons)
                                        {
                                            string season = seasonPath.Replace(showPath, "").RemoveLeadingSlash();
                                            SeasonSourceData seasonData = new SeasonSourceData(season);
                                            List<string> episodes = Directory.GetFiles(seasonPath, "*.*", SearchOption.AllDirectories)
                                                .Where(file => Config.ServerSettings.VideoFileExtensions.Any(file.ToLower().EndsWith)).ToList();
                                            // Episode
                                            foreach (string episodePath in episodes)
                                            {
                                                string episode = episodePath.Replace(seasonPath, "").RemoveLeadingSlash();
                                                VideoSourceData episodeData = new VideoSourceData()
                                                {
                                                    FileName = episode,
                                                    FullPath = episodePath,
                                                    Encoded = destinationFiles.Contains(Path.GetFileNameWithoutExtension(episodePath))
                                                };
                                                seasonData.Episodes.Add(episodeData);
                                                if (episodeData.Encoded == false && entry.Value.Automated == true)
                                                {
                                                    bFoundEncodingJob = true;
                                                    newEncodingJobs.Add(episodeData);
                                                }
                                            }
                                            showData.Seasons.Add(seasonData);
                                        }
                                        _showSourceFiles[entry.Key].Add(showData);
                                    }
                                }
                                newEncodingJobs.ForEach(x => AddEncodingJob(x, entry.Value.Source, entry.Value.Destination));
                            }
                            else
                            {
                                List<VideoSourceData> newEncodingJobs = new List<VideoSourceData>();
                                lock (_videoSourceFileLock)
                                {
                                    _videoSourceFiles[entry.Key] = new List<VideoSourceData>();
                                    List<string> sourceFiles = Directory.GetFiles(entry.Value.Source, "*.*", SearchOption.AllDirectories)
                                        .Where(file => Config.ServerSettings.VideoFileExtensions.Any(file.ToLower().EndsWith)).ToList();
                                    List<string> destinationFiles = Directory.GetFiles(entry.Value.Destination, "*.*", SearchOption.AllDirectories)
                                        .Where(file => Config.ServerSettings.VideoFileExtensions.Any(file.ToLower().EndsWith)).Select(file => file = Path.GetFileNameWithoutExtension(file)).ToList();
                                    foreach (string sourceFile in sourceFiles)
                                    {
                                        // Handles files in subdirectories
                                        string filename = sourceFile.Replace(entry.Value.Source, "").RemoveLeadingSlash();

                                        VideoSourceData sourceData = new VideoSourceData()
                                        {
                                            FileName = filename,
                                            FullPath = sourceFile,
                                            Encoded = destinationFiles.Contains(Path.GetFileNameWithoutExtension(sourceFile))
                                        };
                                        _videoSourceFiles[entry.Key].Add(sourceData);

                                        // If the source file has not been encoded already and it's an automated directory, add to encoding job list
                                        if (sourceData.Encoded == false && entry.Value.Automated == true)
                                        {
                                            bFoundEncodingJob = true;
                                            newEncodingJobs.Add(sourceData);
                                        }
                                    }
                                }

                                if (newEncodingJobs.Count > 0)
                                {
                                    newEncodingJobs.ForEach(x => AddEncodingJob(x, entry.Value.Source, entry.Value.Destination));
                                    MainThread.WakeThreads(); // Found jobs, wake up other threads if they aren't already awake
                                }
                            }
                        }
                        else
                        {
                            // TODO Logging
                            Console.WriteLine($"{entry.Value.Source} does not exist.");
                        }
                    }

                    if (bFoundEncodingJob == false) Sleep();
                }
                catch (Exception ex)
                {
                    // TODO Logging
                    Debug.WriteLine($"[{ThreadName}] ERROR: {ex.Message}");
                }
            }
        }

        #region PRIVATE FUNCTIONS
        private void UpdateSearchDirectories(Dictionary<string, SearchDirectory> searchDirectories)
        {
            searchDirectories = Config.Directories.ToDictionary(x => x.Key, x => (SearchDirectory)x.Value.Clone());

            // Remove any old directories (keys) in source files
            lock (_showSourceFileLock)
            {
                List<string> deleteKeys = _showSourceFiles.Keys.Except(searchDirectories.Keys).ToList();
                deleteKeys.ForEach(x => _showSourceFiles.Remove(x));
            }
            lock (_videoSourceFileLock)
            {
                List<string> deleteKeys = _videoSourceFiles.Keys.Except(searchDirectories.Keys).ToList();
                deleteKeys.ForEach(x => _videoSourceFiles.Remove(x));
            }

            DirectoryUpdate = false;
        }

        private void AddEncodingJob(VideoSourceData sourceData, string sourceDirectoryPath, string destinationDirectoryPath)
        {
            // Only add encoding job is file is ready.
            if (CheckFileReady(sourceData.FullPath))
            {
                EncodingJob encodingJob = new EncodingJob()
                {
                    Name = sourceData.FileName,
                    SourceFullPath = sourceData.FullPath,
                    DestinationFullPath = sourceData.FullPath.Replace(sourceDirectoryPath, destinationDirectoryPath)
                };

                if (!EncodingJobs.Exists(encodingJob)) EncodingJobs.AddEncodingJob(encodingJob);
            }
        }

        /// <summary>Check if file size is changing, if it is, it is not ready for encoding.</summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool CheckFileReady(string filePath)
        {
            bool fileReady = false;
            FileInfo fileInfo = new FileInfo(filePath);

            long beforeFileSize = fileInfo.Length;
            Thread.Sleep(2000);
            long afterFileSize = fileInfo.Length;

            if (beforeFileSize == afterFileSize) fileReady = true;

            return fileReady;
        }
        #endregion PRIVATE FUNCTIONS
    }
}
