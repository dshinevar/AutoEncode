using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

        /// <summary>Get all flags from enum (excluding flag 0 if exists). Assumes enum is backed by an int.</summary>
        /// <param name="e">Flag enum</param>
        /// <returns>IEnumerable of Enums of all the flags</returns>
        public static IEnumerable<Enum> GetFlags(this Enum e) => Enum.GetValues(e.GetType()).Cast<Enum>().Where(x => !Equals((int)(object)x, 0) && e.HasFlag(x));
        public static string GetDisplayName(this Enum value) => value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>().GetName();
        public static string GetDescription(this Enum value) => value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>().GetDescription();
        public static string GetShortName(this Enum value) => value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>().GetShortName();

        #region IEnumerable Extensions
        public static void RemoveRange<T>(this List<T> list, IEnumerable<T> remove)
        {
            foreach (T item in remove)
            {
                list.Remove(item);
            }
        }

        public static IEnumerable<T> Except<T, V>(this IEnumerable<T> first, IEnumerable<V> second, Func<T, V, bool> comparer)
            => first.Where(f => second.Any(s => comparer(f, s)) is false);
        #endregion IEnumerable Extension
    }
}
