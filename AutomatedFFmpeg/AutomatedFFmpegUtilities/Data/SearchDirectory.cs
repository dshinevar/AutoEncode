using System.Collections.Generic;

namespace AutomatedFFmpegUtilities.Data
{
    public class SearchDirectory
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public bool Automated { get; set; }
        public bool TVShowStructure { get; set; }
        public PostProcessingSettings PostProcessing { get; set; }
    }

    public class PostProcessingSettings
    {
        public List<string> CopyFilePaths { get; set; }

        public bool DeleteSourceFile { get; set; }
    }
}
