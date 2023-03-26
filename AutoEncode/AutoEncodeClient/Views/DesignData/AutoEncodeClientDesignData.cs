using AutoEncodeClient.Interfaces;
using AutoEncodeClient.Models;
using AutoEncodeClient.ViewModels;
using AutoEncodeUtilities.Collections;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeClient.Views.DesignData
{
    public class AutoEncodeClientDesignData : IAutoEncodeClientViewModel
    {
        public AutoEncodeClientDesignData() 
        {
            EncodingJobs = new BulkObservableCollection<EncodingJobViewModel>();
            var encodingJobData1 = new EncodingJobData()
            {
                Id = 1,
                SourceFullPath = "M:/Movies/Very Evil Alive (2025).mkv",
                DestinationFullPath = "M:/Movies (Encoded)/Very Evil Alive (2025).mkv",
                Status = EncodingJobStatus.ENCODING,
                EncodingProgress = 50
            };
            var encodingJobData2 = new EncodingJobData()
            {
                Id = 2,
                SourceFullPath = "M:/Movies/Big Big Big Little Big Explosion (2050).mkv",
                DestinationFullPath = "M:/Movies (Encoded)/Big Big Big Little Big Explosion (2050).mkv",
                Status = EncodingJobStatus.BUILT,
                EncodingProgress = 0
            };
            var encodingJobData4 = new EncodingJobData()
            {
                Id = 4,
                SourceFullPath = "M:/Movies/Halloween - Michael's Birthday Party (2030).mkv",
                DestinationFullPath = "M:/Movies (Encoded)/Halloween - Michael's Birthday Party (2030).mkv",
                Status = EncodingJobStatus.BUILDING,
                EncodingProgress = 0
            };
            var encodingJobData3 = new EncodingJobData()
            {
                Id = 3,
                SourceFullPath = "M:/Movies/Knifin Around (1984).mkv",
                DestinationFullPath = "M:/Movies (Encoded)/Knifin Around (1984).mkv",
                Status = EncodingJobStatus.NEW,
                EncodingProgress = 0
            };

            var encodingJobClientModel1 = new EncodingJobClientModel(encodingJobData1);
            var encodingJobClientModel2 = new EncodingJobClientModel(encodingJobData2);
            var encodingJobClientModel4 = new EncodingJobClientModel(encodingJobData4);
            var encodingJobClientModel3 = new EncodingJobClientModel(encodingJobData3);

            var encodingJobViewModel1 = new EncodingJobViewModel(encodingJobClientModel1);
            var encodingJobViewModel2 = new EncodingJobViewModel(encodingJobClientModel2);
            var encodingJobViewModel4 = new EncodingJobViewModel(encodingJobClientModel4);
            var encodingJobViewModel3 = new EncodingJobViewModel(encodingJobClientModel3);

            EncodingJobs.Add(encodingJobViewModel1);
            EncodingJobs.Add(encodingJobViewModel2);
            EncodingJobs.Add(encodingJobViewModel4);
            EncodingJobs.Add(encodingJobViewModel3);

            SelectedEncodingJobViewModel = encodingJobViewModel2;
        }

        #region Properties
        public BulkObservableCollection<EncodingJobViewModel> EncodingJobs { get; }
        public EncodingJobViewModel SelectedEncodingJobViewModel { get; set; }
        #endregion Properties
    }
}
