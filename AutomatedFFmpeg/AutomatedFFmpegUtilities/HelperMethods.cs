using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomatedFFmpegUtilities
{
    public static class HelperMethods
    {
        public static string ConvertSecondsToTimestamp(int seconds) => TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");

        public static int ConvertTimestampToSeconds(string timestamp) => TimeSpan.TryParse(timestamp, out TimeSpan ts) ? Convert.ToInt32(ts.TotalSeconds) : -1;

        public static string JoinFilter(string separator, params string[] strings)
        {
            IEnumerable<string> nonEmptyStrings = strings?.Where(s => !string.IsNullOrEmpty(s));

            if (!nonEmptyStrings?.Any() ?? true)
            {
                return string.Empty;
            }
            else if (nonEmptyStrings.Count() == 1)
            {
                return nonEmptyStrings.Single();
            }
            else
            {
                return string.Join(separator, nonEmptyStrings);
            }
        }
    }
}
