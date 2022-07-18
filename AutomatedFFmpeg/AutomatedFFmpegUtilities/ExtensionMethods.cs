using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;

namespace AutomatedFFmpegUtilities
{
    public static class ExtensionMethods
    {
        public static bool IsValidJson(this string s)
        {
            if (string.IsNullOrWhiteSpace(s)) { return false; }
            if ((s.StartsWith("{") && s.EndsWith("}")) || ((s.StartsWith("[")) && (s.EndsWith("]"))))
            {
                try
                {
                    var obj = JToken.Parse(s);
                    return true;
                }
                catch (JsonReaderException ex)
                {
                    Debug.WriteLine(ex.Message);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary> Remove leading slashes from string if it exists (usually useful if parsing directories/files). </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveLeadingSlashes(this string s) => s.TrimStart(Path.DirectorySeparatorChar);

        /// <summary> Remove ending slashes from string if it exists (used with directories) </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveEndingSlashes(this string s) => s.TrimEnd(Path.DirectorySeparatorChar);
    }
}
