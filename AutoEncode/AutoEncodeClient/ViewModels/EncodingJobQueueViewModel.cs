using AutoEncodeClient.Collections;
using AutoEncodeClient.Communication.Interfaces;
using AutoEncodeClient.Factories;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.ViewModels.EncodingJob.Interfaces;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace AutoEncodeClient.ViewModels;

public class EncodingJobQueueViewModel :
    ViewModelBase,
    IEncodingJobQueueViewModel
{
    #region Dependencies
    public ICommunicationMessageHandler CommunicationMessageHandler { get; set; }

    public IClientUpdateSubscriber ClientUpdateSubscriber { get; set; }

    public IEncodingJobClientModelFactory EncodingJobFactory { get; set; }
    #endregion Dependencies

    #region Properties
    private bool _initialized = false;

    private readonly BulkObservableCollection<IEncodingJobViewModel> _encodingJobs = [];

    public ICollectionView EncodingJobsView { get; set; }

    private IEncodingJobViewModel _selectedEncodingJobViewModel = null;
    public IEncodingJobViewModel SelectedEncodingJobViewModel
    {
        get => _selectedEncodingJobViewModel;
        set => SetAndNotify(_selectedEncodingJobViewModel, value, () => _selectedEncodingJobViewModel = value);
    }
    #endregion Properties

    /// <summary>Default Constructor</summary>
    public EncodingJobQueueViewModel()
    {
        EncodingJobsView = CollectionViewSource.GetDefaultView(_encodingJobs);
    }

    public async void Initialize()
    {
        if (_initialized is false)
        {
            IEnumerable<EncodingJobData> queue = await CommunicationMessageHandler.RequestJobQueue();

            foreach (EncodingJobData data in queue)
            {
                IEncodingJobClientModel model = EncodingJobFactory.Create(data);
                IEncodingJobViewModel viewModel = EncodingJobFactory.Create(model);
                RegisterChildViewModel(viewModel);
                _encodingJobs.Add(viewModel);
            }

            ClientUpdateSubscriber.ClientUpdateMessageReceived += ClientUpdateSubscriber_ClientUpdateMessageReceived;
            ClientUpdateSubscriber.Subscribe(nameof(ClientUpdateType.EncodingJobQueue));
            ClientUpdateSubscriber.Start();
        }

        _initialized = true;
    }

    private void ClientUpdateSubscriber_ClientUpdateMessageReceived(object sender, ClientUpdateMessage e)
    {
        if (e.Type == ClientUpdateType.EncodingJobQueue)
        {
            EncodingJobQueueUpdateData updateData = e.UnpackData<EncodingJobQueueUpdateData>();
            switch (updateData.Type)
            {
                case EncodingJobQueueUpdateType.Add:
                {
                    if (updateData.EncodingJob is not null)
                    {
                        // Make sure we don't already have the job
                        if (_encodingJobs.Any(j => j.Id == updateData.EncodingJob.Id) is false)
                        {
                            IEncodingJobClientModel model = EncodingJobFactory.Create(updateData.EncodingJob);
                            IEncodingJobViewModel viewModel = EncodingJobFactory.Create(model);
                            RegisterChildViewModel(viewModel);
                            Application.Current.Dispatcher.BeginInvoke(() => _encodingJobs.Add(viewModel));
                        }
                    }
                    break;
                }
                case EncodingJobQueueUpdateType.Remove:
                {
                    if (updateData.JobId != default)
                    {
                        IEncodingJobViewModel viewModel = _encodingJobs.FirstOrDefault(j => j.Id == updateData.JobId);

                        Application.Current.Dispatcher.BeginInvoke(() => _encodingJobs.Remove(viewModel));

                        EncodingJobFactory.Release(viewModel.GetModel());
                        EncodingJobFactory.Release(viewModel);
                    }
                    break;
                }
                case EncodingJobQueueUpdateType.Move:
                {
                    // TODO
                    break;
                }
            }
        }
    }
}
