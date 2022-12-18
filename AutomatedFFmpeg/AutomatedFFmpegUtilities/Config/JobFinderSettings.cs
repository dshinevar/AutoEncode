using System;

namespace AutomatedFFmpegUtilities.Config
{
    public class JobFinderSettings
    {
        public string[] VideoFileExtensions { get; set; } = new[] { ".mkv", ".m4v", ".avi" };

        public string SecondarySkipExtension { get; set; } = "skip";

        public double ThreadSleepInMinutes { get; set; } = 5;

        public TimeSpan ThreadSleep => TimeSpan.FromMinutes(ThreadSleepInMinutes);
    }
}
