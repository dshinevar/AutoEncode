using AutoEncodeUtilities.Data;

namespace AutoEncodeServer.Utilities.Data;

public class SourceFileProbeResultData
{
    /// <summary>The title of the source file determined from probe (NOT the FileName) </summary>
    public string TitleOfSourceFile { get; set; }

    public SourceStreamData SourceStreamData { get; set; }

}
