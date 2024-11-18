using AutoEncodeClient.Communication.Interfaces;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Base;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoEncodeClient.Models;

public class EncodingJobClientModel :
    ModelBase,
    IEncodingJobClientModel,
    IDisposable
{
    #region Dependencies
    public ICommunicationMessageHandler CommunicationMessageHandler { get; set; }

    public IClientUpdateSubscriber ClientUpdateSubscriber { get; set; }
    #endregion Dependencies

    public EncodingJobClientModel(EncodingJobData encodingJobData)
    {
        encodingJobData.CopyProperties(this);
    }

    public void Initialize()
    {
        IEnumerable<string> topics =
        [
            $"{nameof(ClientUpdateType.EncodingJobStatus)}-{Id}",
            $"{nameof(ClientUpdateType.EncodingJobProcessingData)}-{Id}",
            $"{nameof(ClientUpdateType.EncodingJobEncodingProgress)}-{Id}"
        ];

        ClientUpdateSubscriber.Initialize();
        ClientUpdateSubscriber.ClientUpdateMessageReceived += ClientUpdateSubscriber_ClientUpdateMessageReceived;

        ClientUpdateSubscriber.Subscribe(topics);
        ClientUpdateSubscriber.Start();
    }

    public void Dispose()
    {
        ClientUpdateSubscriber.ClientUpdateMessageReceived -= ClientUpdateSubscriber_ClientUpdateMessageReceived;
    }

    private void ClientUpdateSubscriber_ClientUpdateMessageReceived(object sender, CommunicationMessage<ClientUpdateType> e)
    {
        switch (e.Type)
        {
            case ClientUpdateType.EncodingJobStatus:
            {
                EncodingJobStatusUpdateData updateData = e.UnpackData<EncodingJobStatusUpdateData>();
                updateData?.CopyProperties(this);
                break;
            }
            case ClientUpdateType.EncodingJobProcessingData:
            {
                EncodingJobProcessingDataUpdateData updateData = e.UnpackData<EncodingJobProcessingDataUpdateData>();
                updateData?.CopyProperties(this);
                break;
            }
            case ClientUpdateType.EncodingJobEncodingProgress:
            {
                EncodingJobEncodingProgressUpdateData updateData = e.UnpackData<EncodingJobEncodingProgressUpdateData>();
                updateData?.CopyProperties(this);
                break;
            }
        }
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

    private double? _currentFramesPerSecond = null;
    public double? CurrentFramesPerSecond
    {
        get => _currentFramesPerSecond;
        set => SetAndNotify(_currentFramesPerSecond, value, () => _currentFramesPerSecond = value);
    }

    private TimeSpan? _estimatedEncodingTimeRemaining = null;
    public TimeSpan? EstimatedEncodingTimeRemaining
    {
        get => _estimatedEncodingTimeRemaining;
        set => SetAndNotify(_estimatedEncodingTimeRemaining, value, () => _estimatedEncodingTimeRemaining = value);
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
    private SourceStreamData _sourceStreamData;
    public SourceStreamData SourceStreamData
    {
        get => _sourceStreamData;
        set => SetAndNotify(_sourceStreamData, value, () => _sourceStreamData = value);
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

    private EncodingCommandArguments _encodingCommandArguments;
    public EncodingCommandArguments EncodingCommandArguments
    {
        get => _encodingCommandArguments;
        set => SetAndNotify(_encodingCommandArguments, value, () => _encodingCommandArguments = value);
    }
    #endregion Processing Data
    #endregion Properties

    #region Public Methods
    public async Task<bool> Cancel() => await CommunicationMessageHandler.RequestCancelJob(Id);

    public async Task<bool> Pause() => await CommunicationMessageHandler.RequestPauseJob(Id);

    public async Task<bool> Resume() => await CommunicationMessageHandler.RequestResumeJob(Id);

    public async Task<bool> CancelThenPause() => await CommunicationMessageHandler.RequestPauseAndCancelJob(Id);

    public async Task<bool> Remove() => await CommunicationMessageHandler.RequestRemoveJob(Id);
    #endregion Public Methods
}
