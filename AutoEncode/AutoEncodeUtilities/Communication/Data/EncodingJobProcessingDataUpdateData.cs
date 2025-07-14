using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;

namespace AutoEncodeUtilities.Communication.Data;

/// <summary>Encapsulates data describing an encoding job's processing data.</summary>
public record EncodingJobProcessingDataUpdateData
{
    /// <summary>Title pulled from file probe.</summary>
    /// <remarks>This can differ from the Name pulled from FileName which is why it can be updated.</remarks>
    public string Title { get; set; }

    public SourceStreamData SourceStreamData { get; set; }

    public EncodingInstructions EncodingInstructions { get; set; }

    public PostProcessingSettings PostProcessingSettings { get; set; }

    public PostProcessingFlags PostProcessingFlags { get; set; }

    public bool NeedsPostProcessing { get; set; }

    public EncodingCommandArguments EncodingCommandArguments { get; set; }
}
