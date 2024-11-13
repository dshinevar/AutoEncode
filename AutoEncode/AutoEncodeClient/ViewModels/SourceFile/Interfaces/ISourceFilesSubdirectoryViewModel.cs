using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AutoEncodeClient.ViewModels.SourceFile.Interfaces;

public interface ISourceFilesSubdirectoryViewModel : IViewModel
{
    /// <summary>Subdirectory name -- dictated by the path.</summary>
    string Name { get; }

    ICollectionView SubdirectoriesView { get; }

    ICollectionView FilesView { get; }

    /// <summary>Indicates if the subdirectory has any files or further subdirectories.</summary>
    /// <returns>True if anything exists; False, otherwise.</returns>
    bool Any();

    IEnumerable<Guid> GetSourceFileGuids();

    /// <summary>Adds the given source file view model to the subdirectory. Will add to any further subdirectories dictated by the RemainingSubPathParts.</summary>
    /// <param name="remainingSubPathParts"></param>
    /// <param name="sourceFileViewModel">Source file to add</param>
    void AddSourceFile(IEnumerable<string> remainingSubPathParts, ISourceFileViewModel sourceFileViewModel);

    /// <summary>Attempts to find and remove the source file indicated by the given Guid. DOES NOT RELEASE FROM CONTAINER -- only removes from file/subdirectory lists. </summary>
    /// <param name="sourceFileGuid">The source file's Guid.</param>
    /// <param name="sourceFile">The source file (if found).</param>
    /// <returns>True if found and removed; False, otherwise.</returns>
    bool TryRemoveSourceFile(Guid sourceFileGuid, IEnumerable<string> remainingSubPathParts, out ISourceFileViewModel sourceFile);

    bool UpdateSourceFile(SourceFileData sourceFileData, IEnumerable<string> remainingSubPathParts);
}
