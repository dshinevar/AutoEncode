using AutoEncodeClient.Collections;
using AutoEncodeUtilities.Data;
using System.Windows.Input;

namespace AutoEncodeClient.ViewModels.Interfaces
{
    public interface ISourceFilesViewModel
    {
        ObservableDictionary<string, BulkObservableCollection<SourceFileData>> MovieSourceFiles { get; }

        ObservableDictionary<string, ObservableDictionary<string, ObservableDictionary<string, BulkObservableCollection<ShowSourceFileData>>>> ShowSourceFiles { get; }

        ICommand RefreshSourceFilesCommand { get; }

        ICommand RequestEncodeCommand { get; }

        void RefreshSourceFiles();
    }
}
