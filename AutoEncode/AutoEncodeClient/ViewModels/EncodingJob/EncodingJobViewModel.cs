using AutoEncodeClient.Command;
using AutoEncodeClient.Dialogs;
using AutoEncodeClient.Enums;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.ViewModels.EncodingJob.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace AutoEncodeClient.ViewModels.EncodingJob;

public class EncodingJobViewModel :
    ViewModelBase<IEncodingJobClientModel>,
    IEncodingJobViewModel,
    IEquatable<EncodingJobData>
{
    public EncodingJobViewModel(IEncodingJobClientModel encodingJobClientModel)
        : base(encodingJobClientModel)
    {
        AECommand cancelCommand = new(() => CanCancel, Cancel);
        CancelCommand = cancelCommand;
        AddCommand(cancelCommand, nameof(CanCancel));

        AECommand pauseCommand = new(() => !ToBePaused, Pause);
        PauseCommand = pauseCommand;
        AddCommand(pauseCommand, nameof(ToBePaused));

        AECommand cancelThenPauseCommand = new(() => CanCancel, CancelThenPause);
        CancelThenPauseCommand = cancelThenPauseCommand;
        AddCommand(cancelThenPauseCommand, nameof(CanCancel));

        ResumeCommand = new AECommand(Resume);

        RemoveCommand = new AECommand(Remove);

        SelectDetailsSectionCommand = new AECommand(SelectDetailsSection);

        if (Model.SourceStreamData is not null)
        {
            SourceStreamData = new SourceStreamDataViewModel(Model.SourceStreamData);
            RegisterChildViewModel(SourceStreamData);
        }
    }

    protected override void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SourceStreamData):
            {
                if (Model.SourceStreamData is not null)
                {
                    if (SourceStreamData is null)
                    {
                        SourceStreamData = new SourceStreamDataViewModel(Model.SourceStreamData);
                        RegisterChildViewModel(SourceStreamData);
                    }
                    else
                    {
                        SourceStreamData.Update(Model.SourceStreamData);
                    }
                }

                break;
            }
        }

        base.ModelPropertyChanged(sender, e);
    }

    public ulong Id => Model.Id;

    public string Title => Model.Title;

    public string Name => Model.Name;

    public string FileName => Model.FileName;

    public string SourceFullPath => Model.SourceFullPath;

    public string DestinationFullPath => Model.DestinationFullPath;

    #region Processing Data
    private ISourceStreamDataViewModel _sourceStreamData;
    public ISourceStreamDataViewModel SourceStreamData
    {
        get => _sourceStreamData;
        set => SetAndNotify(_sourceStreamData, value, () => _sourceStreamData = value);
    }

    public PostProcessingSettings PostProcessingSettings => Model.PostProcessingSettings;

    public EncodingCommandArguments EncodingCommandArguments => Model.EncodingCommandArguments;
    #endregion Processing Data

    #region Status
    public EncodingJobStatus Status => Model.Status;

    public EncodingJobBuildingStatus BuildingStatus => Model.BuildingStatus;

    public byte EncodingProgress => Model.EncodingProgress;

    public bool HasError => Model.HasError;

    public bool ToBePaused => Model.ToBePaused;

    public bool Paused => Model.Paused;

    public bool Canceled => Model.Canceled;

    public bool CanCancel => Model.CanCancel;

    public string ErrorMessage => Model.ErrorMessage;

    public DateTime? ErrorTime => Model.ErrorTime;

    public double? CurrentFramesPerSecond => Model.CurrentFramesPerSecond;

    public TimeSpan? EstimatedEncodingTimeRemaining => Model.EstimatedEncodingTimeRemaining;

    public TimeSpan ElapsedEncodingTime => Model.ElapsedEncodingTime;

    public DateTime? CompletedEncodingDateTime => Model.CompletedEncodingDateTime;

    public DateTime? CompletedPostProcessingTime => Model.CompletedPostProcessingTime;

    public bool Complete => Model.Complete;
    #endregion Status

    #region Other Properties
    private EncodingJobDetailsSection _selectedDetailsSection = EncodingJobDetailsSection.None;
    public EncodingJobDetailsSection SelectedDetailsSection
    {
        get => _selectedDetailsSection;
        set => SetAndNotify(_selectedDetailsSection, value, () => _selectedDetailsSection = value);
    }
    #endregion Other Properties

    #region Commands
    public ICommand CancelCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ResumeCommand { get; }
    public ICommand CancelThenPauseCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand SelectDetailsSectionCommand { get; }
    #endregion Commands

    #region Command Methods
    private async void Cancel()
    {
        bool success = await Model.Cancel();
        if (success is false)
        {
            UserMessageDialogHandler.ShowDialog($"Failed to cancel job for {FileName}", "Cancel Failed", UserMessageDialogButtons.Ok, Severity.ERROR, null);
        }
    }

    private async void Pause()
    {
        bool success = await Model.Pause();
        if (success is false)
        {
            UserMessageDialogHandler.ShowDialog($"Failed to pause job for {FileName}", "Pause Failed", UserMessageDialogButtons.Ok, Severity.ERROR, null);
        }
    }

    private async void Resume()
    {
        bool success = await Model.Resume();
        if (success is false)
        {
            UserMessageDialogHandler.ShowDialog($"Failed to resume job for {FileName}", "Resume Failed", UserMessageDialogButtons.Ok, Severity.ERROR, null);
        }
    }

    private async void CancelThenPause()
    {
        bool success = await Model.CancelThenPause();
        if (success is false)
        {
            UserMessageDialogHandler.ShowDialog($"Failed to cancel then pause job for {FileName}", "Cancel Then Pause Failed", UserMessageDialogButtons.Ok, Severity.ERROR, null);
        }
    }

    private async void Remove()
    {
        bool success = await Model.Remove();
        if (success is false)
        {
            UserMessageDialogHandler.ShowDialog($"Failed to remove job {FileName} from encoding queue.", "Removal Failed", UserMessageDialogButtons.Ok, Severity.ERROR, null);
        }
    }

    private void SelectDetailsSection(object obj)
    {
        if (obj is EncodingJobDetailsSection encodingJobDetailsSection)
        {
            SelectedDetailsSection = encodingJobDetailsSection;
        }
    }
    #endregion Command Methods

    #region Public Methods
    public IEncodingJobClientModel GetModel() => Model;

    public bool Equals(EncodingJobData data) => Id == data.Id;
    #endregion Public Methods
}
