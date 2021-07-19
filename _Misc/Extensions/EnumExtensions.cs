#if (UNITY_WINRT || UNITY_W8_1) && !UNITY_EDITOR && !UNITY_WP8
#define RT_ENABLED
#endif

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Kyub.Reflection;
using System.Reflection;

namespace Kyub.Extensions
{
    public static class EnumExtensions
    {
        public static bool CheckIfIsEnum(System.Type type, bool withFlags)
        {
            if (type == null)
                return false;
            if (!type.IsEnum())
                return false;
            if (withFlags && !type.IsDefined(typeof(System.FlagsAttribute), true))
                return false;
            return true;
        }

        #region Strong-Typed Enums

#if !RT_ENABLED
        private static T SetFlags<T>(this T value, T flags, bool on) where T : struct, System.IConvertible
#else
	    private static T SetFlags<T>(this T value, T flags, bool on) where T : struct
#endif
        {
            if (CheckIfIsEnum<T>(true))
            {
                try
                {
                    long lValue = System.Convert.ToInt64(value);
                    long lFlag = System.Convert.ToInt64(flags);
                    if (on)
                    {
                        lValue |= lFlag;
                    }
                    else
                    {
                        lValue &= (~lFlag);
                    }
                    return (T)System.Enum.ToObject(typeof(T), lValue);
                }
                catch { }
            }
            if (on)
                return flags;
            else
                return value;
        }

#if !RT_ENABLED
        public static bool CheckIfIsEnum<T>(bool withFlags) where T : struct, System.IConvertible
#else
	    public static bool CheckIfIsEnum<T>(bool withFlags) where T : struct
#endif
        {
		    if (!typeof(T).IsEnum())
			    return false;

            if (withFlags && !typeof(T).IsDefined(typeof(System.FlagsAttribute), true))
                return false;
            return true;
        }

#if !RT_ENABLED
        public static List<T> GetFlags<T>(this T value) where T : struct, System.IConvertible
#else
	    public static List<T> GetFlags<T>(this T value) where T : struct
#endif
        {
            List<T> flags = new List<T>();
            if (CheckIfIsEnum<T>(true))
            {
                foreach (T flag in System.Enum.GetValues(typeof(T)))
                {
                    if (value.ContainsFlag(flag) && !flags.Contains(flag))
                        flags.Add(flag);
                }
            }
            else if (CheckIfIsEnum<T>(false))
            {
                if(!flags.Contains(value))
                    flags.Add(value);
            }
            return flags;
        }

#if !RT_ENABLED
        public static bool ContainsFlag<T>(this T value, T flag) where T : struct, System.IConvertible
#else
	    public static bool ContainsFlag<T>(this T value, T flag) where T : struct
#endif
        {
            if (CheckIfIsEnum<T>(true))
            {
                try
                {
                    long lValue = System.Convert.ToInt64(value);
                    long lFlag = System.Convert.ToInt64(flag);
                    return (lValue & lFlag) == lFlag;
                }
                catch { }
            }
            if (CheckIfIsEnum<T>(false))
            {
                if (EqualityComparer<T>.Default.Equals(value, flag))
                    return true;
            }
            return false;
        }

#if !RT_ENABLED
        public static T SetFlags<T>(this T value, T flags) where T : struct, System.IConvertible
#else
	    public static T SetFlags<T>(this T value, T flags) where T : struct
#endif
        {
            return value.SetFlags(flags, true);
        }

#if !RT_ENABLED
        public static T ClearFlags<T>(this T value, T flags) where T : struct, System.IConvertible
#else
	    public static T ClearFlags<T>(this T value, T flags) where T : struct
#endif
        {
            return value.SetFlags(flags, false);
        }

#if !RT_ENABLED
        public static string ToDescriptionString<T>(this T value, bool useValueAsDefaultDescription = false) where T : struct, System.IConvertible
#else
	    public static string ToDescriptionString<T>(this T value, bool useValueAsDefaultDescription = false) where T : struct
#endif
        {
            var type = value.GetType();
            List<string> fieldNames = new List<string>();
            if (!type.IsDefined(typeof(System.FlagsAttribute), true))
            {
                var fieldName = value.ToString();
                if (!string.IsNullOrEmpty(fieldName))
                    fieldNames.Add(fieldName);
            }
            else
            {
                long instanceValue = System.Convert.ToInt64(value);
                var flagValues = System.Enum.GetValues(type);
                foreach (var flagValue in flagValues)
                {
                    long integralValue = System.Convert.ToInt64(flagValue);
                    bool isSet = (instanceValue & integralValue) != 0;

                    if (isSet)
                    {

                        var flagName = flagValue.ToString();
                        if (!string.IsNullOrEmpty(flagName))
                            fieldNames.Add(flagName);

                    }
                }
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (var fieldName in fieldNames)
            {
                try
                {
                    var field = type.GetField(fieldName);
                    if (field != null)
                    {
                        object[] atts = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
                        var textValue = atts.Length > 0 && atts[0] is DescriptionAttribute ? ((DescriptionAttribute)atts[0]).Description : 
                            (useValueAsDefaultDescription? fieldName : string.Empty);

                        if (!string.IsNullOrEmpty(textValue))
                        {
                            if (builder.Length > 0)
                                builder.Append(", ");
                            builder.Append(textValue);
                        }
                    }
                }
                catch { }
            }
            return builder.ToString();
        }

        #endregion
    }
}
