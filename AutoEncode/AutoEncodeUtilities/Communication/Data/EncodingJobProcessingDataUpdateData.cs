using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;

namespace AutoEncodeUtilities.Communication.Data;

public class EncodingJobProcessingDataUpdateData
{
    public SourceStreamData SourceStreamData { get; set; }

    public EncodingInstructions EncodingInstructions { get; set; }

    public PostProcessingSettings PostProcessingSettings { get; set; }

    public PostProcessingFlags PostProcessingFlags { get; set; }

    public bool NeedsPostProcessing { get; set; }

    public EncodingCommandArguments EncodingCommandArguments { get; set; }
}
