using AutoEncodeClient.Collections;
using AutoEncodeClient.Models.Interfaces;
using System;

namespace AutoEncodeClient.ViewModels.Interfaces
{
    public interface IAutoEncodeClientViewModel : IDisposable
    {
        void Initialize(IAutoEncodeClientModel model);

        ISourceFilesViewModel SourceFilesViewModel { get; }

        BulkObservableCollection<IEncodingJobViewModel> EncodingJobs { get; }

        IEncodingJobViewModel SelectedEncodingJobViewModel { get; set; }
    }
}
