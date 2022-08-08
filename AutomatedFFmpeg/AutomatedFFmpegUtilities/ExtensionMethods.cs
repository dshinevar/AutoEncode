using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AutomatedFFmpegUtilities
{
    public static class ExtensionMethods
    {
        public static T DeepClone<T>(this T source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (source is null)
            {
                return default;
            }

            var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
            var serializeSettings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source, serializeSettings), deserializeSettings);
        }

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

        public static string GetName(this Enum value) => value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>().GetName();
        public static string GetDescription(this Enum value) => value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>().GetDescription();
    }
}
