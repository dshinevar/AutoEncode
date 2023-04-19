using AutoEncodeClient.Collections;
using AutoEncodeUtilities.Data;
using System.Windows.Input;

namespace AutoEncodeClient.ViewModels.Interfaces
{
    public interface IAutoEncodeClientViewModel
    {
        ICommand RefreshSourceFilesCommand { get; }

        BulkObservableCollection<EncodingJobViewModel> EncodingJobs { get; }

        EncodingJobViewModel SelectedEncodingJobViewModel { get; set; }

        ObservableDictionary<string, BulkObservableCollection<VideoSourceData>> MovieSourceFiles { get; }

        ObservableDictionary<string, BulkObservableCollection<ShowSourceData>> ShowSourceFiles { get; }
    }
}
