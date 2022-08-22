namespace AutomatedFFmpegUtilities.Config
{
    public class GlobalJobSettings
    {
        public int HoursCompletedUntilRemoval { get; set; } = 1;
        public int HoursErroredUntilRemoval { get; set; } = 2;
    }
}
