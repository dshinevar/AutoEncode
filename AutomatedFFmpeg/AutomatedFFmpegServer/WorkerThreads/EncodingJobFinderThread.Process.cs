﻿using AutomatedFFmpegUtilities;
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
        private void ThreadLoop()
        {
            int failedToFindJobCount = 0;

            while (Shutdown is false)
            {
                try
                {
                    Status = AFWorkerThreadStatus.PROCESSING;
                    if (DirectoryUpdate) UpdateSearchDirectories(SearchDirectories);

                    bool bFoundEncodingJob = false;

                    // Don't do anything if the queue is full
                    if (EncodingJobQueue.Count < State.GlobalJobSettings.MaxNumberOfJobsInQueue)
                    {
                        BuildSourceFiles(SearchDirectories);

                        // Add encoding jobs for automated search directories and files not encoded
                        foreach (KeyValuePair<string, List<VideoSourceData>> entry in MovieSourceFiles)
                        {
                            if (SearchDirectories[entry.Key].Automated is true)
                            {
                                List<VideoSourceData> moviesToEncode = entry.Value.Where(x => x.Encoded is false).ToList();
                                bFoundEncodingJob = moviesToEncode.Any();
                                moviesToEncode.ForEach(x => CreateEncodingJob(x, SearchDirectories[entry.Key].PostProcessing, SearchDirectories[entry.Key].Source, SearchDirectories[entry.Key].Destination));
                            }

                        }
                        foreach (KeyValuePair<string, List<ShowSourceData>> entry in ShowSourceFiles)
                        {
                            if (SearchDirectories[entry.Key].Automated is true)
                            {
                                List<VideoSourceData> episodesToEncode = entry.Value.SelectMany(show => show.Seasons).SelectMany(season => season.Episodes)
                                    .Where(episode => episode.Encoded is false).ToList();
                                bFoundEncodingJob = episodesToEncode.Any();
                                episodesToEncode.ForEach(x => CreateEncodingJob(x, SearchDirectories[entry.Key].PostProcessing, SearchDirectories[entry.Key].Source, SearchDirectories[entry.Key].Destination));
                            }
                        }
                    }

                    if (bFoundEncodingJob is false)
                    {
                        failedToFindJobCount++;
                        if (failedToFindJobCount >= MaxFailedToFindJobCount)
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
                    Logger.LogException(ex, "Error during looking for encoding jobs.", ThreadName);
                    Debug.WriteLine($"[{ThreadName}] ERROR: {ex.Message}");
                    return;
                }
            }
        }

        #region PRIVATE FUNCTIONS
        private void UpdateSearchDirectories(Dictionary<string, SearchDirectory> searchDirectories)
        {
            searchDirectories = State.Directories.ToDictionary(x => x.Key, x => x.Value.DeepClone());

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
            Parallel.ForEach(searchDirectories, entry =>
            {
                if (Directory.Exists(entry.Value.Source))
                {
                    // TV Show structured directories
                    if (entry.Value.TVShowStructure)
                    {
                        List<ShowSourceData> shows = new();
                        IEnumerable<string> sourceShows = Directory.GetDirectories(entry.Value.Source);
                        IEnumerable<string> destinationFiles = Directory.GetFiles(entry.Value.Destination, "*.*", SearchOption.AllDirectories)
                            .Where(file => State.ServerSettings.VideoFileExtensions.Any(file.ToLower().EndsWith)).Select(file => file = Path.GetFileNameWithoutExtension(file));
                        // Show
                        foreach (string showPath in sourceShows)
                        {
                            string showName = new DirectoryInfo(showPath).Name;
                            ShowSourceData showData = new(showName);
                            IEnumerable<string> seasons = Directory.GetDirectories(showPath);
                            // Season
                            foreach (string seasonPath in seasons)
                            {
                                string season = new DirectoryInfo(seasonPath).Name;
                                SeasonSourceData seasonData = new(season);
                                IEnumerable<string> episodes = Directory.GetFiles(seasonPath, "*.*", SearchOption.AllDirectories)
                                    .Where(file => State.ServerSettings.VideoFileExtensions.Any(file.ToLower().EndsWith));
                                // Episode
                                foreach (string episodePath in episodes)
                                {
                                    VideoSourceData episodeData = new()
                                    {
                                        FullPath = episodePath,
                                        Encoded = destinationFiles.Contains(Path.GetFileNameWithoutExtension(episodePath))
                                    };
                                    seasonData.Episodes.Add(episodeData);
                                }
                                seasonData.Episodes.Sort((x,y) => x.FileName.CompareTo(y.FileName));
                                showData.Seasons.Add(seasonData);
                            }
                            showData.Seasons.Sort((x, y) => x.Season.CompareTo(y.Season));
                            shows.Add(showData);
                        }
                        shows.Sort((x, y) => x.ShowName.CompareTo(y.ShowName));

                        lock (showSourceFileLock)
                        {
                            ShowSourceFiles[entry.Key] = shows;
                        }
                    }
                    else
                    {
                        List<VideoSourceData> movies = new();
                        IEnumerable<string> sourceFiles = Directory.GetFiles(entry.Value.Source, "*.*", SearchOption.AllDirectories)
                            .Where(file => State.ServerSettings.VideoFileExtensions.Any(file.ToLower().EndsWith));
                        IEnumerable<string> destinationFiles = Directory.GetFiles(entry.Value.Destination, "*.*", SearchOption.AllDirectories)
                            .Where(file => State.ServerSettings.VideoFileExtensions.Any(file.ToLower().EndsWith)).Select(file => file = Path.GetFileNameWithoutExtension(file));
                        foreach (string sourceFile in sourceFiles)
                        {
                            VideoSourceData sourceData = new()
                            {
                                FullPath = sourceFile,
                                Encoded = destinationFiles.Contains(Path.GetFileNameWithoutExtension(sourceFile))
                            };
                            movies.Add(sourceData);
                        }
                        movies.Sort((x,y) => x.FileName.CompareTo(y.FileName));

                        lock (movieSourceFileLock)
                        {
                            MovieSourceFiles[entry.Key] = movies;
                        }
                    }
                }
                else
                {
                    Logger.LogError($"{entry.Value.Source} does not exist.", ThreadName);
                    Debug.WriteLine($"{entry.Value.Source} does not exist.");
                }
            });
        }

        private void CreateEncodingJob(VideoSourceData sourceData, PostProcessingSettings postProcessingSettings, string sourceDirectoryPath, string destinationDirectoryPath)
        {
            // Don't create encoding job if we are at max count
            if (EncodingJobQueue.Count < State.GlobalJobSettings.MaxNumberOfJobsInQueue)
            {
                // Only add encoding job is file is ready.
                if (CheckFileReady(sourceData.FullPath))
                {
                    int newJobId = EncodingJobQueue.CreateEncodingJob(sourceData, postProcessingSettings, sourceDirectoryPath, destinationDirectoryPath);
                    if (newJobId is not -1) Logger.LogInfo($"(JobID: {newJobId}) {sourceData.FileName} added to encoding job queue.", ThreadName);
                }
            }
        }

        /// <summary>Check if file size is changing, if it is, it is not ready for encoding.</summary>
        /// <param name="filePath"></param>
        /// <returns>True if file is ready; False, otherwise</returns>
        private static bool CheckFileReady(string filePath)
        {
            FileInfo fileInfo = new(filePath);
            long beforeFileSize = fileInfo.Length;

            Thread.Sleep(2000);

            fileInfo.Refresh();
            long afterFileSize = fileInfo.Length;

            return beforeFileSize == afterFileSize;
        }
        #endregion PRIVATE FUNCTIONS
    }
}