namespace AutomatedFFmpegUtilities.Config
{
    public class GlobalJobSettings
    {
        public int MaxNumberOfJobsInQueue { get; set; } = 20;
        public int HoursCompletedUntilRemoval { get; set; } = 1;
        public int HoursErroredUntilRemoval { get; set; } = 2;
    }
}
