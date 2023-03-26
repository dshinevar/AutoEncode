using AutoEncodeClient.Models;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeClient.ViewModels
{
    public class EncodingJobViewModel : ViewModelBase<EncodingJobClientModel>
    {
        public EncodingJobViewModel(EncodingJobClientModel model)
            : base(model) { }

        public int Id => Model.Id;

        public string Name => Model.Name;

        public string FileName => Model.FileName;

        public string SourceFullPath => Model.SourceFullPath;

        public string DestinationFullPath => Model.DestinationFullPath;

        public EncodingJobStatus Status
        {
            get => Model.Status;
            set 
            {
                if (value != Model.Status)
                {
                    Model.Status = value;
                    OnPropertyChanged();
                }
            }
        }

        public int EncodingProgress
        {
            get => Model.EncodingProgress;
            set
            {
                if (value != Model.EncodingProgress)
                {
                    Model.EncodingProgress = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
