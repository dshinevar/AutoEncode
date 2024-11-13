using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;

namespace AutoEncodeUtilities.Communication.Data;

public class SourceFileUpdateData
{
    public SourceFileUpdateType Type { get; set; }

    public SourceFileData SourceFile { get; set; }
}
