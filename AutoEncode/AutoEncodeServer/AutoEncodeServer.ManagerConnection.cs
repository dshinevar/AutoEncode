using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities.Enums;
using System;

namespace AutoEncodeServer;

// MANAGER CONNECTION
// Implements ISourceFileManagerConnection, IEncodingJobManagerConnection
// and acts as the go between managers
public partial class AutoEncodeServer :
    ISourceFileManagerConnection,
    IEncodingJobManagerConnection
{
    #region ISourceFileManagerConnection
    /// <summary>Requests the SourceFileManager to update the source encoding status based off the given <see cref="EncodingJobStatus"/> </summary>
    /// <param name="sourceFileGuid">Id of the source file to update.</param>
    /// <param name="status"><see cref="EncodingJobStatus"/> to base status off of. Null indicates not in queue.</param>
    /// <returns>True, if request was added.</returns>
    public bool UpdateSourceFileEncodingStatus(Guid sourceFileGuid, EncodingJobStatus? status)
        => SourceFileManager.AddUpdateSourceFileEncodingStatusRequest(sourceFileGuid, status);
    #endregion ISourceFileManagerConnection


    #region IEncodingJobManagerConnection
    /// <summary>Requests the EncodingJobManager to create an encoding job based off the given source file.</summary>
    /// <param name="sourceFile"><see cref="ISourceFileModel"/> to create encoding job from.</param>
    /// <returns>True, if request was added.</returns>
    public bool CreateEncodingJob(ISourceFileModel sourceFile)
        => EncodingJobManager.AddCreateEncodingJobRequest(sourceFile);

    public EncodingJobStatus? GetEncodingJobStatusBySourceFileGuid(Guid sourceFileGuid)
        => EncodingJobManager.GetEncodingJobStatusBySourceFileGuid(sourceFileGuid);
    #endregion IEncodingJobManagerConnection
}
