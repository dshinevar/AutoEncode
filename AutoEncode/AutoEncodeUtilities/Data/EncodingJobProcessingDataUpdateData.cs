using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using AutoEncodeUtilities.Json;
using Newtonsoft.Json;

namespace AutoEncodeUtilities.Data
{
    public class EncodingJobProcessingDataUpdateData
    {
        public ISourceStreamData SourceStreamData { get; set; }

        public EncodingInstructions EncodingInstructions { get; set; }

        public PostProcessingSettings PostProcessingSettings { get; set; }

        public PostProcessingFlags PostProcessingFlags { get; set; }

        public bool NeedsPostProcessing { get; set; }

        [JsonConverter(typeof(EncodingCommandArgumentsConverter<IEncodingCommandArguments>))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IEncodingCommandArguments CommandArguments { get; set; }
    }
}
