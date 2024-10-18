using AutoEncodeClient.Enums;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using System;
using System.Windows.Input;

namespace AutoEncodeClient.ViewModels.Interfaces;

public interface IEncodingJobViewModel
{
    IEncodingJobClientModel Model { get; }

    ulong Id { get; }

    string Title { get; }

    string Name { get; }

    string FileName { get; }

    string SourceFullPath { get; }

    string DestinationFullPath { get; }

    #region Processing Data
    SourceStreamDataViewModel SourceStreamData { get; }

    PostProcessingSettings PostProcessingSettings { get; }

    IEncodingCommandArguments EncodingCommandArguments { get; }
    #endregion Processing Data

    #region Status
    EncodingJobStatus Status { get; }

    EncodingJobBuildingStatus BuildingStatus { get; }

    byte EncodingProgress { get; }

    bool HasError { get; }

    bool ToBePaused { get; }

    bool Paused { get; }

    bool Canceled { get; }

    bool CanCancel { get; }

    string ErrorMessage { get; }

    DateTime? ErrorTime { get; }

    double? CurrentFramesPerSecond { get; }

    TimeSpan? EstimatedEncodingTimeRemaining { get; }

    TimeSpan ElapsedEncodingTime { get; }

    DateTime? CompletedEncodingDateTime { get; }

    DateTime? CompletedPostProcessingTime { get; }

    bool Complete { get; }
    #endregion Status

    #region Commands
    ICommand CancelCommand { get; }

    ICommand PauseCommand { get; }

    ICommand ResumeCommand { get; }

    ICommand CancelThenPauseCommand { get; }

    ICommand RemoveCommand { get; }

    ICommand SelectDetailsSectionCommand { get; }
    #endregion Commands

    EncodingJobDetailsSection SelectedDetailsSection { get; set; }
}
