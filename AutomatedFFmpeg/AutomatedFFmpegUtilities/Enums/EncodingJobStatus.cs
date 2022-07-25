using System.ComponentModel.DataAnnotations;

namespace AutomatedFFmpegUtilities.Enums
{
    public enum EncodingJobStatus
    {
        [Display(Name = "New", Description = "New", ShortName = "New")]
        NEW = 0,
        [Display(Name = "Analyzing", Description = "Analyzing", ShortName = "Analyzing")]
        ANALYZING = 1,
        [Display(Name = "Analyzed", Description = "Analyzed", ShortName = "Analyzed")]
        ANALYZED = 2,
        [Display(Name = "Encoding", Description = "Encoding", ShortName = "Encoding")]
        ENCODING = 3,
        [Display(Name = "Encoded", Description = "Encoded", ShortName = "Encoded")]
        ENCODED = 4
    }
}
