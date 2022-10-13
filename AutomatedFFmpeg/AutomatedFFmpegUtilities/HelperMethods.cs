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

        /// <summary>Same as JoinFilter but wraps the strings in the given open and close strings </summary>
        /// <param name="separator">String the separates the strings</param>
        /// <param name="open">Open string</param>
        /// <param name="close">Close string</param>
        /// <param name="strings">List of strings</param>
        /// <returns></returns>
        public static string JoinFilterWrap(string separator, string open, string close, params string[] strings)
        {
            IEnumerable<string> nonEmptyStrings = strings?.Where(s => !string.IsNullOrEmpty(s)).ToList();

            if (!nonEmptyStrings?.Any() ?? true)
            {
                return string.Empty;
            }
            else if (nonEmptyStrings.Count() == 1)
            {
                return $"{open}{nonEmptyStrings.Single()}{close}";
            }
            else
            {
                return string.Join(separator, nonEmptyStrings.Select(x => $"{open}{x}{close}"));
            }
        }

        public static string FormatEncodingTime(TimeSpan ts) => (ts.Days > 0) ? $"{ts:d\\d\\ hh\\:mm\\:ss}" : $"{ts:hh\\:mm\\:ss}";

        public static bool IsAnyNullOrWhitespace(object obj) => obj.GetType().GetProperties().Where(pi => pi.PropertyType == typeof(string))
                                                                            .Select(pi => (string)pi.GetValue(obj)).Any(value => string.IsNullOrWhiteSpace(value));
    }
}
