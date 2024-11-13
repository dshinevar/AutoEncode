using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.ViewModels.SourceFile.Interfaces;
using AutoEncodeUtilities.Data;

namespace AutoEncodeClient.Factories;

/// <summary>Factory used for creating any source file models.</summary>
public interface ISourceFileFactory
{
    #region SourceFileClientModel
    ISourceFileClientModel Create(SourceFileData sourceFileData);

    void Release(ISourceFileClientModel model);
    #endregion SourceFileClientModel


    #region SourceFileViewModel
    ISourceFileViewModel Create(ISourceFileClientModel sourceFileClientModel);

    void Release(ISourceFileViewModel viewModel);
    #endregion SourceFileViewModel


    #region SourceFilesSubdirectoryViewModel
    ISourceFilesSubdirectoryViewModel CreateSubdirectory(string name);

    void Release(ISourceFilesSubdirectoryViewModel viewModel);
    #endregion SourceFilesSubdirectoryViewModel


    #region SourceFilesDirectoryViewModel
    ISourceFilesDirectoryViewModel CreateDirectory(string name);

    void Release(ISourceFilesDirectoryViewModel viewModel);
    #endregion SourceFilesDirectoryViewModel
}
