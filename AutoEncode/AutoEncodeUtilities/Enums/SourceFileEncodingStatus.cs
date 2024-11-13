using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Enums;

public enum SourceFileEncodingStatus : byte
{
    [Display(Name = "Not Encoded")]
    NOT_ENCODED = 0,

    [Display(Name = "In Queue")]
    IN_QUEUE = 1,

    [Display(Name = "Encoded")]
    ENCODED = 2,
}
