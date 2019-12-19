#if (UNITY_WINRT || UNITY_WP_8_1) && !UNITY_EDITOR && !UNITY_WP8
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
        public static bool CheckIfIsEnum(System.Type p_type, bool withFlags)
        {
            if (p_type == null)
                return false;
            if (!p_type.IsEnum())
                return false;
            if (withFlags && !p_type.IsDefined(typeof(System.FlagsAttribute), true))
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
            List<T> v_flags = new List<T>();
            if (CheckIfIsEnum<T>(true))
            {
                foreach (T flag in System.Enum.GetValues(typeof(T)))
                {
                    if (value.ContainsFlag(flag) && !v_flags.Contains(flag))
                        v_flags.Add(flag);
                }
            }
            else if (CheckIfIsEnum<T>(false))
            {
                if(!v_flags.Contains(value))
                    v_flags.Add(value);
            }
            return v_flags;
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
        public static string ToDescriptionString<T>(this T value) where T : struct, System.IConvertible
#else
	    public static string ToDescriptionString<T>(this T value) where T : struct
#endif
        {
            DescriptionAttribute[] v_attr = (DescriptionAttribute[])value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
            return v_attr.Length > 0 ? v_attr[0].Description : string.Empty;
        }

        #endregion
    }
}
