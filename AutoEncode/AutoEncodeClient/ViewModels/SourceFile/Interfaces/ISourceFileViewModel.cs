using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Windows.Input;

namespace AutoEncodeClient.ViewModels.SourceFile.Interfaces;

public interface ISourceFileViewModel : IViewModel
{
    Guid Guid { get; }

    string Filename { get; }

    string FullPath { get; }

    string DestinationFullPath { get; }

    SourceFileEncodingStatus EncodingStatus { get; }

    bool RequestingEncode { get; }

    ICommand RequestEncodeCommand { get; }

    void Update(SourceFileData sourceFileData);

    /// <summary>
    /// Gets the <see cref="ISourceFileClientModel"/> of the viewmodel.<br/>
    /// SHOULD ONLY BE USED FOR CONTAINER RELEASING.
    /// </summary>
    /// <returns>The ViewModel's Model.</returns>
    ISourceFileClientModel GetModel();
}
