using AutoEncodeUtilities.Interfaces;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Data
{
    public class PostProcessingSettings
        : IUpdateable<PostProcessingSettings>
    {
        public List<string> CopyFilePaths { get; set; }

        public bool DeleteSourceFile { get; set; }

        public void Update(PostProcessingSettings settings) => settings.CopyProperties(this);
    }
}
