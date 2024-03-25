using AutoEncodeClient.Collections;
using AutoEncodeClient.Command;
using AutoEncodeClient.Communication;
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
        #region Dependencies
        public ICommunicationManager CommunicationManager { get; set; }
        #endregion Dependencies

        public SourceFilesViewModel()
        {
            AECommand refreshSourceFilesCommand = new(() => CanRefreshSourceFiles, RefreshSourceFiles);
            RefreshSourceFilesCommand = refreshSourceFilesCommand;
            AddCommand(refreshSourceFilesCommand, nameof(CanRefreshSourceFiles));

            AECommand requestEncodeCommand = new(() => CanRequestEncode, RequestEncode);
            RequestEncodeCommand = requestEncodeCommand;
            AddCommand(requestEncodeCommand, nameof(CanRequestEncode));
        }

        #region Properties
        public ObservableDictionary<string, IEnumerable<SourceFileData>> MovieSourceFiles { get; set; } = [];
        public ObservableDictionary<string, IEnumerable<ShowSourceFileViewModel>> ShowSourceFiles { get; set; } = [];
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
                CanRefreshSourceFiles = true;
                return;
            }

            // Handle Movies
            MovieSourceFiles.Clear();

            var movies = sourceFiles.Where(x => x.Value.IsShows is false);

            foreach (var movieDir in movies)
            {
                Application.Current.Dispatcher.Invoke(() => MovieSourceFiles.Add(movieDir.Key, movieDir.Value.Files));
            }

            // Handle Shows
            ShowSourceFiles.Clear();

            var shows = sourceFiles.Where(x => x.Value.IsShows is true).ToDictionary(x => x.Key, x => x.Value.Files.Cast<ShowSourceFileData>());

            foreach (KeyValuePair<string, IEnumerable<ShowSourceFileData>> showDir in shows)
            {
                List<ShowSourceFileViewModel> showsInDir = [];
                var groupedShows = showDir.Value.GroupBy(x => x.ShowName);

                foreach (var group in groupedShows)
                {
                    ShowSourceFileViewModel showViewModel = new()
                    {
                        ShowName = group.Key
                    };

                    var groupedSeasons = group.GroupBy(x => x.SeasonInt).OrderBy(x => x.Key);

                    foreach (var season in groupedSeasons)
                    {
                        SeasonSourceFileViewModel seasonViewModel = new()
                        {
                            SeasonInt = season.Key
                        };

                        var episodes = season.OrderBy(x => x.EpisodeInts.First());
                        seasonViewModel.Episodes.AddRange(episodes);
                        showViewModel.Seasons.Add(seasonViewModel);
                    }

                    showsInDir.Add(showViewModel);
                }

                Application.Current.Dispatcher.Invoke(() => ShowSourceFiles.Add(showDir.Key, showsInDir));
            }

            CanRefreshSourceFiles = true;
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
