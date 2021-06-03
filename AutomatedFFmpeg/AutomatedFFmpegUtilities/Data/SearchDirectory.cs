using System;

namespace AutomatedFFmpegUtilities.Data
{
    public class SearchDirectory : ICloneable
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public bool Automated { get; set; }
        public bool TVShowStructure { get; set; }

        public object Clone() => this.MemberwiseClone();
    }
}
