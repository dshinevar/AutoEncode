using AutoEncodeClient.Factories;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.ViewModels.SourceFile.Interfaces;
using AutoEncodeUtilities.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace AutoEncodeClient.ViewModels.SourceFile;

/// <summary>Top level directory for source files -- <see cref="SearchDirectory"/> </summary>
public class SourceFilesDirectoryViewModel :
    ViewModelBase,
    ISourceFilesDirectoryViewModel
{
    #region Dependencies
    public ISourceFileFactory SourceFileFactory { get; set; }
    #endregion Dependencies

    private bool _initialized = false;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetAndNotify(_name, value, () => _name = value);
    }
    private readonly ObservableCollection<ISourceFilesSubdirectoryViewModel> _subdirectories = [];
    public ICollectionView SubdirectoriesView { get; set; }

    private readonly ObservableCollection<ISourceFileViewModel> _files = [];
    public ICollectionView FilesView { get; set; }

    /// <summary>Combines the Subdirectories and Files into one collection for display purposes.</summary>
    public IList SubdirectoriesAndFiles { get; set; }

    public SourceFilesDirectoryViewModel(string name)
    {
        Name = name;

        SubdirectoriesView = CollectionViewSource.GetDefaultView(_subdirectories);
        SubdirectoriesView.SortDescriptions.Add(new(nameof(ISourceFilesSubdirectoryViewModel.Name), ListSortDirection.Ascending));

        FilesView = CollectionViewSource.GetDefaultView(_files);
        FilesView.SortDescriptions.Add(new(nameof(ISourceFileViewModel.FileNameWithoutExtension), ListSortDirection.Ascending));

        SubdirectoriesAndFiles = new CompositeCollection()
        {
            new CollectionContainer() { Collection = SubdirectoriesView },
            new CollectionContainer() { Collection = FilesView },
        };
    }

    public void Initialize(IEnumerable<SourceFileData> sourceFiles)
    {
        if (_initialized is false)
        {
            foreach (SourceFileData sourceFile in sourceFiles)
            {
                AddSourceFile(sourceFile);
            }
        }

        _initialized = true;
    }

    public void AddSourceFile(SourceFileData sourceFileData)
    {
        string[] subPathParts = GetSubPathPartsForSourceFile(sourceFileData);

        ISourceFileClientModel sourceFileModel = SourceFileFactory.Create(sourceFileData);
        ISourceFileViewModel sourceFileViewModel = SourceFileFactory.Create(sourceFileModel);

        // If no sub path parts, just add to list of files
        if (subPathParts.Length == 0)
        {
            _files.Add(sourceFileViewModel);
            RegisterChildViewModel(sourceFileViewModel);
        }
        else
        {
            string firstSubPathPart = subPathParts.First();

            // Try to see if we have a subdirectory already, if not create it
            ISourceFilesSubdirectoryViewModel subdirectoryViewModel = _subdirectories.FirstOrDefault(_ => _.Name == firstSubPathPart);
            if (subdirectoryViewModel is null)
            {
                subdirectoryViewModel = SourceFileFactory.CreateSubdirectory(firstSubPathPart);
                RegisterChildViewModel(subdirectoryViewModel);
                _subdirectories.Add(subdirectoryViewModel);
            }

            subdirectoryViewModel.AddSourceFile(subPathParts.Skip(1), sourceFileViewModel);
        }
    }

    public bool RemoveSourceFile(SourceFileData sourceFileData)
    {
        ISourceFileViewModel sourceFile = _files.FirstOrDefault(f => f.Guid == sourceFileData.Guid);
        if (sourceFile is not null)
        {
            _files.Remove(sourceFile);
            SourceFileFactory.Release(sourceFile.GetModel());
            SourceFileFactory.Release(sourceFile);
            return true;
        }

        string[] subPathParts = GetSubPathPartsForSourceFile(sourceFileData);

        // Already checked files -- find subdirectory
        if (subPathParts.Length > 0)
        {
            string firstSubPathPart = subPathParts.First();
            ISourceFilesSubdirectoryViewModel subdirectoryViewModel = _subdirectories.FirstOrDefault(_ => _.Name == firstSubPathPart);
            if (subdirectoryViewModel is not null)
            {
                if (subdirectoryViewModel.TryRemoveSourceFile(sourceFileData.Guid, subPathParts.Skip(1), out sourceFile) is true)
                {
                    // If subdirectory has no more files or subdirectories, just delete it
                    if (subdirectoryViewModel.Any() is false)
                    {
                        _subdirectories.Remove(subdirectoryViewModel);
                        SourceFileFactory.Release(subdirectoryViewModel);
                    }

                    SourceFileFactory.Release(sourceFile.GetModel());
                    SourceFileFactory.Release(sourceFile);

                    return true;
                }
            }
        }

        // If none of the subdirectories find/remove the file, return false indicating it does not exist here
        return false;
    }

    public bool UpdateSourceFile(SourceFileData sourceFileData)
    {
        ISourceFileViewModel sourceFileViewModel = _files.FirstOrDefault(f => f.Guid == sourceFileData.Guid);
        if (sourceFileViewModel is not null)
        {
            sourceFileViewModel.Update(sourceFileData);
            return true;
        }

        string[] subPathParts = GetSubPathPartsForSourceFile(sourceFileData);

        if (subPathParts.Length > 0)
        {
            string firstSubPathPart = subPathParts.First();
            ISourceFilesSubdirectoryViewModel subdirectoryViewModel = _subdirectories.FirstOrDefault(_ => _.Name == firstSubPathPart);
            return subdirectoryViewModel?.UpdateSourceFile(sourceFileData, subPathParts.Skip(1)) ?? false;
        }

        return false;
    }

    #region Private Methods
    private static string[] GetSubPathPartsForSourceFile(SourceFileData sourceFileData)
    {
        string pathWithoutSourceAndFilename = sourceFileData.FullPath.Replace(sourceFileData.SourceDirectory, string.Empty).Replace(sourceFileData.FileName, string.Empty);

        char directorySeparator = System.IO.Path.DirectorySeparatorChar;
        if (pathWithoutSourceAndFilename.Contains(System.IO.Path.AltDirectorySeparatorChar))
            directorySeparator = System.IO.Path.AltDirectorySeparatorChar;

        return pathWithoutSourceAndFilename.Split(directorySeparator, StringSplitOptions.RemoveEmptyEntries);
    }
    #endregion Private Methods
}
