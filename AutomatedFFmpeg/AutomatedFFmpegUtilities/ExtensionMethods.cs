using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary> Remove leading slash from string if it exists (usually useful if parsing directories/files). </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveLeadingSlash(this string s) => s[0] == Path.DirectorySeparatorChar ? s.Remove(0, 1) : s;
    }
}
