using AutoEncodeClient.ApiClient;
using AutoEncodeClient.Interfaces;
using AutoEncodeClient.Models;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Collections;
using AutoEncodeUtilities.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AutoEncodeClient.ViewModels
{
    public class AutoEncodeClientViewModel : 
        ViewModelBase<AutoEncodeClientModel>,
        IAutoEncodeClientViewModel
    {
        private Timer EncodingJobQueueStateTimer { get; set; }

        public AutoEncodeClientViewModel(AutoEncodeClientModel model)
            : base(model) 
        {
            EncodingJobQueueStateTimer = new Timer(3000)
            {
                AutoReset = false,
                Enabled = true
            };
            EncodingJobQueueStateTimer.Elapsed += EncodingJobQueueStateTimerElapsed;
            EncodingJobQueueStateTimer.Start();
        }

        public BulkObservableCollection<EncodingJobViewModel> EncodingJobs { get; } = new BulkObservableCollection<EncodingJobViewModel>();
        public ObservableDictionary<string, int> Dictionary { get; set; }

        private EncodingJobViewModel _selectedEncodingJobViewModel = null;
        public EncodingJobViewModel SelectedEncodingJobViewModel
        {
            get => _selectedEncodingJobViewModel;
            set
            {
                if (value != SelectedEncodingJobViewModel)
                {
                    _selectedEncodingJobViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        private void EncodingJobQueueStateTimerElapsed(object src, ElapsedEventArgs e)
        {
            this.Dictionary.Count();

            List<EncodingJobData> encodingJobQueue = Model.GetCurrentEncodingJobQueue();

            if (encodingJobQueue != null && encodingJobQueue.Any())
            {
                // Remove jobs no longer in queue first
                IEnumerable<EncodingJobViewModel> viewModelsToRemove = EncodingJobs.Where(x => !encodingJobQueue.Any(y => y.Id == x.Id));

                Application.Current.Dispatcher.BeginInvoke(() => EncodingJobs.RemoveRange(viewModelsToRemove));

                // Update or Create the rest
                foreach (EncodingJobData data in encodingJobQueue)
                {
                    EncodingJobViewModel job = EncodingJobs.SingleOrDefault(x => x.Id == data.Id);
                    if (job is not null)
                    {
                        data.CopyProperties(job);
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
                        EncodingJobClientModel model = new(data);
                        EncodingJobViewModel viewModel = new(model);
                        Application.Current.Dispatcher.BeginInvoke(() => EncodingJobs.Insert(encodingJobQueue.IndexOf(data), viewModel));
                    }
                }
            }

            EncodingJobQueueStateTimer.Start();
        }
    }
}
