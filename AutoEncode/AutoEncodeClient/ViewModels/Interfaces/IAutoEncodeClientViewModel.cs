using AutoEncodeClient.Collections;
using System;

namespace AutoEncodeClient.ViewModels.Interfaces
{
    public interface IAutoEncodeClientViewModel : IDisposable
    {
        ISourceFilesViewModel SourceFilesViewModel { get; }

        BulkObservableCollection<EncodingJobViewModel> EncodingJobs { get; }

        EncodingJobViewModel SelectedEncodingJobViewModel { get; set; }

        bool ConnectedToServer { get; }
    }
}
