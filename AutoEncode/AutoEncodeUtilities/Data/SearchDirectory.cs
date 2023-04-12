namespace AutoEncodeUtilities.Data
{
    public class SearchDirectory
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public bool Automated { get; set; }
        public bool TVShowStructure { get; set; }
        public PostProcessingSettings PostProcessing { get; set; }
    }
}
