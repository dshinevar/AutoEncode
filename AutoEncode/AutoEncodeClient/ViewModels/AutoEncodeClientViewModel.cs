using AutoEncodeClient.Collections;
using AutoEncodeClient.Communication;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using Castle.Windsor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace AutoEncodeClient.ViewModels
{
    public class AutoEncodeClientViewModel :
        ViewModelBase<IAutoEncodeClientModel>,
        IAutoEncodeClientViewModel
    {
        #region Dependencies
        public IWindsorContainer Container { get; set; }

        public ISourceFilesViewModel SourceFilesViewModel { get; set; }

        public IClientUpdateSubscriber ClientUpdateSubscriber { get; set; }

        public IEncodingJobClientModelFactory EncodingJobFactory { get; set; }
        #endregion Dependencies

        /// <summary>Default Constructor </summary>
        public AutoEncodeClientViewModel() { }

        public async void Initialize(IAutoEncodeClientModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            Model = model;

            // On initial startup, request the queue
            ICommunicationManager communicationManager = Container.Resolve<ICommunicationManager>();
            IEnumerable<EncodingJobData> queue = await communicationManager.RequestJobQueue();
            UpdateQueue(queue);

            // Subscriber handles future queue updates
            ClientUpdateSubscriber.SubscribeToEncodingJobQueueUpdate(CommunicationConstants.EncodingJobQueueUpdate, (data) => UpdateQueue(data.Queue));
        }

        #region SubViewModels
        public BulkObservableCollection<IEncodingJobViewModel> EncodingJobs { get; } = [];

        private IEncodingJobViewModel _selectedEncodingJobViewModel = null;
        public IEncodingJobViewModel SelectedEncodingJobViewModel
        {
            get => _selectedEncodingJobViewModel;
            set => SetAndNotify(_selectedEncodingJobViewModel, value, () => _selectedEncodingJobViewModel = value);
        }
        #endregion SubViewModels

        private void UpdateQueue(IEnumerable<EncodingJobData> encodingJobQueue)
        {
            if (encodingJobQueue is not null)
            {
                List<EncodingJobData> encodingJobQueueList = encodingJobQueue.ToList();

                // If empty just clear
                if (encodingJobQueueList.Count == 0)
                {
                    IEnumerable<IEncodingJobViewModel> queue = EncodingJobs.ToList();
                    Application.Current.Dispatcher.BeginInvoke(EncodingJobs.Clear);

                    foreach (IEncodingJobViewModel viewModel in queue)
                    {
                        EncodingJobFactory.Release(viewModel.Model);
                    }

                    return;
                }

                // Remove jobs no longer in queue first
                IEnumerable<IEncodingJobViewModel> viewModelsToRemove = EncodingJobs.Where(x => !encodingJobQueueList.Any(y => y.Id == x.Id)).ToList();
                bool selectedViewModelWillBeRemoved = viewModelsToRemove.Any(x => x.Id == SelectedEncodingJobViewModel?.Id);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (selectedViewModelWillBeRemoved is true) SelectedEncodingJobViewModel = null;
                    EncodingJobs.RemoveRange(viewModelsToRemove);
                });

                foreach (IEncodingJobViewModel viewModel in viewModelsToRemove)
                {
                    EncodingJobFactory.Release(viewModel.Model);
                }

                // Add new jobs
                IEnumerable<EncodingJobData> newJobs = encodingJobQueueList.Where(x => !EncodingJobs.Any(y => y.Id == x.Id)).ToList();
                foreach (EncodingJobData newJob in newJobs)
                {
                    IEncodingJobClientModel model = EncodingJobFactory.Create(newJob);
                    IEncodingJobViewModel viewModel = new EncodingJobViewModel(model);
                    Application.Current.Dispatcher.Invoke(() => EncodingJobs.Add(viewModel));
                }

                // Sort
                foreach (EncodingJobData data in encodingJobQueueList)
                {
                    IEncodingJobViewModel viewModel = EncodingJobs.FirstOrDefault(x => x.Id == data.Id);
                    int oldIndex = EncodingJobs.IndexOf(viewModel);
                    int newIndex = encodingJobQueueList.IndexOf(data);
                    if (oldIndex != newIndex)
                    {
                        Application.Current.Dispatcher.Invoke(() => EncodingJobs.Move(oldIndex, newIndex));
                    }
                }
            }
        }
    }
}
