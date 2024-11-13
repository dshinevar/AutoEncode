using AutoEncodeClient.Communication.Interfaces;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Base;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using System;
using System.Threading.Tasks;

namespace AutoEncodeClient.Models;

public class SourceFileClientModel :
    ModelBase,
    ISourceFileClientModel
{
    #region Dependencies
    public ILogger Logger { get; set; }

    public ICommunicationMessageHandler CommunicationMessageHandler { get; set; }
    #endregion Dependencies

    #region Properties
    public Guid Guid { get; set; }
    public string Filename { get; set; }
    public string FullPath { get; set; }
    public string DestinationFullPath { get; set; }

    private SourceFileEncodingStatus _encodingStatus;
    public SourceFileEncodingStatus EncodingStatus
    {
        get => _encodingStatus;
        set => SetAndNotify(_encodingStatus, value, () => _encodingStatus = value);
    }
    #endregion Properties

    public SourceFileClientModel(SourceFileData sourceFileData)
    {
        sourceFileData.CopyProperties(this);
    }

    public void UpdateEncodingStatus(SourceFileEncodingStatus encodingStatus)
        => EncodingStatus = encodingStatus;

    public async Task<bool> RequestEncode() => await CommunicationMessageHandler.RequestEncode(Guid);
}
