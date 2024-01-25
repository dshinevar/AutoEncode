using AutoEncodeClient.Collections;
using AutoEncodeClient.Comm;
using AutoEncodeClient.Command;
using AutoEncodeClient.Dialogs;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AutoEncodeClient.ViewModels
{
    public class SourceFilesViewModel :
        ViewModelBase,
        ISourceFilesViewModel
    {
        private readonly ICommunicationManager CommunicationManager;

        public SourceFilesViewModel() { }

        public SourceFilesViewModel(ICommunicationManager communicationManager)
        {
            CommunicationManager = communicationManager;

            AECommand refreshSourceFilesCommand = new(() => CanRefreshSourceFiles, RefreshSourceFiles);
            RefreshSourceFilesCommand = refreshSourceFilesCommand;
            AddCommand(refreshSourceFilesCommand, nameof(CanRefreshSourceFiles));

            AECommandWithParameter requestEncodeCommand = new(() => CanRequestEncode, RequestEncode);
            RequestEncodeCommand = requestEncodeCommand;
            AddCommand(requestEncodeCommand, nameof(CanRequestEncode));
        }

        #region Properties
        public ObservableDictionary<string, BulkObservableCollection<SourceFileData>> MovieSourceFiles { get; set; } = new();
        public ObservableDictionary<string, ObservableDictionary<string, ObservableDictionary<string, BulkObservableCollection<ShowSourceFileData>>>> ShowSourceFiles { get; set; } = new();
        #endregion Properties

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

        public async void RefreshSourceFiles()
        {
            CanRefreshSourceFiles = false;

            var sourceFiles = await CommunicationManager.RequestSourceFiles();

            if (sourceFiles is null)
            {
                AEDialogHandler.ShowError("Failed to get source files.", "Source File Request Failure");
                return;
            }

            var movies = new Dictionary<string, BulkObservableCollection<SourceFileData>>(sourceFiles.Where(x => x.Value.IsShows is false)
                                                                                                        .ToDictionary(x => x.Key, x => new BulkObservableCollection<SourceFileData>(x.Value.Files)));

            var shows = sourceFiles.Where(x => x.Value.IsShows is true).ToDictionary(x => x.Key, x => x.Value.Files.Cast<ShowSourceFileData>());
            var showsConverted = new Dictionary<string, BulkObservableCollection<ShowSourceFileData>>(shows.ToDictionary(x => x.Key, x => new BulkObservableCollection<ShowSourceFileData>(x.Value)));

            if (movies is not null)
            {
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    MovieSourceFiles.Refresh(movies);

                    foreach (KeyValuePair<string, BulkObservableCollection<SourceFileData>> keyValuePair in MovieSourceFiles)
                    {
                        keyValuePair.Value.Sort(SourceFileData.CompareByFileName);
                    }
                });
            }

            if (shows is not null)
            {
                BuildShowSourceFiles(shows);
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
            try
            {
                if (obj is SourceFileData sourceFileData)
                {
                    success = await CommunicationManager.RequestEncode(sourceFileData.Guid);

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
    }
}
