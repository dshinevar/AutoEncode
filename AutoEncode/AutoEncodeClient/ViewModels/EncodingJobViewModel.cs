using AutoEncodeClient.Command;
using AutoEncodeClient.Dialogs;
using AutoEncodeClient.Enums;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.Models.StreamDataModels;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using System;
using System.Windows.Input;

namespace AutoEncodeClient.ViewModels
{
    public class EncodingJobViewModel :
        ViewModelBase<IEncodingJobClientModel>,
        IEncodingJobViewModel,
        IEquatable<IEncodingJobData>
    {
        public EncodingJobViewModel(IEncodingJobClientModel model)
            : base(model)
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

            SelectDetailsSectionCommand = new AECommandWithParameter(SelectDetailsSection);

            Model.PropertyChanged += ModelPropertyChanged;
        }

        public ulong Id => Model.Id;

        public string Title => Model.Title;

        public string Name => Model.Name;

        public string FileName => Model.FileName;

        public string SourceFullPath => Model.SourceFullPath;

        public string DestinationFullPath => Model.DestinationFullPath;

        #region Processing Data
        public SourceStreamDataClientModel SourceStreamData
        {
            get => Model.SourceStreamData;
            //set => SetAndNotify(Model.SourceStreamData, value, () => Model.SourceStreamData = value);
        }
        public PostProcessingSettings PostProcessingSettings
        {
            get => Model.PostProcessingSettings;
            /*set => SetAndNotify(Model.PostProcessingSettings, value, () =>
            {
                if (Model.PostProcessingSettings is null) Model.PostProcessingSettings = value;
                else Model.PostProcessingSettings.Update(value);
            });
            */
        }

        public IEncodingCommandArguments EncodingCommandArguments => Model.EncodingCommandArguments;
        #endregion Processing Data

        #region Status
        public EncodingJobStatus Status
        {
            get => Model.Status;
            //set => SetAndNotify(Model.Status, value, () => Model.Status = value);
        }

        public EncodingJobBuildingStatus BuildingStatus
        {
            get => Model.BuildingStatus;
            //set => SetAndNotify(Model.BuildingStatus, value, () => Model.BuildingStatus = value);
        }

        public byte EncodingProgress
        {
            get => Model.EncodingProgress;
            //set => SetAndNotify(Model.EncodingProgress, value, () => Model.EncodingProgress = value);
        }

        public bool HasError
        {
            get => Model.HasError;
            //set => SetAndNotify(Model.HasError, value, () => Model.HasError = value);
        }

        public bool ToBePaused
        {
            get => Model.ToBePaused;
            //set => SetAndNotify(Model.ToBePaused, value, () => Model.ToBePaused = value);
        }

        public bool Paused
        {
            get => Model.Paused;
            //set => SetAndNotify(Model.Paused, value, () => Model.Paused = value);
        }

        public bool Canceled
        {
            get => Model.Canceled;
            //set => SetAndNotify(Model.Cancelled, value, () => Model.Cancelled = value);
        }

        public bool CanCancel
        {
            get => Model.CanCancel;
            //set => SetAndNotify(Model.CanCancel, value, () => Model.CanCancel = value);
        }

        public string ErrorMessage
        {
            get => Model.ErrorMessage;
            //set => SetAndNotify(Model.ErrorMessage, value, () => Model.ErrorMessage = value);
        }

        public DateTime? ErrorTime
        {
            get => Model.ErrorTime;
            //set => SetAndNotify(Model.ErrorTime, value, () => Model.ErrorTime = value);
        }

        public TimeSpan ElapsedEncodingTime
        {
            get => Model.ElapsedEncodingTime;
            //set => SetAndNotify(Model.ElapsedEncodingTime, value, () => Model.ElapsedEncodingTime = value);
        }

        public DateTime? CompletedEncodingDateTime
        {
            get => Model.CompletedEncodingDateTime;
            //set => SetAndNotify(Model.CompletedEncodingDateTime, value, () => Model.CompletedEncodingDateTime = value);
        }

        public DateTime? CompletedPostProcessingTime
        {
            get => Model.CompletedPostProcessingTime;
            //set => SetAndNotify(Model.CompletedPostProcessingTime, value, () => Model.CompletedPostProcessingTime = value);
        }

        public bool Complete
        {
            get => Model.Complete;
            //set => SetAndNotify(Model.Complete, value, () => Model.Complete = value);
        }
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
                AEDialogHandler.ShowError($"Failed to cancel job for {FileName}", "Cancel Failed");
            }
        }

        private async void Pause()
        {
            bool success = await Model.Pause();
            if (success is false)
            {
                AEDialogHandler.ShowError($"Failed to pause job for {FileName}", "Pause Failed");
            }
        }

        private async void Resume()
        {
            bool success = await Model.Resume();
            if (success is false)
            {
                AEDialogHandler.ShowError($"Failed to resume job for {FileName}", "Resume Failed");
            }
        }

        private async void CancelThenPause()
        {
            bool success = await Model.CancelThenPause();
            if (success is false)
            {
                AEDialogHandler.ShowError($"Failed to cancel then pause job for {FileName}", "Cancel Then Pause Failed");
            }
        }

        private async void Remove()
        {
            bool success = await Model.Remove();
            if (success is false)
            {
                AEDialogHandler.ShowError($"Failed to remove job {FileName} from encoding queue.", "Removal Failed");
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
        public bool Equals(IEncodingJobData data) => Id == data.Id;
        #endregion Public Methods
    }
}
