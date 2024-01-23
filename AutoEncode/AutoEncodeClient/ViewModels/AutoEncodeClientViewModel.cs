using AutoEncodeClient.Collections;
using AutoEncodeClient.Comm;
using AutoEncodeClient.Command;
using AutoEncodeClient.Config;
using AutoEncodeClient.Dialogs;
using AutoEncodeClient.Models;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public AutoEncodeClientViewModel(AutoEncodeClientModel model, ILogger logger, CommunicationManager communicationManager, AEClientConfig config)
            : base(model)
        {
            CommunicationManager = communicationManager;

            AECommand refreshSourceFilesCommand = new(() => CanRefreshSourceFiles, RefreshSourceFiles);
            RefreshSourceFilesCommand = refreshSourceFilesCommand;
            AddCommand(refreshSourceFilesCommand, nameof(CanRefreshSourceFiles));

            AECommandWithParameter requestEncodeCommand = new(() => CanRequestEncode, RequestEncode);
            RequestEncodeCommand = requestEncodeCommand;
            AddCommand(requestEncodeCommand, nameof(CanRequestEncode));

            RefreshSourceFiles();

            ClientUpdateService = new ClientUpdateService(logger, config.ConnectionSettings.IPAddress, config.ConnectionSettings.ClientUpdatePort);
            ClientUpdateService.DataReceived += (s, data) => UpdateClient(data);
            ClientUpdateService.Start();
        }

        #region Commands
        public ICommand RefreshSourceFilesCommand { get; }
        private bool _canRefreshSouceFiles = true;
        private bool CanRefreshSourceFiles
        {
            get => _canRefreshSouceFiles;
            set => SetAndNotify(_canRefreshSouceFiles, value, () => _canRefreshSouceFiles = value);
        }

        public ICommand RequestEncodeCommand { get; }
        private bool _canRequestEncode = true;
        private bool CanRequestEncode
        {
            get => _canRequestEncode;
            set => SetAndNotify(_canRequestEncode, value, () => _canRequestEncode = value);
        }
        #endregion Commands

        #region Properties
        public BulkObservableCollection<EncodingJobViewModel> EncodingJobs { get; } = new BulkObservableCollection<EncodingJobViewModel>();
        public ObservableDictionary<string, BulkObservableCollection<SourceFileData>> MovieSourceFiles { get; } = new();
        public ObservableDictionary<string, ObservableDictionary<string, ObservableDictionary<string, BulkObservableCollection<ShowSourceFileData>>>> ShowSourceFiles { get; } = new();

        private EncodingJobViewModel _selectedEncodingJobViewModel = null;
        public EncodingJobViewModel SelectedEncodingJobViewModel
        {
            get => _selectedEncodingJobViewModel;
            set => SetAndNotify(_selectedEncodingJobViewModel, value, () => _selectedEncodingJobViewModel = value);
        }
        public bool ConnectedToServer => ClientUpdateService.Connected;
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
                        Application.Current.Dispatcher.BeginInvoke(() => job.Update(data));

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

        private async void RefreshSourceFiles()
        {
            CanRefreshSourceFiles = false;

            var (Movies, Shows) = await Model.RequestSourceFiles();

            if (Movies is null && Shows is null)
            {
                AEDialogHandler.ShowError("Failed to get source files.", "Source File Request Failure");
                return;
            }

            if (Movies is not null)
            {
                var converted = new Dictionary<string, BulkObservableCollection<SourceFileData>>(Movies
                                        .ToDictionary(x => x.Key, x => new BulkObservableCollection<SourceFileData>(x.Value)));
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    MovieSourceFiles.Refresh(converted);

                    foreach (KeyValuePair<string, BulkObservableCollection<SourceFileData>> keyValuePair in MovieSourceFiles)
                    {
                        keyValuePair.Value.Sort(SourceFileData.CompareByFileName);
                    }

                });
            }

            if (Shows is not null)
            {
                BuildShowSourceFiles(Shows);
            }

            CanRefreshSourceFiles = true;
        }

        private async void BuildShowSourceFiles(IDictionary<string, IEnumerable<ShowSourceFileData>> showSourceData)
        {
            ObservableDictionary<string, ObservableDictionary<string, ObservableDictionary<string, BulkObservableCollection<ShowSourceFileData>>>> updateFiles = new();

            foreach (var directory in showSourceData)
            {
                var showSeasonsCompiled = new ObservableDictionary<string, ObservableDictionary<string, BulkObservableCollection<ShowSourceFileData>>>();
                Dictionary<string, IEnumerable<ShowSourceFileData>> filesByShow = directory.Value.GroupBy(s => s.ShowName).ToDictionary(x => x.Key, x => x.AsEnumerable());

                foreach (var show in filesByShow)
                {
                    var filesBySeason =
                        new ObservableDictionary<string, BulkObservableCollection<ShowSourceFileData>>(show.Value.GroupBy(x => x.Season)
                            .ToDictionary(y => y.Key, y => new BulkObservableCollection<ShowSourceFileData>(y.Select(f => f).OrderBy(o => o.EpisodeInts.First()).ToList())));

                    showSeasonsCompiled.Add(show.Key, filesBySeason);
                }

                updateFiles.Add(directory.Key, showSeasonsCompiled);
            }

            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                ShowSourceFiles.Refresh(updateFiles);
            });
        }

        private async void RequestEncode(object obj)
        {
            CanRequestEncode = false;
            bool success;
            bool isShow = false;
            try
            {
                if (obj is SourceFileData sourceFileData)
                {
                    if (sourceFileData is ShowSourceFileData) isShow = true;

                    success = await Model.RequestEncodingJob(sourceFileData.Guid, isShow);

                    if (success is false)
                    {
                        AEDialogHandler.ShowError($"Failed to add {sourceFileData.FileName} to encoding job queue.", "Failed To Add Encoding Job");
                    }
                }
            }
            catch (Exception ex)
            {
                AEDialogHandler.ShowError(ex.Message, "Exception Thrown While Requesting Encode");
            }

            CanRequestEncode = true;
        }

        public void Dispose()
        {
            ClientUpdateService.Dispose();
        }
    }
}
