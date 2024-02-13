using AutoEncodeClient.Collections;
using AutoEncodeClient.Models.Interfaces;

namespace AutoEncodeClient.ViewModels.Interfaces
{
    public interface IAutoEncodeClientViewModel
    {
        void Initialize(IAutoEncodeClientModel model);

        ISourceFilesViewModel SourceFilesViewModel { get; }

        BulkObservableCollection<IEncodingJobViewModel> EncodingJobs { get; }

        IEncodingJobViewModel SelectedEncodingJobViewModel { get; set; }
    }
}
