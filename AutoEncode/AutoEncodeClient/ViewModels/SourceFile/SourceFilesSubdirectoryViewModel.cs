using AutoEncodeClient.Command;
using AutoEncodeClient.Communication.Interfaces;
using AutoEncodeClient.Factories;
using AutoEncodeClient.ViewModels.SourceFile.Interfaces;
using AutoEncodeUtilities.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace AutoEncodeClient.ViewModels.SourceFile;

public class SourceFilesSubdirectoryViewModel :
    ViewModelBase,
    ISourceFilesSubdirectoryViewModel
{
    #region Dependencies
    public ISourceFileFactory SourceFileFactory { get; set; }

    public ICommunicationMessageHandler CommunicationMessageHandler { get; set; }
    #endregion Dependencies

    #region Properties
    public string Name { get; set; }

    private bool _requestingEncode = false;
    public bool RequestingEncode
    {
        get => _requestingEncode;
        set => SetAndNotify(_requestingEncode, value, () => _requestingEncode = value);
    }

    private readonly ObservableCollection<ISourceFilesSubdirectoryViewModel> _subdirectories = [];
    public ICollectionView SubdirectoriesView { get; set; }

    private readonly ObservableCollection<ISourceFileViewModel> _files = [];
    public ICollectionView FilesView { get; set; }

    /// <summary>Combines the Subdirectories and Files into one collection for display purposes.</summary>
    public IList SubdirectoriesAndFiles { get; }
    #endregion Properties

    #region Commands
    public ICommand RequestEncodeCommand { get; set; }
    #endregion Commands

    public SourceFilesSubdirectoryViewModel(string name)
    {
        AECommand requestEncodeCommand = new(CanRequestEncode, RequestEncode);
        RequestEncodeCommand = requestEncodeCommand;
        AddCommand(requestEncodeCommand, nameof(RequestingEncode));

        Name = name;

        SubdirectoriesView = CollectionViewSource.GetDefaultView(_subdirectories);
        SubdirectoriesView.SortDescriptions.Add(new(nameof(ISourceFilesSubdirectoryViewModel.Name), ListSortDirection.Ascending));

        FilesView = CollectionViewSource.GetDefaultView(_files);
        FilesView.SortDescriptions.Add(new(nameof(ISourceFileViewModel.Filename), ListSortDirection.Ascending));

        SubdirectoriesAndFiles = new CompositeCollection()
        {
            new CollectionContainer() { Collection = SubdirectoriesView },
            new CollectionContainer() { Collection = FilesView },
        };
    }


    #region Public Methods
    public bool Any() => _subdirectories.Any() || _files.Any();

    public IEnumerable<Guid> GetSourceFileGuids()
    {
        List<Guid> sourceFileGuids = _files.OrderBy(f => f.Filename).Select(f => f.Guid).ToList();

        foreach (ISourceFilesSubdirectoryViewModel subdirectory in _subdirectories)
        {
            sourceFileGuids.AddRange(subdirectory.GetSourceFileGuids());
        }

        return sourceFileGuids;
    }

    public void AddSourceFile(IEnumerable<string> remainingSubPathParts, ISourceFileViewModel sourceFileViewModel)
    {
        if (remainingSubPathParts.Any() is false)
        {
            _files.Add(sourceFileViewModel);
            RegisterChildViewModel(sourceFileViewModel);
        }
        else
        {
            string firstSubPathPart = remainingSubPathParts.First();

            // Try to see if we have a subdirectory already, if not create it
            ISourceFilesSubdirectoryViewModel subdirectoryViewModel = _subdirectories.FirstOrDefault(_ => _.Name == firstSubPathPart);
            if (subdirectoryViewModel is null)
            {
                subdirectoryViewModel = SourceFileFactory.CreateSubdirectory(firstSubPathPart);
                RegisterChildViewModel(subdirectoryViewModel);
                _subdirectories.Add(subdirectoryViewModel);
            }

            subdirectoryViewModel.AddSourceFile(remainingSubPathParts.Skip(1), sourceFileViewModel);
        }
    }

    public bool TryRemoveSourceFile(Guid sourceFileGuid, IEnumerable<string> remainingSubPathParts, out ISourceFileViewModel sourceFile)
    {
        sourceFile = _files.FirstOrDefault(f => f.Guid == sourceFileGuid);
        if (sourceFile is not null)
        {
            return _files.Remove(sourceFile);
        }

        if (remainingSubPathParts.Any() is true)
        {
            string firstSubPathPart = remainingSubPathParts.First();
            ISourceFilesSubdirectoryViewModel subdirectoryViewModel = _subdirectories.FirstOrDefault(_ => _.Name == firstSubPathPart);

            if (subdirectoryViewModel is not null)
            {
                if (subdirectoryViewModel.TryRemoveSourceFile(sourceFileGuid, remainingSubPathParts.Skip(1), out sourceFile) is true)
                {
                    if (subdirectoryViewModel.Any() is false)
                    {
                        _subdirectories.Remove(subdirectoryViewModel);
                        SourceFileFactory.Release(subdirectoryViewModel);
                    }

                    return true;
                }
            }           
        }

        // Didn't find file, just return false
        return false;
    }

    public bool UpdateSourceFile(SourceFileData sourceFileData, IEnumerable<string> remainingSubPathParts)
    {
        ISourceFileViewModel sourceFileViewModel = _files.FirstOrDefault(f => f.Guid == sourceFileData.Guid);
        if (sourceFileViewModel is not null)
        {
            sourceFileViewModel.Update(sourceFileData);
            return true;
        }

        if (remainingSubPathParts.Any() is true)
        {
            string firstSubPathPart = remainingSubPathParts.First();
            ISourceFilesSubdirectoryViewModel subdirectoryViewModel = _subdirectories.FirstOrDefault(_ => _.Name == firstSubPathPart);
            return subdirectoryViewModel?.UpdateSourceFile(sourceFileData, remainingSubPathParts.Skip(1)) ?? false;
        }

        return false;
    }
    #endregion Public Methods

    #region Command Methods
    private bool CanRequestEncode() => RequestingEncode is false;
    private async void RequestEncode()
    {
        RequestingEncode = true;

        bool success = await CommunicationMessageHandler.BulkRequestEncode(GetSourceFileGuids());

        if (success is false)
        {
            ShowErrorDialog("Failed to request bulk encode.", "Bulk Encode Request Failed");
        }

        RequestingEncode = false;
    }
    #endregion Command Methods
}
