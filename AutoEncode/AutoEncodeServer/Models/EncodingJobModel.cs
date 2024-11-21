using AutoEncodeServer.Communication;
using AutoEncodeServer.Communication.Interfaces;
using AutoEncodeServer.Models.Data;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Base;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace AutoEncodeServer.Models;

public partial class EncodingJobModel :
    ModelBase,
    IEncodingJobModel
{
    #region Dependencies
    public ILogger Logger { get; set; }

    public IClientUpdatePublisher ClientUpdatePublisher { get; set; }
    #endregion Dependencies

    #region Events
    public event EventHandler<EncodingJobStatusChangedEventArgs> EncodingJobStatusChanged;
    #endregion Events

    #region Properties
    public ulong Id { get; }

    public Guid SourceFileGuid { get; }

    private string _title = string.Empty;
    public string Title
    {
        get => string.IsNullOrWhiteSpace(_title) ? Name : _title;
        set => _title = value;
    }

    public string Name { get; }

    public string Filename { get; }

    public string SourceFullPath { get; }

    public string DestinationFullPath { get; }

    #endregion Properties

    #region Status Properties
    private EncodingJobStatus _status;
    public EncodingJobStatus Status
    {
        get => _status;
        set => SetAndAct(_status, value, () => _status = value, () =>
        {
            SendStatusUpdate();
            EncodingJobStatusChanged?.Invoke(this, new EncodingJobStatusChangedEventArgs() { Status = _status });
        });
    }

    private EncodingJobBuildingStatus _buildingStatus = EncodingJobBuildingStatus.BUILDING;
    public EncodingJobBuildingStatus BuildingStatus
    {
        get => _buildingStatus;
        set => SetAndAct(_buildingStatus, value, () => _buildingStatus = value, SendStatusUpdate);
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

    public double? CurrentFramesPerSecond { get; set; }

    public TimeSpan? EstimatedEncodingTimeRemaining { get; set; } = null;

    public TimeSpan ElapsedEncodingTime { get; set; } = TimeSpan.Zero;

    public DateTime? CompletedEncodingDateTime { get; set; } = null;

    public DateTime? CompletedPostProcessingTime { get; set; } = null;
    #endregion Encoding Progress Properties

    #region Processing Data
    private SourceStreamData _sourceStreamData;
    public SourceStreamData SourceStreamData
    {
        get => _sourceStreamData;
        set => SetAndAct(_sourceStreamData, value, () => _sourceStreamData = value, SendProcessingDataUpdate);
    }

    private EncodingInstructions _encodingInstructions = null;
    public EncodingInstructions EncodingInstructions
    {
        get => _encodingInstructions;
        set => SetAndAct(_encodingInstructions, value, () => _encodingInstructions = value, SendProcessingDataUpdate);
    }

    public PostProcessingSettings PostProcessingSettings { get; set; }

    public PostProcessingFlags PostProcessingFlags { get; set; } = PostProcessingFlags.None;

    public bool NeedsPostProcessing => !PostProcessingFlags.Equals(PostProcessingFlags.None) && PostProcessingSettings is not null;

    private EncodingCommandArguments _encodingCommandArguments;
    public EncodingCommandArguments EncodingCommandArguments
    {
        get => _encodingCommandArguments;
        set => SetAndAct(_encodingCommandArguments, value, () => _encodingCommandArguments = value, SendProcessingDataUpdate);
    }

    public CancellationTokenSource TaskCancellationTokenSource { get; set; }
    #endregion Processing Data

    /// <summary>Default Constructor</summary>
    public EncodingJobModel() { }

    /// <summary>Factory Constructor </summary>
    /// <param name="id">Id of the Job</param>
    /// <param name="sourceFileGuid">Links encoding job to a source file. </param>
    /// <param name="sourceFileFullPath">Full Path of the source file</param>
    /// <param name="destinationFileFullPath">Full Path of the expected destination file.</param>
    /// <param name="postProcessingSettings"><see cref="PostProcessingSettings"/> of the job</param>
    public EncodingJobModel(ulong id, Guid sourceFileGuid, string sourceFileFullPath, string destinationFileFullPath, PostProcessingSettings postProcessingSettings)
    {
        Id = id;
        SourceFileGuid = sourceFileGuid;
        SourceFullPath = sourceFileFullPath;
        DestinationFullPath = destinationFileFullPath;
        Filename = Path.GetFileName(sourceFileFullPath);
        Name = Path.GetFileNameWithoutExtension(Filename);
        PostProcessingSettings = postProcessingSettings;
        SetPostProcessingFlags();
    }

    #region ===== Public Methods =====

    #region == Action Methods ==
    public void Cancel()
    {
        if (CanCancel is true)
        {
            if (Canceled is false)
            {
                TaskCancellationTokenSource?.Cancel();
                SendStatusUpdate();
            }
        }
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

        SendStatusUpdate();
    }

    public void Resume()
    {
        Paused = false;
        ToBePaused = false;

        SendStatusUpdate();
    }
    #endregion == Action Methods ==

    public void CleanupJob()
    {
        if (Canceled is true)
        {
            ResetCancel();
        }

        // If complete, no point in pausing, just "resume"
        if (Complete is false)
        {
            if (ToBePaused is true)
            {
                Pause();
            }
        }
        else
        {
            Resume();
        }
    }

    public EncodingJobData ToEncodingJobData()
    {
        EncodingJobData encodingJobData = new();
        this.CopyProperties(encodingJobData);
        return encodingJobData;
    }

    public override string ToString() => $"({Id}) - {Filename}";
    #endregion ===== Public Methods =====

    #region ===== Private Methods =====

    #region == Status Methods ==
    private void ResetCancel()
    {
        TaskCancellationTokenSource = null;
        ResetStatus();
    }

    private void SetError(string errorMessage, Exception ex = null)
    {
        HasError = true;
        ErrorTime = DateTime.Now;

        StringBuilder sb = new(errorMessage);
        if (ex is not null)
        {

            sb.AppendLine().AppendLine(ex.Message);

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

    private void ResetStatus()
    {
        if (Status > EncodingJobStatus.NEW)
        {
            if (Status.Equals(EncodingJobStatus.ENCODING))
            {
                CompletedEncodingDateTime = null;
                ResetEncodingProgress();
            }

            Status -= 1;
        }
    }

    #endregion == Status Methods ==

    #region == Encoding Progress Methods ==
    private void ResetEncodingProgress()
    {
        EncodingProgress = 0;
        EstimatedEncodingTimeRemaining = null;
        CurrentFramesPerSecond = null;
        ElapsedEncodingTime = TimeSpan.Zero;
    }

    private void UpdateEncodingProgress(byte? encodingProgress, int? estimatedSecondsRemaining, double? currentFps, TimeSpan? timeElapsed)
    {
        // If progress is null, just don't update it
        if (encodingProgress is byte progressByte)
        {
            if (progressByte > 100) EncodingProgress = 100;
            else if (progressByte < 0) EncodingProgress = 0;
            else EncodingProgress = progressByte;
        }

        if (estimatedSecondsRemaining is int estimatedSecondsRemainingInt)
        {
            EstimatedEncodingTimeRemaining = TimeSpan.FromSeconds(estimatedSecondsRemainingInt);
        }

        if (currentFps is double currentFpsDouble)
        {
            CurrentFramesPerSecond = currentFpsDouble;
        }

        if (timeElapsed is TimeSpan actualTimeElapsed)
        {
            ElapsedEncodingTime = actualTimeElapsed;
        }

        SendEncodingProgressUpdate();
    }

    private void CompleteEncoding(TimeSpan timeElapsed)
    {
        CompletedEncodingDateTime = DateTime.Now;
        UpdateEncodingProgress(100, 0, 0, timeElapsed);
        Status = EncodingJobStatus.ENCODED;
    }

    private void CompletePostProcessing()
    {
        CompletedPostProcessingTime = DateTime.Now;
        Status = EncodingJobStatus.POST_PROCESSED;
        SendEncodingProgressUpdate();
    }
    #endregion == Encoding Progress Methods ==

    #region == Processing Data Methods ==
    private void SetSourceScanType(VideoScanType scanType)
    {
        if (SourceStreamData?.VideoStream is not null)
        {
            SourceStreamData.VideoStream.ScanType = scanType;
            SendProcessingDataUpdate();
        }
    }

    private void SetSourceCrop(string crop)
    {
        if (SourceStreamData?.VideoStream is not null)
        {
            SourceStreamData.VideoStream.Crop = crop;
            SendProcessingDataUpdate();
        }
    }

    private void AddSourceHDRMetadataFilePath(HDRFlags hdrFlag, string hdrMetadataFilePath)
    {
        if (SourceStreamData?.VideoStream is not null && SourceStreamData.VideoStream.HasDynamicHDR is true)
        {
            if (SourceStreamData.VideoStream.HDRData.DynamicMetadataFullPaths.TryAdd(hdrFlag, hdrMetadataFilePath) is true)
            {
                SendProcessingDataUpdate();
            }
        }
    }
    #endregion == Processing Data Methods ==

    #region == PostProcessing Methods ==
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

    #endregion == PostProcessing Methods ==

    #region == Send Update Methods ==
    private void SendStatusUpdate()
    {
        EncodingJobStatusUpdateData statusUpdateData = new();
        this.CopyProperties(statusUpdateData);
        (string topic, CommunicationMessage<ClientUpdateType> message) = ClientUpdateMessageFactory.CreateEncodingJobStatusUpdate(Id, statusUpdateData);
        ClientUpdatePublisher.AddClientUpdateRequest(topic, message);
    }

    private void SendProcessingDataUpdate()
    {
        EncodingJobProcessingDataUpdateData processingDataUpdate = new();
        this.CopyProperties(processingDataUpdate);
        (string topic, CommunicationMessage<ClientUpdateType> message) = ClientUpdateMessageFactory.CreateEncodingJobProcessingDataUpdate(Id, processingDataUpdate);
        ClientUpdatePublisher.AddClientUpdateRequest(topic, message);
    }

    private void SendEncodingProgressUpdate()
    {
        EncodingJobEncodingProgressUpdateData encodingProgressUpdateData = new();
        this.CopyProperties(encodingProgressUpdateData);
        (string topic, CommunicationMessage<ClientUpdateType> message) = ClientUpdateMessageFactory.CreateEncodingJobEncodingProgressUpdate(Id, encodingProgressUpdateData);
        ClientUpdatePublisher.AddClientUpdateRequest(topic, message);
    }
    #endregion == Send Update Methods ==

    #endregion ===== Private Methods =====
}
