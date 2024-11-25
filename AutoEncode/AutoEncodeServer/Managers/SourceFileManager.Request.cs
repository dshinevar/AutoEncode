using AutoEncodeServer.Communication;
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
    private static readonly object _lock = new();

    #region Request Processing
    public Dictionary<string, IEnumerable<SourceFileData>> RequestSourceFiles()
    {
        if (_updatingSourceFilesMRE.WaitOne(TimeSpan.FromSeconds(45)))
        {
            lock (_lock)
            {
                return _sourceFiles.Values.GroupBy(sf => sf.SearchDirectoryName).ToDictionary(x => x.Key, x => x.Select(sf => sf.ToData()));
            }
        }

        return null;
    }

    private void RequestEncodingJobForSourceFile(Guid sourceFileGuid)
    {
        ISourceFileModel sourceFileModel = null;
        try
        {
            if (_updatingSourceFilesMRE.WaitOne(TimeSpan.FromSeconds(45)))
            {
                if (_sourceFiles.TryGetValue(sourceFileGuid, out sourceFileModel) is true)
                {
                    _encodingJobManager.AddCreateEncodingJobRequest(sourceFileModel);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Error occured requesting an encoding job for a source file.", nameof(SourceFileManager), new { SourceFile = sourceFileModel?.Filename });
        }
    }

    public void BulkRequestEncodingJob(IEnumerable<Guid> sourceFileGuids)
    {
        try
        {
            if (_updatingSourceFilesMRE.WaitOne(TimeSpan.FromSeconds(45)))
            {
                foreach (Guid sourceFileGuid in sourceFileGuids)
                {
                    if (_sourceFiles.TryGetValue(sourceFileGuid, out ISourceFileModel sourceFileModel) is true)
                    {
                        _encodingJobManager.AddCreateEncodingJobRequest(sourceFileModel);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Error occurred bulk requesting encoding jobs.", nameof(SourceFileManager));
        }
    }

    private void UpdateSourceFileEncodingStatusFromEncodingJobStatus(Guid sourceFileGuid, EncodingJobStatus encodingJobStatus)
    {
        if (_sourceFiles.TryGetValue(sourceFileGuid, out ISourceFileModel sourceFile) is true)
        {
            if (sourceFile.UpdateEncodingStatus(TranslateEncodingJobStatusToSourceFileEncodingStatus(encodingJobStatus)) is true)
            {
                SourceFileUpdateData update = new(SourceFileUpdateType.Update, sourceFile.ToData());
                (string topic, CommunicationMessage<ClientUpdateType> message) = ClientUpdateMessageFactory.CreateSourceFileUpdate([update]);
                ClientUpdatePublisher.AddClientUpdateRequest(topic, message);
            }
        }
    }
    #endregion Request Processing

    #region Add Requests
    public bool AddUpdateSourceFileEncodingStatusRequest(Guid sourceFileGuid, EncodingJobStatus encodingJobStatus)
        => Requests.TryAdd(() => UpdateSourceFileEncodingStatusFromEncodingJobStatus(sourceFileGuid, encodingJobStatus));

    public bool AddRequestEncodingJobForSourceFileRequest(Guid sourceFileGuid)
        => Requests.TryAdd(() => RequestEncodingJobForSourceFile(sourceFileGuid));

    public bool AddBulkRequestEncodingJobRequest(IEnumerable<Guid> sourceFileGuids)
        => Requests.TryAdd(() => BulkRequestEncodingJob(sourceFileGuids));
    #endregion Add Requests
}
