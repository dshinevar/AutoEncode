using AutoEncodeClient.Collections;
using AutoEncodeUtilities.Data;
using System;
using System.Windows.Input;

namespace AutoEncodeClient.ViewModels.Interfaces
{
    public interface IAutoEncodeClientViewModel : IDisposable
    {
        ICommand RefreshSourceFilesCommand { get; }

        BulkObservableCollection<EncodingJobViewModel> EncodingJobs { get; }

        EncodingJobViewModel SelectedEncodingJobViewModel { get; set; }

        ObservableDictionary<string, BulkObservableCollection<SourceFileData>> MovieSourceFiles { get; }

        ObservableDictionary<string, ObservableDictionary<string, ObservableDictionary<string, BulkObservableCollection<ShowSourceFileData>>>> ShowSourceFiles { get; }

        bool ConnectedToServer { get; }
    }
}
