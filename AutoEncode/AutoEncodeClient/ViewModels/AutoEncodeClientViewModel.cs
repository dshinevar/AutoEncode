using AutoEncodeClient.Collections;
using AutoEncodeClient.Comm;
using AutoEncodeClient.Config;
using AutoEncodeClient.Models;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace AutoEncodeClient.ViewModels
{
    public class AutoEncodeClientViewModel :
        ViewModelBase<AutoEncodeClientModel>,
        IAutoEncodeClientViewModel,
        IDisposable
    {
        private readonly ClientUpdateService ClientUpdateService;
        private readonly ICommunicationManager CommunicationManager;

        public AutoEncodeClientViewModel(AutoEncodeClientModel model, ILogger logger, ICommunicationManager communicationManager, AEClientConfig config)
            : base(model)
        {
            CommunicationManager = communicationManager;

            SourceFilesViewModel = new SourceFilesViewModel(communicationManager);

            ClientUpdateService = new ClientUpdateService(logger, config.ConnectionSettings.IPAddress, config.ConnectionSettings.ClientUpdatePort);
            ClientUpdateService.DataReceived += (s, data) => UpdateClient(data);
            ClientUpdateService.Start();

            SourceFilesViewModel.RefreshSourceFiles();
        }

        #region SubViewModels
        public ISourceFilesViewModel SourceFilesViewModel { get; }
        public BulkObservableCollection<EncodingJobViewModel> EncodingJobs { get; } = new BulkObservableCollection<EncodingJobViewModel>();

        private EncodingJobViewModel _selectedEncodingJobViewModel = null;
        public EncodingJobViewModel SelectedEncodingJobViewModel
        {
            get => _selectedEncodingJobViewModel;
            set => SetAndNotify(_selectedEncodingJobViewModel, value, () => _selectedEncodingJobViewModel = value);
        }
        #endregion SubViewModels

        #region Properties
        public bool ConnectedToServer => ClientUpdateService.Connected;
        #endregion Properties

        private void UpdateClient(List<EncodingJobData> encodingJobQueue)
        {
            if (encodingJobQueue is not null)
            {
                if (encodingJobQueue.Any() is false)
                {
                    Application.Current.Dispatcher.Invoke(() => EncodingJobs.Clear());
                    return;
                }

                // Remove jobs no longer in queue first
                IEnumerable<EncodingJobViewModel> viewModelsToRemove = EncodingJobs.Where(x => !encodingJobQueue.Any(y => y.Id == x.Id));
                bool selectedViewModelWillBeRemoved = viewModelsToRemove.Any(x => x.Id == SelectedEncodingJobViewModel?.Id);

                Application.Current.Dispatcher.Invoke(() =>
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
                        Application.Current.Dispatcher.Invoke(() => job.Update(data));

                        int currentIndex = EncodingJobs.IndexOf(job);
                        int newIndex = encodingJobQueue.IndexOf(data);

                        bool isSelectedViewModel = job.Id == SelectedEncodingJobViewModel?.Id;

                        if (currentIndex != newIndex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
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
                        Application.Current.Dispatcher.Invoke(() => EncodingJobs.Insert(encodingJobQueue.IndexOf(data), viewModel));
                    }
                }
            }
        }

        public void Dispose()
        {
            ClientUpdateService.Dispose();
        }
    }
}
