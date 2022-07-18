using AutomatedFFmpegUtilities;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutomatedFFmpegServer.WorkerThreads
{
    public partial class EncodingJobFinderThread
    {
        protected void ThreadLoop()
        {
            int failedToFindJobCount = 0;

            while (Shutdown is false)
            {
                try
                {
                    Status = AFWorkerThreadStatus.PROCESSING;
                    if (DirectoryUpdate) UpdateSearchDirectories(SearchDirectories);

                    bool bFoundEncodingJob = false;
                    BuildSourceFiles(SearchDirectories);

                    // Add encoding jobs for automated search directories and files not encoded
                    foreach (KeyValuePair<string, List<VideoSourceData>> entry in MovieSourceFiles)
                    {
                        if (SearchDirectories[entry.Key].Automated is true)
                        {
                            List<VideoSourceData> moviesToEncode = entry.Value.Where(x => x.Encoded is false).ToList();
                            bFoundEncodingJob = moviesToEncode.Any();
                            moviesToEncode.ForEach(x => CreateEncodingJob(x, SearchDirectories[entry.Key].Source, SearchDirectories[entry.Key].Destination));
                        }

                    }
                    foreach (KeyValuePair<string, List<ShowSourceData>> entry in ShowSourceFiles)
                    {
                        if (SearchDirectories[entry.Key].Automated is true)
                        {
                            List<VideoSourceData> episodesToEncode = entry.Value.SelectMany(show => show.Seasons).SelectMany(season => season.Episodes)
                                .Where(episode => episode.Encoded is false).ToList();
                            bFoundEncodingJob = episodesToEncode.Any();
                            episodesToEncode.ForEach(x => CreateEncodingJob(x, SearchDirectories[entry.Key].Source, SearchDirectories[entry.Key].Destination));
                        }
                    }

                    if (bFoundEncodingJob is false)
                    {
                        failedToFindJobCount++;
                        if (failedToFindJobCount >= MAX_COUNT)
                        {
                            DeepSleep();
                        }
                        else
                        {
                            Sleep();
                        }
                    }
                    else
                    {
                        failedToFindJobCount = 0;
                    }
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
            lock (showSourceFileLock)
            {
                List<string> deleteKeys = ShowSourceFiles.Keys.Except(searchDirectories.Keys).ToList();
                deleteKeys.ForEach(x => ShowSourceFiles.Remove(x));
            }
            lock (movieSourceFileLock)
            {
                List<string> deleteKeys = MovieSourceFiles.Keys.Except(searchDirectories.Keys).ToList();
                deleteKeys.ForEach(x => MovieSourceFiles.Remove(x));
            }

            DirectoryUpdate = false;
        }

        /// <summary> Builds out SourceFiles from the search directories </summary>
        /// <param name="searchDirectories">Search Directories</param>
        private void BuildSourceFiles(Dictionary<string, SearchDirectory> searchDirectories)
        {
            Parallel.ForEach(searchDirectories.ToList(), entry =>
            {
                if (Directory.Exists(entry.Value.Source))
                {
                    // TV Show structured directories
                    if (entry.Value.TVShowStructure)
                    {
                        List<ShowSourceData> shows = new List<ShowSourceData>();
                        List<string> sourceShows = Directory.GetDirectories(entry.Value.Source).ToList();
                        List<string> destinationFiles = Directory.GetFiles(entry.Value.Destination, "*.*", SearchOption.AllDirectories).ToList()
                            .Where(file => Config.ServerSettings.VideoFileExtensions.Any(file.ToLower().EndsWith)).Select(file => file = Path.GetFileNameWithoutExtension(file)).ToList();
                        // Show
                        foreach (string showPath in sourceShows)
                        {
                            string showName = showPath.Replace(entry.Value.Source, "").RemoveLeadingSlashes();
                            ShowSourceData showData = new ShowSourceData(showName);
                            List<string> seasons = Directory.GetDirectories(showPath).ToList();
                            // Season
                            foreach (string seasonPath in seasons)
                            {
                                string season = seasonPath.Replace(showPath, "").RemoveLeadingSlashes();
                                SeasonSourceData seasonData = new SeasonSourceData(season);
                                List<string> episodes = Directory.GetFiles(seasonPath, "*.*", SearchOption.AllDirectories)
                                    .Where(file => Config.ServerSettings.VideoFileExtensions.Any(file.ToLower().EndsWith)).ToList();
                                // Episode
                                foreach (string episodePath in episodes)
                                {
                                    string episode = episodePath.Replace(seasonPath, "").RemoveLeadingSlashes();
                                    VideoSourceData episodeData = new VideoSourceData()
                                    {
                                        FileName = episode,
                                        FullPath = episodePath,
                                        Encoded = destinationFiles.Contains(Path.GetFileNameWithoutExtension(episodePath))
                                    };
                                    seasonData.Episodes.Add(episodeData);
                                }
                                showData.Seasons.Add(seasonData);
                            }
                            shows.Add(showData);
                        }

                        lock (showSourceFileLock)
                        {
                            ShowSourceFiles[entry.Key] = shows;
                        }
                    }
                    else
                    {
                        List<VideoSourceData> movies = new List<VideoSourceData>();
                        List<string> sourceFiles = Directory.GetFiles(entry.Value.Source, "*.*", SearchOption.AllDirectories)
                            .Where(file => Config.ServerSettings.VideoFileExtensions.Any(file.ToLower().EndsWith)).ToList();
                        List<string> destinationFiles = Directory.GetFiles(entry.Value.Destination, "*.*", SearchOption.AllDirectories)
                            .Where(file => Config.ServerSettings.VideoFileExtensions.Any(file.ToLower().EndsWith)).Select(file => file = Path.GetFileNameWithoutExtension(file)).ToList();
                        foreach (string sourceFile in sourceFiles)
                        {
                            // Handles files in subdirectories
                            string filename = sourceFile.Replace(entry.Value.Source, "").RemoveLeadingSlashes();

                            VideoSourceData sourceData = new VideoSourceData()
                            {
                                FileName = filename,
                                FullPath = sourceFile,
                                Encoded = destinationFiles.Contains(Path.GetFileNameWithoutExtension(sourceFile))
                            };
                            movies.Add(sourceData);
                        }

                        lock (movieSourceFileLock)
                        {
                            MovieSourceFiles[entry.Key] = movies;
                        }
                    }
                }
                else
                {
                    // TODO Logging
                    Console.WriteLine($"{entry.Value.Source} does not exist.");
                }
            });
        }

        private void CreateEncodingJob(VideoSourceData sourceData, string sourceDirectoryPath, string destinationDirectoryPath)
        {
            // Only add encoding job is file is ready.
            if (CheckFileReady(sourceData.FullPath))
            {
                EncodingJobQueue.CreateEncodingJob(sourceData, sourceDirectoryPath, destinationDirectoryPath);
            }
        }

        /// <summary>Check if file size is changing, if it is, it is not ready for encoding.</summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool CheckFileReady(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            long beforeFileSize = fileInfo.Length;
            Thread.Sleep(2000);
            long afterFileSize = fileInfo.Length;

            return beforeFileSize == afterFileSize;
        }
        #endregion PRIVATE FUNCTIONS
    }
}
