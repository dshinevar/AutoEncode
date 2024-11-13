using AutoEncodeClient.Models;
using AutoEncodeClient.ViewModels;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeClient.ViewModels.SourceFile;
using AutoEncodeUtilities.Data;
using System;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Input;

namespace AutoEncodeClient.Views.DesignData
{
    public class AutoEncodeClientDesignData : ViewModelBase, IAutoEncodeClientViewModel
    {
        public AutoEncodeClientDesignData()
        {
            ObservableCollection<SourceFileViewModel> files = [];
            files.Add(new(new SourceFileClientModel(new SourceFileData()
            {
                Filename = "Test Source File Movie 1.mkv"
            })));

            SourceFilesViewModel = new SourceFilesViewModel();
            SourceFilesDirectoryViewModel directoryViewModel1 = new("Test Source Files 1")
            {
                SubdirectoriesAndFiles = new CompositeCollection()
                {
                    files
                }
            };
            SourceFilesViewModel.SourceFiles.Add("Test Source Files 1", directoryViewModel1);

            SourceFilesDirectoryViewModel directoryViewModel2 = new("Test Source Files 2")
            {
                SubdirectoriesAndFiles = new CompositeCollection()
                {
                    files
                }
            };

            SourceFilesViewModel.SourceFiles.Add("Test Source Files 2", directoryViewModel2);

            EncodingJobQueueViewModel = new EncodingJobQueueViewModel();
        }

        #region Properties
        public ISourceFilesViewModel SourceFilesViewModel { get; set; }

        public IEncodingJobQueueViewModel EncodingJobQueueViewModel { get; set; }

        public ICommand RefreshSourceFilesCommand { get; }

        public void Dispose() => throw new NotImplementedException();

        public void Initialize() => throw new NotImplementedException();
        #endregion Properties
    }
}
