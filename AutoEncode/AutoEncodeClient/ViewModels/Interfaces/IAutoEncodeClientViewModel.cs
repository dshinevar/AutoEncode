using AutoEncodeClient.Collections;

namespace AutoEncodeClient.ViewModels.Interfaces;

public interface IAutoEncodeClientViewModel
{
    void Initialize();

    ISourceFilesViewModel SourceFilesViewModel { get; }

    BulkObservableCollection<IEncodingJobViewModel> EncodingJobs { get; }

    IEncodingJobViewModel SelectedEncodingJobViewModel { get; set; }
}
