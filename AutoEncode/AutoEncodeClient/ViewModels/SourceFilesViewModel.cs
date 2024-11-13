using AutoEncodeClient.Collections;
using AutoEncodeClient.Communication.Interfaces;
using AutoEncodeClient.Factories;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeClient.ViewModels.SourceFile.Interfaces;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace AutoEncodeClient.ViewModels;

public class SourceFilesViewModel :
    ViewModelBase,
    ISourceFilesViewModel
{
    #region Dependencies
    public ICommunicationMessageHandler CommunicationMessageHandler { get; set; }

    public IClientUpdateSubscriber ClientUpdateSubscriber { get; set; }

    public ISourceFileFactory SourceFileFactory { get; set; }
    #endregion Dependencies

    #region Properties
    private bool _initialized = false;

    public ObservableDictionary<string, ISourceFilesDirectoryViewModel> SourceFiles { get; } = [];
    #endregion Properties

    public SourceFilesViewModel() { }

    public async void Initialize()
    {
        if (_initialized is false)
        {
            Dictionary<string, IEnumerable<SourceFileData>> sourceFiles = await CommunicationMessageHandler.RequestSourceFiles();

            foreach (KeyValuePair<string, IEnumerable<SourceFileData>> sourceFilesByDirectory in sourceFiles.OrderBy(_ => _.Key))
            {
                ISourceFilesDirectoryViewModel directory = SourceFileFactory.CreateDirectory(sourceFilesByDirectory.Key);
                directory.Initialize(sourceFilesByDirectory.Value);
                SourceFiles.Add(sourceFilesByDirectory.Key, directory);
            }

            ClientUpdateSubscriber.ClientUpdateMessageReceived += ClientUpdateSubscriber_ClientUpdateMessageReceived;
            ClientUpdateSubscriber.Subscribe(nameof(ClientUpdateType.SourceFilesUpdate));
            ClientUpdateSubscriber.Start();
        }

        _initialized = true;
    }

    private void ClientUpdateSubscriber_ClientUpdateMessageReceived(object sender, ClientUpdateMessage e)
    {
        if (e.Type == ClientUpdateType.SourceFilesUpdate)
        {
            IEnumerable<SourceFileUpdateData> sourceFileUpdates = e.UnpackData<IEnumerable<SourceFileUpdateData>>();
            foreach (SourceFileUpdateData sourceFileUpdateData in sourceFileUpdates)
            {
                switch (sourceFileUpdateData.Type)
                {
                    case SourceFileUpdateType.Add:
                    {
                        if (SourceFiles.TryGetValue(sourceFileUpdateData.SourceFile.SearchDirectoryName, out ISourceFilesDirectoryViewModel directoryViewModel) is true)
                        {
                            Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                directoryViewModel.AddSourceFile(sourceFileUpdateData.SourceFile);
                            });                            
                        }
                        break;
                    }
                    case SourceFileUpdateType.Remove:
                    {
                        if (SourceFiles.TryGetValue(sourceFileUpdateData.SourceFile.SearchDirectoryName, out ISourceFilesDirectoryViewModel directoryViewModel) is true)
                        {
                            Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                directoryViewModel.RemoveSourceFile(sourceFileUpdateData.SourceFile);
                            });
                        }
                        break;
                    }
                    case SourceFileUpdateType.Update:
                    {
                        if (SourceFiles.TryGetValue(sourceFileUpdateData.SourceFile.SearchDirectoryName, out ISourceFilesDirectoryViewModel directoryViewModel) is true)
                        {
                            Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                directoryViewModel.UpdateSourceFile(sourceFileUpdateData.SourceFile);
                            });
                        }
                        break;
                    }
                }
            }
        }
    }
}
