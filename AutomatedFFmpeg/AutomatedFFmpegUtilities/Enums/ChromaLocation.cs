using System.ComponentModel;

namespace AutomatedFFmpegUtilities.Enums
{
    public enum ChromaLocation
    {
        [Description("Left")]
        LEFT_DEFAULT = 0,
        [Description("Center")]
        CENTER = 1,
        [Description("Top Left")]
        TOP_LEFT = 2,
        [Description("Top")]
        TOP = 3,
        [Description("Bottom Left")]
        BOTTOM_LEFT = 4,
        [Description("Bottom")]
        BOTTOM = 5
    }
}