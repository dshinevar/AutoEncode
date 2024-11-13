using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Data;
using System.Collections.Generic;
using System.ComponentModel;

namespace AutoEncodeClient.ViewModels.SourceFile.Interfaces;

public interface ISourceFilesDirectoryViewModel : IViewModel
{
    /// <summary>Name of directory -- dictated by server config's name (not by any path).</summary>
    string Name { get; }

    ICollectionView SubdirectoriesView { get; }

    ICollectionView FilesView { get; }

    void Initialize(IEnumerable<SourceFileData> sourceFiles);

    /// <summary>Adds the given source file to the directory. Will place into proper subdirectory if needed. Will create model/viewmodel from data.</summary>
    /// <param name="sourceFile">The source file data used.</param>
    void AddSourceFile(SourceFileData sourceFile);

    /// <summary>Finds and removes the source file identified by the given Guid. Handles container releasing of the file.</summary>
    /// <param name="sourceFileData">The source file's data.</param>
    /// <returns>True if source file found and removed. False otherwise.</returns>
    bool RemoveSourceFile(SourceFileData sourceFileData);

    bool UpdateSourceFile(SourceFileData sourceFileData);
}
