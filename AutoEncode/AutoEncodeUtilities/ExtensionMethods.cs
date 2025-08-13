using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace AutoEncodeUtilities;

public static class ExtensionMethods
{
    public static bool IsNullOrDefault<T>(this T obj)
    {
        // Typical checks
        if (obj is null) return true;
        if (Equals(obj, default)) return true;

        // Non-null nullables
        Type methodType = typeof(T);
        if (Nullable.GetUnderlyingType(methodType) != null) return false;

        // Boxed values
        Type objType = obj.GetType();
        if (objType.IsValueType && objType != methodType)
        {
            object testObj = Activator.CreateInstance(objType);
            return testObj.Equals(obj);
        }

        return false;
    }

    public static T DeepClone<T>(this T source)
    {
        // Don't serialize a null object, simply return the default for that object
        if (source is null)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(source));
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
        foreach (PropertyInfo srcProp in srcProps)
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
        if (string.IsNullOrWhiteSpace(s)) return false;
        if ((s.StartsWith('{') && s.EndsWith('}')) || (s.StartsWith('[') && s.EndsWith(']')))
        {
            try
            {
                _ = JsonDocument.Parse(s);

                return true;
            }
            catch (JsonException ex)
            {
                HelperMethods.DebugLog(ex.Message, nameof(IsValidJson));
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public static string Indent(this string s, int indent)
        => s.PadLeft(indent + s.Length, ' ');

    /// <summary>Get all flags from enum (excluding flag 0 if exists). Assumes enum is backed by an int.</summary>
    /// <param name="e">Flag enum</param>
    /// <returns>IEnumerable of Enums of all the flags</returns>
    public static IEnumerable<Enum> GetFlags(this Enum e) => Enum.GetValues(e.GetType()).Cast<Enum>().Where(x => !Equals((int)(object)x, 0) && e.HasFlag(x));
    public static string GetDisplayName(this Enum value) => value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>().GetName();
    public static string GetDescription(this Enum value) => value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>().GetDescription();
    public static string GetShortName(this Enum value) => value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>().GetShortName();

    #region IEnumerable / IList Extensions
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
    {
        if (list is List<T> asList)
        {
            asList.AddRange(items);
        }
        else
        {
            foreach (T item in items)
            {
                list.Add(item);
            }
        }
    }

    public static void RemoveRange<T>(this List<T> list, IEnumerable<T> remove)
    {
        foreach (T item in remove.ToList())
        {
            list.Remove(item);
        }
    }

    public static IEnumerable<T> Except<T, V>(this IEnumerable<T> first, IEnumerable<V> second, Func<T, V, bool> comparer)
        => first.Where(f => second.Any(s => comparer(f, s)) is false);
    #endregion IEnumerable / IList Extension
}
