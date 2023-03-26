using AutoEncodeClient.ViewModels;
using AutoEncodeUtilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeClient.Interfaces
{
    public interface IAutoEncodeClientViewModel
    {
        BulkObservableCollection<EncodingJobViewModel> EncodingJobs { get; }

        ObservableDictionary<string, int> Dictionary { get; set; }

        EncodingJobViewModel SelectedEncodingJobViewModel { get; set; }
    }
}
