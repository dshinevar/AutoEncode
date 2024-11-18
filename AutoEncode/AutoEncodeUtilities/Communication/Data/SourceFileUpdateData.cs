using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;

namespace AutoEncodeUtilities.Communication.Data;

public record SourceFileUpdateData(SourceFileUpdateType Type, SourceFileData SourceFile);
