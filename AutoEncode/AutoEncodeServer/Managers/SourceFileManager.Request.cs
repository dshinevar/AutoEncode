using AutoEncodeServer.Communication;
using AutoEncodeServer.Data.Request;
using AutoEncodeServer.Enums;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoEncodeServer.Managers;

// REQUESTS
public partial class SourceFileManager : ISourceFileManager
{
    private readonly object _lock = new();

    protected override void ProcessManagerRequest(ManagerRequest request)
    {
        try
        {
            switch (request.Type)
            {
                case ManagerRequestType.UpdateSourceFileEncodingStatus:
                {
                    if (request is ManagerRequest<UpdateSourceFileEncodingStatusRequest> updateRequest)
                    {
                        UpdateSourceFileEncodingStatusFromEncodingJobStatus(updateRequest.RequestData.SourceFileGuid, updateRequest.RequestData.EncodingJobStatus);
                    }
                    break;
                }
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Error processing request {request.Type}.", nameof(SourceFileManager));
        }
    }

    #region Request Processing
    public Dictionary<string, IEnumerable<SourceFileData>> RequestSourceFiles()
    {
        if (_buildingSourceFilesEvent.WaitOne(TimeSpan.FromSeconds(45)))
        {
            lock (_lock)
            {
                return _sourceFiles.Values.GroupBy(sf => sf.SearchDirectoryName).ToDictionary(x => x.Key, x => x.Select(sf => sf.ToData()));
            }
        }

        return null;
    }

    public bool RequestEncodingJob(Guid sourceFileGuid)
    {
        if (_buildingSourceFilesEvent.WaitOne(TimeSpan.FromSeconds(45)))
        {
            if (_sourceFiles.TryGetValue(sourceFileGuid, out ISourceFileModel sourceFileModel) is true)
            {
                return _encodingJobManager.AddCreateEncodingJobRequest(sourceFileModel);
            }
        }

        return false;
    }

    public IEnumerable<string> BulkRequestEncodingJob(IEnumerable<Guid> sourceFileGuids)
    {
        List<string> failedRequests = [];

        if (_buildingSourceFilesEvent.WaitOne(TimeSpan.FromSeconds(45)))
        {
            foreach (Guid sourceFileGuid in sourceFileGuids)
            {
                if (_sourceFiles.TryGetValue(sourceFileGuid, out ISourceFileModel sourceFileModel) is true)
                {
                    if (_encodingJobManager.AddCreateEncodingJobRequest(sourceFileModel) is false)
                    {
                        failedRequests.Add(sourceFileModel.Filename);
                    }
                }
            }
        }

        return failedRequests;
    }

    private void UpdateSourceFileEncodingStatusFromEncodingJobStatus(Guid sourceFileGuid, EncodingJobStatus encodingJobStatus)
    {
        if (_sourceFiles.TryGetValue(sourceFileGuid, out ISourceFileModel sourceFile) is true)
        {
            if (sourceFile.UpdateEncodingStatus(TranslateEncodingJobStatusToSourceFileEncodingStatus(encodingJobStatus)) is true)
            {
                SourceFileUpdateData update = new()
                {
                    Type = SourceFileUpdateType.Update,
                    SourceFile = sourceFile.ToData()
                };
                (string topic, ClientUpdateMessage message) = ClientUpdateMessageFactory.CreateSourceFileUpdate([update]);
                ClientUpdatePublisher.AddClientUpdateRequest(topic, message);
            }
        }
    }
    #endregion Request Processing

    #region Add Requests
    public bool AddUpdateSourceFileEncodingStatusRequest(Guid sourceFileGuid, EncodingJobStatus encodingJobStatus)
        => TryAddRequest(new ManagerRequest<UpdateSourceFileEncodingStatusRequest>()
        {
            Type = ManagerRequestType.UpdateSourceFileEncodingStatus,
            RequestData = new()
            {
                SourceFileGuid = sourceFileGuid,
                EncodingJobStatus = encodingJobStatus
            }
        });
    #endregion Add Requests
}
