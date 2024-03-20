using AutoEncodeServer.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Base;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using AutoEncodeUtilities.Json;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace AutoEncodeServer.EncodingJob
{
    public class EncodingJobModel :
        ModelBase,
        IEncodingJobModel,
        IEncodingJobData
    {
        #region Properties
        public ulong Id { get; }

        private string _title = string.Empty;
        public string Title
        {
            get => string.IsNullOrWhiteSpace(_title) ? Name : _title;
            set => _title = value;
        }

        public string Name { get; }

        public string FileName { get; }

        public string SourceFullPath { get; }

        public string DestinationFullPath { get; }

        #endregion Properties

        #region Status Properties
        public bool IsComplete => HasError is false && EncodingProgress == 100 &&             // Ensure not errored and EncodingProgress is at 100%
            ((Status.Equals(EncodingJobStatus.ENCODED) && NeedsPostProcessing is false) ||      // If post-processing not needed, make sure status is ENCODED
            (Status.Equals(EncodingJobStatus.POST_PROCESSED) && NeedsPostProcessing is true));  // Or, If post-processing needed, make sure status is POST_PROCESSED

        private EncodingJobStatus _status;
        public EncodingJobStatus Status
        {
            get => _status;
            set => SetAndNotify(_status, value, () => _status = value, CommunicationConstants.EncodingJobStatusUpdate);
        }

        private EncodingJobBuildingStatus _buildingStatus = EncodingJobBuildingStatus.BUILDING;
        public EncodingJobBuildingStatus BuildingStatus
        {
            get => _buildingStatus;
            set => SetAndNotify(_buildingStatus, value, () => _buildingStatus = value, CommunicationConstants.EncodingJobStatusUpdate);
        }

        public bool IsProcessing => Status.Equals(EncodingJobStatus.BUILDING) ||
                                    Status.Equals(EncodingJobStatus.ENCODING) ||
                                    Status.Equals(EncodingJobStatus.POST_PROCESSING);

        public bool HasError { get; set; } = false;

        public string ErrorMessage { get; set; } = string.Empty;

        public DateTime? ErrorTime { get; set; } = null;

        public bool ToBePaused { get; set; } = false;

        public bool Paused { get; set; } = false;

        public bool Canceled => TaskCancellationTokenSource?.IsCancellationRequested ?? false;

        public bool CanCancel => IsProcessing;

        public bool Complete => (HasError is false && EncodingProgress == 100) && // Ensure not errored and EncodingProgress is at 100%
            ((Status.Equals(EncodingJobStatus.ENCODED) && NeedsPostProcessing is false) || // If post-processing not needed, make sure status is ENCODED
            (Status.Equals(EncodingJobStatus.POST_PROCESSED) && NeedsPostProcessing is true)); // Or, If post-processing needed, make sure status is POST_PROCESSED
        #endregion Status Properties

        #region Encoding Progress Properties
        public byte EncodingProgress { get; set; }

        public TimeSpan ElapsedEncodingTime { get; set; } = TimeSpan.Zero;

        public DateTime? CompletedEncodingDateTime { get; set; } = null;

        public DateTime? CompletedPostProcessingTime { get; set; } = null;
        #endregion Encoding Progress Properties

        #region Processing Data
        private ISourceStreamData _sourceStreamData;
        public ISourceStreamData SourceStreamData
        {
            get => _sourceStreamData;
            set => SetAndNotify(_sourceStreamData, value, () => _sourceStreamData = value, CommunicationConstants.EncodingJobProcessingDataUpdate);
        }

        private EncodingInstructions _encodingInstructions = null;
        public EncodingInstructions EncodingInstructions
        {
            get => _encodingInstructions;
            set => SetAndNotify(_encodingInstructions, value, () => _encodingInstructions = value, CommunicationConstants.EncodingJobProcessingDataUpdate);
        }

        public PostProcessingSettings PostProcessingSettings { get; set; }

        public PostProcessingFlags PostProcessingFlags { get; set; } = PostProcessingFlags.None;

        public bool NeedsPostProcessing => !PostProcessingFlags.Equals(PostProcessingFlags.None) && PostProcessingSettings is not null;

        private IEncodingCommandArguments _encodingCommandArguments;
        [JsonConverter(typeof(EncodingCommandArgumentsConverter<IEncodingCommandArguments>))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IEncodingCommandArguments EncodingCommandArguments
        {
            get => _encodingCommandArguments;
            set => SetAndNotify(_encodingCommandArguments, value, () => _encodingCommandArguments = value, CommunicationConstants.EncodingJobProcessingDataUpdate);
        }

        public CancellationTokenSource TaskCancellationTokenSource { get; set; }
        #endregion Processing Data

        /// <summary>Default Constructor</summary>
        public EncodingJobModel() { }

        /// <summary>Factory Constructor </summary>
        /// <param name="id">Id of the Job</param>
        /// <param name="sourceFileFullPath">Full Path of the source file</param>
        /// <param name="destinationFileFullPath">Full Path of the expected destination file.</param>
        /// <param name="postProcessingSettings"><see cref="PostProcessingSettings"/> of the job</param>
        public EncodingJobModel(ulong id, string sourceFileFullPath, string destinationFileFullPath, PostProcessingSettings postProcessingSettings)
        {
            Id = id;
            SourceFullPath = sourceFileFullPath;
            DestinationFullPath = destinationFileFullPath;
            FileName = Path.GetFileName(sourceFileFullPath);
            Name = Path.GetFileNameWithoutExtension(FileName);
            PostProcessingSettings = postProcessingSettings;
            SetPostProcessingFlags();
        }

        #region Public Methods

        #region Status Methods
        public void SetStatus(EncodingJobStatus status) => Status = status;

        public void SetBuildingStatus(EncodingJobBuildingStatus buildingStatus) => BuildingStatus = buildingStatus;

        public void SetError(string errorMessage, Exception ex = null)
        {
            HasError = true;
            ErrorTime = DateTime.Now;

            StringBuilder sb = new(errorMessage);
            if (ex is not null)
            {
                sb.AppendLine(ex.Message);

                Exception innerEx = ex.InnerException;
                while (innerEx is not null)
                {
                    sb.AppendLine(innerEx.Message);
                    innerEx = innerEx.InnerException;
                }
            }

            ErrorMessage = sb.ToString();
            
            ResetStatus();
        }

        public void Cancel()
        {
            if (CanCancel is true)
            {
                if (Canceled is false)
                {
                    TaskCancellationTokenSource?.Cancel();
                    OnPropertyChanged(CommunicationConstants.EncodingJobStatusUpdate);
                }
            }
        }

        public void ResetCancel()
        {
            TaskCancellationTokenSource = null;
            ResetStatus();
        }

        public void Pause()
        {
            if (IsProcessing is false)
            {
                ToBePaused = false;
                Paused = true;
            }
            else
            {
                ToBePaused = true;
                Paused = false;
            }

            OnPropertyChanged(CommunicationConstants.EncodingJobStatusUpdate);
        }

        public void Resume()
        {
            Paused = false;
            ToBePaused = false;

            OnPropertyChanged(CommunicationConstants.EncodingJobStatusUpdate);
        }
        #endregion Status Methods

        #region Processing Data Methods
        public void SetSourceStreamData(ISourceStreamData sourceStreamData) => SourceStreamData = sourceStreamData;

        public void SetSourceScanType(VideoScanType scanType)
        {
            if (SourceStreamData?.VideoStream is not null)
            {
                SourceStreamData.VideoStream.ScanType = scanType;
                OnPropertyChanged(CommunicationConstants.EncodingJobProcessingDataUpdate);
            }
        }

        public void SetSourceCrop(string crop)
        {
            if (SourceStreamData?.VideoStream is not null)
            {
                SourceStreamData.VideoStream.Crop = crop;
                OnPropertyChanged(CommunicationConstants.EncodingJobProcessingDataUpdate);
            }
        }

        public void AddSourceHDRMetadataFilePath(HDRFlags hdrFlag, string hdrMetadataFilePath)
        {
            if (SourceStreamData?.VideoStream is not null && SourceStreamData.VideoStream.HasDynamicHDR is true)
            {
                if (SourceStreamData.VideoStream.HDRData.DynamicMetadataFullPaths.TryAdd(hdrFlag, hdrMetadataFilePath) is true)
                {
                    OnPropertyChanged(CommunicationConstants.EncodingJobProcessingDataUpdate);
                }
            }
        }

        public void SetEncodingInstructions(EncodingInstructions encodingInstructions) =>
            EncodingInstructions = encodingInstructions;

        public void SetEncodingCommandArguments(IEncodingCommandArguments encodingCommandArguments) =>
            EncodingCommandArguments = encodingCommandArguments;

        public void SetTaskCancellationToken(CancellationTokenSource tokenSource) =>
            TaskCancellationTokenSource = tokenSource;


        #endregion Processing Data Methods

        #region Encoding Progress Methods
        public void UpdateEncodingProgress(byte? progress, TimeSpan? timeElapsed)
        {
            // If progress is null, just don't update it
            if (progress is byte progressByte)
            {
                if (progressByte > 100) EncodingProgress = 100;
                else if (progressByte < 0) EncodingProgress = 0;
                else EncodingProgress = progressByte;
            }

            if (timeElapsed is TimeSpan actualTimeElapsed)
            {
                ElapsedEncodingTime = actualTimeElapsed;
            }

            OnPropertyChanged(CommunicationConstants.EncodingJobEncodingProgressUpdate);
        }

        public void CompleteEncoding(TimeSpan timeElapsed)
        {
            CompletedEncodingDateTime = DateTime.Now;
            UpdateEncodingProgress(100, timeElapsed);
            SetStatus(EncodingJobStatus.ENCODED);
        }

        public void CompletePostProcessing()
        {
            CompletedPostProcessingTime = DateTime.Now;
            SetStatus(EncodingJobStatus.POST_PROCESSED);

            OnPropertyChanged(CommunicationConstants.EncodingJobEncodingProgressUpdate);
        }
        #endregion Encoding Progress Methods

        public EncodingJobStatusUpdateData GetStatusUpdate()
        {
            EncodingJobStatusUpdateData statusUpdateData = new();
            this.CopyProperties(statusUpdateData);
            return statusUpdateData;
        }

        public EncodingJobProcessingDataUpdateData GetProcessingDataUpdate()
        {
            EncodingJobProcessingDataUpdateData processingDataUpdate = new();
            this.CopyProperties(processingDataUpdate);
            return processingDataUpdate;
        }

        public EncodingJobEncodingProgressUpdateData GetEncodingUpdate()
        {
            EncodingJobEncodingProgressUpdateData encodingProgresssUpdateData = new();
            this.CopyProperties(encodingProgresssUpdateData);
            return encodingProgresssUpdateData;
        }

        public EncodingJobData ToEncodingJobData()
        {
            EncodingJobData encodingJobData = new();
            this.CopyProperties(encodingJobData);
            return encodingJobData;
        }

        public override string ToString() => $"({Id}) - {FileName}";
        #endregion Public Methods

        #region Private Methods
        private void ResetStatus()
        {
            if (Status > EncodingJobStatus.NEW)
            {
                if (Status.Equals(EncodingJobStatus.ENCODING))
                {
                    CompletedEncodingDateTime = null;
                    UpdateEncodingProgress(0, TimeSpan.Zero);
                }

                SetStatus(Status -= 1);
            }
        }

        /// <summary>Sets the given PostProcessingFlag for the job </summary>
        /// <param name="flag">Flag/param>
        private void SetPostProcessingFlag(PostProcessingFlags flag) => PostProcessingFlags |= flag;
        /// <summary>Clears the job of the given PostProcessingFlag </summary>
        /// <param name="flag">Flag</param>
        private void ClearPostProcessingFlag(PostProcessingFlags flag) => PostProcessingFlags &= ~flag;
        /// <summary>Looks at PostProcessingSettings and determines what flags need set</summary>
        private void SetPostProcessingFlags()
        {
            if (PostProcessingSettings is null)
            {
                PostProcessingFlags = PostProcessingFlags.None;
                return;
            }

            if ((PostProcessingSettings?.CopyFilePaths?.Count ?? -1) > 0 is true)
            {
                SetPostProcessingFlag(PostProcessingFlags.Copy);
            }
            else
            {
                ClearPostProcessingFlag(PostProcessingFlags.Copy);
            }

            if ((PostProcessingSettings?.DeleteSourceFile ?? false) is true)
            {
                SetPostProcessingFlag(PostProcessingFlags.DeleteSourceFile);
            }
            else
            {
                ClearPostProcessingFlag(PostProcessingFlags.DeleteSourceFile);
            }
        }
        #endregion Private Methods
    }
}
