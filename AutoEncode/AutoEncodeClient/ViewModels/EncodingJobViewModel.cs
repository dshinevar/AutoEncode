using AutoEncodeClient.Models;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using System;

namespace AutoEncodeClient.ViewModels
{
    public class EncodingJobViewModel :
        ViewModelBase<EncodingJobClientModel>,
        IUpdateable<IEncodingJobData>,
        IEquatable<IEncodingJobData>
    {
        public EncodingJobViewModel(EncodingJobClientModel model)
            : base(model) { }

        public int Id => Model.Id;

        public string Name => Model.Name;

        public string FileName => Model.FileName;

        public string SourceFullPath => Model.SourceFullPath;

        public string DestinationFullPath => Model.DestinationFullPath;

        #region Processing Data
        public SourceStreamData SourceStreamData
        {
            get => Model.SourceStreamData;
            set => SetAndNotify(Model.SourceStreamData, value, () =>
            {
                if (Model.SourceStreamData is null) Model.SourceStreamData = value;
                else Model.SourceStreamData.Update(value);
            });
        }

        public PostProcessingSettings PostProcessingSettings
        {
            get => Model.PostProcessingSettings;
            set => SetAndNotify(Model.PostProcessingSettings, value, () =>
            {
                if (Model.PostProcessingSettings is null) Model.PostProcessingSettings = value;
                else Model.PostProcessingSettings.Update(value);
            });
        }
        #endregion Processing Data

        #region Status
        public EncodingJobStatus Status
        {
            get => Model.Status;
            set => SetAndNotify(Model.Status, value, () => Model.Status = value);
        }

        public int EncodingProgress
        {
            get => Model.EncodingProgress;
            set => SetAndNotify(Model.EncodingProgress, value, () => Model.EncodingProgress = value);
        }

        public bool Error
        {
            get => Model.Error;
            set => SetAndNotify(Model.Error, value, () => Model.Error = value);
        }

        public string LastErrorMessage
        {
            get => Model.LastErrorMessage;
            set => SetAndNotify(Model.LastErrorMessage, value, () => Model.LastErrorMessage = value);
        }

        public DateTime? ErrorTime
        {
            get => Model.ErrorTime;
            set => SetAndNotify(Model.ErrorTime, value, () => Model.ErrorTime = value);
        }

        public TimeSpan? ElapsedEncodingTime
        {
            get => Model.ElapsedEncodingTime;
            set => SetAndNotify(Model.ElapsedEncodingTime, value, () => Model.ElapsedEncodingTime = value);
        }

        public DateTime? CompletedEncodingDateTime
        {
            get => Model.CompletedEncodingDateTime;
            set => SetAndNotify(Model.CompletedEncodingDateTime, value, () => Model.CompletedEncodingDateTime = value);
        }

        public DateTime? CompletedPostProcessingTime
        {
            get => Model.CompletedPostProcessingTime;
            set => SetAndNotify(Model.CompletedPostProcessingTime, value, () => Model.CompletedPostProcessingTime = value);
        }
        #endregion Status

        public void Update(IEncodingJobData updatedData) => updatedData.CopyProperties(this);

        public bool Equals(IEncodingJobData data) => Id == data.Id;
    }
}
