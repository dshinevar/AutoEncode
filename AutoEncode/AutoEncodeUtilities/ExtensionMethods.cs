using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace AutoEncodeUtilities
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

        public static void CopyProperties(this object source, object target)
        {
            if (source is null || target is null)
            {
                throw new Exception("Source/Target Object is null.");
            }

            Type sourceType = source.GetType();
            Type targetType = target.GetType();

            PropertyInfo[] srcProps = sourceType.GetProperties();
            foreach(PropertyInfo srcProp in srcProps) 
            {
                if (srcProp.CanRead is false) continue;

                PropertyInfo targetProp = targetType.GetProperty(srcProp.Name);
                if (targetProp is null) continue;
                if (targetProp.CanWrite is false) continue;

                MethodInfo targetPropSetMethodNonPublic = targetProp.GetSetMethod(true);
                if (targetPropSetMethodNonPublic is not null && targetPropSetMethodNonPublic.IsPrivate is true) continue;
                if ((targetProp.GetSetMethod().Attributes & MethodAttributes.Static) != 0) continue;

                if (targetProp.PropertyType.IsAssignableFrom(srcProp.PropertyType) is false) continue;

                targetProp.SetValue(target, srcProp.GetValue(source));
            }
        }

        public static bool IsValidJson(this string s)
        {
            if (string.IsNullOrWhiteSpace(s)) { return false; }
            if ((s.StartsWith("{") && s.EndsWith("}")) || (s.StartsWith("[") && s.EndsWith("]")))
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

        public static string GetDisplayName(this Enum value) => value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>().GetName();
        public static string GetDescription(this Enum value) => value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>().GetDescription();
    }
}
