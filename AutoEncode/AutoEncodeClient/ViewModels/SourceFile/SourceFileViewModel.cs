using AutoEncodeClient.Command;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.ViewModels.SourceFile.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Windows.Input;

namespace AutoEncodeClient.ViewModels.SourceFile;

public class SourceFileViewModel :
    ViewModelBase<ISourceFileClientModel>,
    ISourceFileViewModel
{
    #region Properties
    public Guid Guid => Model.Guid;

    public string Filename => Model.Filename;

    public string FullPath => Model.FullPath;

    public string DestinationFullPath => Model.DestinationFullPath;

    public SourceFileEncodingStatus EncodingStatus => Model.EncodingStatus;

    private bool _requestingEncode = false;
    public bool RequestingEncode
    {
        get => _requestingEncode;
        set => SetAndNotify(_requestingEncode, value, () => _requestingEncode = value);
    }
    #endregion Properties

    #region Commands
    public ICommand RequestEncodeCommand { get; set; }
    #endregion Commands

    public SourceFileViewModel(ISourceFileClientModel sourceFileClientModel)
        : base(sourceFileClientModel)
    {
        AECommand requestEncodeCommand = new(CanRequestEncode, RequestEncode);
        RequestEncodeCommand = requestEncodeCommand;
        AddCommand(requestEncodeCommand, nameof(RequestingEncode));
    }

    public void Update(SourceFileData sourceFileData)
        => Model.UpdateEncodingStatus(sourceFileData.EncodingStatus);

    public ISourceFileClientModel GetModel() => Model;

    #region Command Methods
    private bool CanRequestEncode() => RequestingEncode is false;
    private async void RequestEncode()
    {
        RequestingEncode = true;

        bool result = await Model.RequestEncode();

        if (result is false)
        {
            ShowErrorDialog($"Failed to add a request to encode {Filename}.", "Encode Request Failed");
        }

        RequestingEncode = false;
    }
    #endregion Command Methods
}
