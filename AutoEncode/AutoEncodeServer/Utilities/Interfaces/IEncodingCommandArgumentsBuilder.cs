using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities.Data;

namespace AutoEncodeServer.Utilities.Interfaces;

public interface IEncodingCommandArgumentsBuilder
{
    EncodingCommandArguments Build(IEncodingJobData encodingJobData);
}
