using AutoEncodeServer.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AutoEncodeServer.WorkerThreads
{
    public partial class EncodingJobFinderThread : IEncodingJobFinderThread
    {
        #region Private Properties / Fields
        private bool _directoryUpdate = false;
        private bool _initialized = false;
        private readonly ManualResetEvent _sleepMRE = new(false);

        private ManualResetEvent ShutdownMRE { get; set; }
        private readonly CancellationTokenSource _shutdownCancellationTokenSource = new();

        /// <summary> Reference to the Server State</summary>
        private AEServerConfig State { get; set; }

        private Thread WorkerThread { get; set; }
        private TimeSpan ThreadSleep { get; set; } = TimeSpan.FromMinutes(2);
        private string ThreadName => WorkerThread?.Name ?? nameof(EncodingJobFinderThread);
        #endregion Private Properties / Fields

        #region Dependencies
        public ILogger Logger { get; set; }

        public IEncodingJobManager EncodingJobManager { get; set; }
        #endregion Dependencies

        /// <summary>Default Constructor</summary>
        public EncodingJobFinderThread() { }

        #region Public Functions
        public void Initialize(AEServerConfig serverState, ManualResetEvent shutdownMRE)
        {
            if (_initialized is false)
            {
                State = serverState;
                ShutdownMRE = shutdownMRE;
                ThreadSleep = State.JobFinderSettings.ThreadSleep;
                SearchDirectories = State.Directories.ToDictionary(x => x.Key, x => x.Value.DeepClone());
            }

            _initialized = true;
        }

        public void Start()
        {
            if (_initialized is false) throw new Exception($"{ThreadName} is not initialized.");

            Logger.LogInfo($"{ThreadName} Starting", ThreadName);


            WorkerThread = new Thread(ThreadLoop)
            {
                Name = ThreadName,
                IsBackground = true
            };

            BuildSourceFiles();

            WorkerThread.Start(_shutdownCancellationTokenSource.Token);
        }

        public void Stop()
        {
            Logger.LogInfo($"{ThreadName} Shutting Down", ThreadName);

            _shutdownCancellationTokenSource.Cancel();

            Wake();

            WorkerThread.Join();

            ShutdownMRE.Set();
        }

        public void UpdateSearchDirectories() => _directoryUpdate = true;

        public IDictionary<string, (bool IsShows, IEnumerable<SourceFileData> Files)> RequestSourceFiles()
        {
            Wake();

            bool success = _buildingSourceFilesEvent.WaitOne(TimeSpan.FromSeconds(30));

            if (success)
            {
                return SourceFiles.ToDictionary(x => x.Key, x => (x.Value.IsShows, x.Value.Files.AsEnumerable()));
            }

            return null;
        }

        public bool RequestEncodingJob(Guid guid)
        {
            bool success = false;

            // Wait for source file building if occurring
            if (_buildingSourceFilesEvent.WaitOne(TimeSpan.FromSeconds(30)))
            {
                if (SourceFilesByGuid.TryGetValue(guid, out (string Directory, SourceFileData File) sourceFile))
                {
                    if (string.IsNullOrWhiteSpace(sourceFile.Directory) is false && sourceFile.File is not null)
                    {
                        if (CreateEncodingJob(sourceFile.File, SearchDirectories[sourceFile.Directory]) is true)
                        {
                            success = true;
                        }
                        else
                        {
                            Logger.LogError($"Failed to create encoding job for requested file {sourceFile.File.FullPath}");
                        }
                    }
                    else
                    {
                        Logger.LogError("Source file has invalid data.");
                    }
                }
                else
                {
                    Logger.LogError("CLIENT REQUEST: Failed to find source file to encode with the requested GUID.");
                }
            }

            return success;
        }
        #endregion Public Functions

        #region Private Functions
        /// <summary> Wakes up thread by setting the Sleep AutoResetEvent.</summary>
        private void Wake() => _sleepMRE.Set();

        /// <summary> Sleeps thread for certain amount of time. </summary>
        private void Sleep()
        {
            if (_shutdownCancellationTokenSource.IsCancellationRequested is false)
            {
                _sleepMRE.Reset();
                _sleepMRE.WaitOne(ThreadSleep);
            }
        }
        #endregion Private Functions
    }
}
