using AutoEncodeClient.Communication;
using AutoEncodeClient.Config;
using AutoEncodeClient.Data;
using AutoEncodeClient.Enums;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.Models.StreamDataModels;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Base;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using AutoEncodeUtilities.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoEncodeClient.Models
{
    public class EncodingJobClientModel :
        ModelBase,
        IEncodingJobClientModel,
        IUpdateable<IEncodingJobData>
    {
        #region Dependencies
        public ICommunicationManager CommunicationManager { get; set; }

        public IClientUpdateSubscriber ClientUpdateSubscriber { get; set; }

        public AEClientConfig Config { get; set; }
        #endregion Dependencies

        public EncodingJobClientModel(IEncodingJobData encodingJobData)
        {
            encodingJobData.CopyProperties(this);
            if (encodingJobData.SourceStreamData is not null)
            {
                SourceStreamData = new(encodingJobData.SourceStreamData);
            }
        }

        public void Initialize()
        {
            List<SubscriberTopic> topics = [];
            topics.Add(new(ClientUpdateType.Status_Update, $"{CommunicationConstants.EncodingJobStatusUpdate}-{Id}"));
            topics.Add(new(ClientUpdateType.Processing_Data_Update, $"{CommunicationConstants.EncodingJobProcessingDataUpdate}-{Id}"));
            topics.Add(new(ClientUpdateType.Encoding_Progress_Update, $"{CommunicationConstants.EncodingJobEncodingProgressUpdate}-{Id}"));

            ClientUpdateSubscriber.Initialize(Config.ConnectionSettings.IPAddress, Config.ConnectionSettings.ClientUpdatePort, topics);
            ClientUpdateSubscriber.StatusUpdateReceived += async (s, d) => await Task.Run(() => UpdateStatus(d));
            ClientUpdateSubscriber.ProcessingDataUpdateReceived += async (s, d) => await Task.Run(() => UpdateProcessingData(d));
            ClientUpdateSubscriber.EncodingProgressUpdateReceived += async (s, d) => await Task.Run(() => UpdateEncodingProgress(d));

            ClientUpdateSubscriber.Start();
        }

        #region Properties
        private ulong _id;
        public ulong Id
        {
            get => _id;
            set => SetAndNotify(_id, value, () => _id = value);
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetAndNotify(_title, value, () => _title = value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetAndNotify(_name, value, () => _name = value);
        }

        private string _filename = string.Empty;
        public string FileName
        {
            get => _filename;
            set => SetAndNotify(_filename, value, () => _filename = value);
        }

        private string _sourceFullPath = string.Empty;
        public string SourceFullPath
        {
            get => _sourceFullPath;
            set => SetAndNotify(_sourceFullPath, value, () => _sourceFullPath = value);
        }

        private string _destinationFullPath = string.Empty;
        public string DestinationFullPath
        {
            get => _destinationFullPath;
            set => SetAndNotify(_destinationFullPath, value, () => _destinationFullPath = value);
        }

        #region Status
        private EncodingJobStatus _status = EncodingJobStatus.NEW;
        public EncodingJobStatus Status
        {
            get => _status;
            set => SetAndNotify(_status, value, () => _status = value);
        }

        private EncodingJobBuildingStatus _buildingStatus = EncodingJobBuildingStatus.BUILDING;
        public EncodingJobBuildingStatus BuildingStatus
        {
            get => _buildingStatus;
            set => SetAndNotify(_buildingStatus, value, () => _buildingStatus = value);
        }

        private bool _hasError = false;
        public bool HasError
        {
            get => _hasError;
            set => SetAndNotify(_hasError, value, () => _hasError = value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetAndNotify(_errorMessage, value, () => _errorMessage = value);
        }

        private DateTime? _errorTime = null;
        public DateTime? ErrorTime
        {
            get => _errorTime;
            set => SetAndNotify(_errorTime, value, () => _errorTime = value);
        }

        private bool _toBePaused = false;
        public bool ToBePaused
        {
            get => _toBePaused;
            set => SetAndNotify(_toBePaused, value, () => _toBePaused = value);
        }

        private bool _paused = false;
        public bool Paused
        {
            get => _paused;
            set => SetAndNotify(_paused, value, () => _paused = value);
        }

        private bool _canceled = false;
        public bool Canceled
        {
            get => _canceled;
            set => SetAndNotify(_canceled, value, () => _canceled = value);
        }

        private bool _canCancel = false;
        public bool CanCancel
        {
            get => _canCancel;
            set => SetAndNotify(_canCancel, value, () => _canCancel = value);
        }

        private byte _encodingProgress;
        public byte EncodingProgress
        {
            get => _encodingProgress;
            set => SetAndNotify(_encodingProgress, value, () => _encodingProgress = value);
        }

        private TimeSpan _elapsedEncodingTime = TimeSpan.Zero;
        public TimeSpan ElapsedEncodingTime
        {
            get => _elapsedEncodingTime;
            set => SetAndNotify(_elapsedEncodingTime, value, () => _elapsedEncodingTime = value);
        }

        private DateTime? _completedEncodingDateTime = null;
        public DateTime? CompletedEncodingDateTime
        {
            get => _completedEncodingDateTime;
            set => SetAndNotify(_completedEncodingDateTime, value, () => _completedEncodingDateTime = value);
        }

        private DateTime? _completedPostProcessingTime = null;
        public DateTime? CompletedPostProcessingTime
        {
            get => _completedPostProcessingTime;
            set => SetAndNotify(_completedPostProcessingTime, value, () => _completedPostProcessingTime = value);
        }

        private bool _complete = false;
        public bool Complete
        {
            get => _complete;
            set => SetAndNotify(_complete, value, () => _complete = value);
        }
        #endregion Status

        #region Processing Data        
        private SourceStreamDataClientModel _sourceStreamDataClientModel;
        public SourceStreamDataClientModel SourceStreamData
        {
            get => _sourceStreamDataClientModel;
            set => SetAndNotify(_sourceStreamDataClientModel, value, () => _sourceStreamDataClientModel = value);
        }

        private EncodingInstructions _encodingInstructions;
        public EncodingInstructions EncodingInstructions
        {
            get => _encodingInstructions;
            set => SetAndNotify(_encodingInstructions, value, () => _encodingInstructions = value);
        }

        private bool _needsPostProcessing;
        public bool NeedsPostProcessing
        {
            get => _needsPostProcessing;
            set => SetAndNotify(_needsPostProcessing, value, () => _needsPostProcessing = value);
        }

        private PostProcessingFlags _postProcessingFlags = PostProcessingFlags.None;
        public PostProcessingFlags PostProcessingFlags
        {
            get => _postProcessingFlags;
            set => SetAndNotify(_postProcessingFlags, value, () => _postProcessingFlags = value);
        }

        private PostProcessingSettings _postProcessingSettings;
        public PostProcessingSettings PostProcessingSettings
        {
            get => _postProcessingSettings;
            set => SetAndNotify(_postProcessingSettings, value, () => _postProcessingSettings = value);
        }

        private IEncodingCommandArguments _encodingCommandArguments;
        [JsonConverter(typeof(EncodingCommandArgumentsConverter<IEncodingCommandArguments>))]
        public IEncodingCommandArguments EncodingCommandArguments
        {
            get => _encodingCommandArguments;
            set => SetAndNotify(_encodingCommandArguments, value, () => _encodingCommandArguments = value);
        }
        #endregion Processing Data
        #endregion Properties

        public void Update(IEncodingJobData encodingJobData)
        {
            encodingJobData.CopyProperties(this);

            if (encodingJobData.SourceStreamData is not null)
            {
                if (SourceStreamData is null) SourceStreamData = new(encodingJobData.SourceStreamData);
                else SourceStreamData.Update(encodingJobData.SourceStreamData);

                OnPropertyChanged(nameof(SourceStreamData));
            }
        }

        #region Public Methods
        public async Task<bool> Cancel() => await CommunicationManager.CancelJob(Id);

        public async Task<bool> Pause() => await CommunicationManager.PauseJob(Id);

        public async Task<bool> Resume() => await CommunicationManager.ResumeJob(Id);

        public async Task<bool> CancelThenPause() => await CommunicationManager.CancelThenPauseJob(Id);

        public async Task<bool> Remove() => await CommunicationManager.RequestRemoveJob(Id);

        public override bool Equals(object obj)
        {
            if (obj is IEncodingJobData data)
            {
                return Id == data.Id;
            }

            return false;
        }

        public override int GetHashCode() => Id.GetHashCode();
        #endregion Public Methods

        #region Private Methods
        private void UpdateStatus(EncodingJobStatusUpdateData data)
        {
            data.CopyProperties(this);
        }

        private void UpdateProcessingData(EncodingJobProcessingDataUpdateData data)
        {
            data.CopyProperties(this);

            if (data.SourceStreamData is not null)
            {
                if (SourceStreamData is null) SourceStreamData = new(data.SourceStreamData);
                else SourceStreamData.Update(data.SourceStreamData);

                OnPropertyChanged(nameof(SourceStreamData));
            }
        }

        private void UpdateEncodingProgress(EncodingJobEncodingProgressUpdateData data)
        {
            data.CopyProperties(this);
        }
        #endregion Private Methods
    }
}
