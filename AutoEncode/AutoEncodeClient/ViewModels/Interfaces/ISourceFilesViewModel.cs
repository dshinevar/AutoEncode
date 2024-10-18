using AutoEncodeClient.Collections;
using AutoEncodeUtilities.Data;
using System.Collections.Generic;
using System.Windows.Input;

namespace AutoEncodeClient.ViewModels.Interfaces;

public interface ISourceFilesViewModel
{
    ObservableDictionary<string, IEnumerable<SourceFileData>> MovieSourceFiles { get; }

    ObservableDictionary<string, IEnumerable<ShowSourceFileViewModel>> ShowSourceFiles { get; }

    ICommand RefreshSourceFilesCommand { get; }

    ICommand RequestEncodeCommand { get; }

    void RefreshSourceFiles();
}
