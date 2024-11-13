using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities.Data;
using System;

namespace AutoEncodeServer.Factories;

public interface IEncodingJobModelFactory
{
    /// <summary>Creates an <see cref="IEncodingJobModel"/> </summary>
    /// <param name="id">Id of the Job</param>
    /// <param name="sourceFileGuid">Links encoding job to a source file. </param>
    /// <param name="sourceFileFullPath">Full Path of the source file</param>
    /// <param name="destinationFileFullPath">Full Path of the expected destination file.</param>
    /// <param name="postProcessingSettings"><see cref="PostProcessingSettings"/> of the job</param>
    /// <returns><see cref="IEncodingJobModel"/></returns>
    IEncodingJobModel Create(ulong id, Guid sourceFileGuid, string sourceFileFullPath, string destinationFileFullPath, PostProcessingSettings postProcessingSettings);

    void Release(IEncodingJobModel encodingJobModel);
}
