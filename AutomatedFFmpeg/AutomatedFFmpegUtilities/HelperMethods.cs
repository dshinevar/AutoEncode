using System;

namespace AutomatedFFmpegUtilities
{
    public static class HelperMethods
    {
        public static string ConvertSecondsToTimestamp(int seconds) => TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss"); 
    }
}
