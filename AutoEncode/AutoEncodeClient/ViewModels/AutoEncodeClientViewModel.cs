using AutoEncodeClient.Collections;
using AutoEncodeClient.Comm;
using AutoEncodeClient.Command;
using AutoEncodeClient.Config;
using AutoEncodeClient.Models;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AutoEncodeClient.ViewModels
{
    public class AutoEncodeClientViewModel :
        ViewModelBase<AutoEncodeClientModel>,
        IAutoEncodeClientViewModel,
        IDisposable
    {
        private readonly ClientUpdateService ClientUpdateService;
        private readonly CommunicationManager CommunicationManager;

        private Task RefreshSourceFilesTask { get; set; }

        public AutoEncodeClientViewModel(AutoEncodeClientModel model, ILogger logger, CommunicationManager communicationManager, AEClientConfig config)
            : base(model)
        {
            CommunicationManager = communicationManager;

            AECommand refreshSourceFilesCommand = new(RefreshSourceFiles);
            RefreshSourceFilesCommand = refreshSourceFilesCommand;

            RefreshSourceFiles();

            ClientUpdateService = new ClientUpdateService(logger, config.ConnectionSettings.IPAddress, config.ConnectionSettings.ClientUpdatePort);
            ClientUpdateService.DataReceived += (s, data) => UpdateClient(data);
            ClientUpdateService.Start();
        }

        #region Commands
        public ICommand RefreshSourceFilesCommand { get; }
        #endregion Commands

        #region Properties
        public BulkObservableCollection<EncodingJobViewModel> EncodingJobs { get; } = new BulkObservableCollection<EncodingJobViewModel>();
        public ObservableDictionary<string, BulkObservableCollection<VideoSourceData>> MovieSourceFiles { get; }
            = new ObservableDictionary<string, BulkObservableCollection<VideoSourceData>>();
        public ObservableDictionary<string, BulkObservableCollection<ShowSourceData>> ShowSourceFiles { get; }
            = new ObservableDictionary<string, BulkObservableCollection<ShowSourceData>>();

        private EncodingJobViewModel _selectedEncodingJobViewModel = null;
        public EncodingJobViewModel SelectedEncodingJobViewModel
        {
            get => _selectedEncodingJobViewModel;
            set => SetAndNotify(_selectedEncodingJobViewModel, value, () => _selectedEncodingJobViewModel = value);
        }
        public bool ConnectedToServer => Model.ConnectedToServer;
        #endregion Properties

        private void UpdateClient(List<EncodingJobData> encodingJobQueue)
        {
            if (encodingJobQueue is not null)
            {
                if (encodingJobQueue.Any() is false)
                {
                    Application.Current.Dispatcher.BeginInvoke(() => EncodingJobs.Clear());
                    return;
                }

                // Remove jobs no longer in queue first
                IEnumerable<EncodingJobViewModel> viewModelsToRemove = EncodingJobs.Where(x => !encodingJobQueue.Any(y => y.Id == x.Id));
                bool selectedViewModelWillBeRemoved = viewModelsToRemove.Any(x => x.Id == SelectedEncodingJobViewModel?.Id);

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (selectedViewModelWillBeRemoved is true) SelectedEncodingJobViewModel = null;
                    EncodingJobs.RemoveRange(viewModelsToRemove);
                });

                // Update or Create the rest
                foreach (EncodingJobData data in encodingJobQueue)
                {
                    EncodingJobViewModel job = EncodingJobs.SingleOrDefault(x => x.Equals(data));
                    if (job is not null)
                    {
                        job.Update(data);
                        int currentIndex = EncodingJobs.IndexOf(job);
                        int newIndex = encodingJobQueue.IndexOf(data);

                        bool isSelectedViewModel = job.Id == SelectedEncodingJobViewModel?.Id;

                        if (currentIndex != newIndex)
                        {
                            Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                EncodingJobs.Move(currentIndex, newIndex);
                                if (isSelectedViewModel is true) SelectedEncodingJobViewModel = job;
                            });
                        }
                    }
                    else
                    {
                        EncodingJobClientModel model = new(data, CommunicationManager);
                        EncodingJobViewModel viewModel = new(model);
                        Application.Current.Dispatcher.BeginInvoke(() => EncodingJobs.Insert(encodingJobQueue.IndexOf(data), viewModel));
                    }
                }
            }
        }

        private void RefreshSourceFiles()
        {
            if (RefreshSourceFilesTask?.IsCompleted ?? true)
            {
                RefreshSourceFilesTask = Task.Factory.StartNew(() =>
                {
                    Dictionary<string, List<VideoSourceData>> movieSourceData = Model.GetCurrentMovieSourceData();
                    Dictionary<string, List<ShowSourceData>> showSourceData = Model.GetCurrentShowSourceData();

                    if (movieSourceData is not null)
                    {
                        var converted = new Dictionary<string, BulkObservableCollection<VideoSourceData>>(movieSourceData
                                                .ToDictionary(x => x.Key, x => new BulkObservableCollection<VideoSourceData>(x.Value)));
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            MovieSourceFiles.Refresh(converted);

                            foreach (KeyValuePair<string, BulkObservableCollection<VideoSourceData>> keyValuePair in MovieSourceFiles)
                            {
                                keyValuePair.Value.Sort(VideoSourceData.CompareByFileName);
                            }

                        });
                    }

                    if (showSourceData is not null)
                    {
                        var converted = new Dictionary<string, BulkObservableCollection<ShowSourceData>>(showSourceData
                                                .ToDictionary(x => x.Key, x => new BulkObservableCollection<ShowSourceData>(x.Value)));
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            ShowSourceFiles.Refresh(converted);
                        });
                    }
                });
            }
        }

        public void Dispose()
        {
            ClientUpdateService.Dispose();
        }
    }
}
