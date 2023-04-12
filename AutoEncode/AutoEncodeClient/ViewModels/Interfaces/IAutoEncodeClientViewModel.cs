using AutoEncodeClient.ViewModels;
using AutoEncodeUtilities.Collections;
using AutoEncodeUtilities.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
