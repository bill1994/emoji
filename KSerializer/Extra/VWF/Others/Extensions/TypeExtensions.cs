using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Kyub.Reflection;
using System.Reflection;

namespace Kyub.Serialization.VWF.Extensions
{
    public static class TypeExtensions
    {
        public static System.Reflection.MemberInfo GetMemberFromAll(this Type type, string memberName, Type peak, BindingFlags flags)
        {
            var result = GetAllMembers(type, peak, flags).FirstOrDefault(x => x.Name == memberName);
            return result;
        }

        public static System.Reflection.MemberInfo GetMemberFromAll(this Type type, string memberName, BindingFlags flags)
        {
            var peak = type.IsA<MonoBehaviour>() ? typeof(MonoBehaviour)
                     : type.IsA<ScriptableObject>() ? typeof(ScriptableObject)
                     : typeof(object);

            return GetMemberFromAll(type, memberName, peak, flags);
        }

        /// <summary>
        /// Returns all members (including private ones) from this type till peak
        /// http://stackoverflow.com/questions/1155529/not-getting-fields-from-gettype-getfields-with-bindingflag-default/1155549#1155549
        /// </summary>
        public static IEnumerable<System.Reflection.MemberInfo> GetAllMembers(this Type type, Type peak, BindingFlags flags)
        {
            if (type == null || type == peak)
                return Enumerable.Empty<System.Reflection.MemberInfo>();
            System.Type v_baseType = type.BaseType();
            IEnumerable<System.Reflection.MemberInfo> v_allBaseMembers = GetAllMembers(v_baseType, peak, flags);
            return type.GetMembers(flags).Concat(v_allBaseMembers);
        }

        public static readonly Dictionary<string, string> TypeNameAlternatives = new Dictionary<string, string>()
        {
            { "Single"   , "float"    },
            { "Int32"    , "int"      },
            { "String"   , "string"   },
            { "Boolean"  , "bool"     },
            { "Single[]" , "float[]"  },
            { "Int32[]"  , "int[]"    },
            { "String[]" , "string[]" },
            { "Boolean[]", "bool[]"   }
        };

        /// <summary>
        /// Used to filter out unwanted type names. Ex "int" instead of "Int32"
        /// </summary>
        public static string TypeNameGauntlet(this Type type)
        {
            string typeName = type.Name;
            if (typeName == "Object") // Could be a UnityEngine.Object, or System.Object - avoid confusion
            {
                typeName = typeof(UnityEngine.Object) == type ? "UnityObject" : "object";
            }
            else
            {
                string altTypeName = string.Empty;
                if (TypeNameAlternatives.TryGetValue(typeName, out altTypeName))
                    typeName = altTypeName;
            }
            return typeName;
        }

        private static Func<Type, string> _getNiceName;
        private static Func<Type, string> getNiceName
        {
            get
            {
                return _getNiceName ?? (_getNiceName = new Func<Type, string>(type =>
                {
                    if (type.IsArray)
                    {
                        int rank = type.GetArrayRank();
                        return type.GetElementType().GetNiceName() + (rank == 1 ? "[]" : "[,]");
                    }

                    if (type.IsSubclassOfRawGeneric(typeof(Nullable<>)))
                        return type.GetGenericArguments()[0].GetNiceName() + "?";

                    if (type.IsGenericParameter() || !type.IsGenericType())
                        return TypeNameGauntlet(type);

                    var builder = new StringBuilder();
                    var name = type.Name;
                    var index = name.IndexOf("`");
                    builder.Append(name.Substring(0, index));
                    builder.Append('<');
                    var args = type.GetGenericArguments();
                    for (int i = 0; i < args.Length; i++)
                    {
                        var arg = args[i];
                        if (i != 0)
                        {
                            builder.Append(", ");
                        }
                        builder.Append(GetNiceName(arg));
                    }
                    builder.Append('>');
                    return builder.ToString();
                }).Memoize());
            }
        }

        /// <summary>
        /// Ex: typeof(Dictionary<int, string>) => "Dictionary<int, string>"
        /// Credits to @jaredpar: http://stackoverflow.com/questions/401681/how-can-i-get-the-correct-text-definition-of-a-generic-type-using-reflection
        /// </summary>
        public static string GetNiceName(this Type type)
        {
            return getNiceName(type);
        }

        /// <summary>
        /// Generic version of IsSubclassOf(Type)
        /// </summary>
        public static bool IsSubclassOf<T>(this Type type)
        {
            return type.IsSubclassOf(typeof(T));
        }

        /// <summary>
        /// Returns true if the type exists within the hierarchy chain of the specified generic type
        /// (is equal to it or a subclass of it)
        /// </summary>
        public static bool IsA<T>(this Type type)
        {
            return type.IsA(typeof(T));
        }

        /// <summary>
        /// Returns true if the type exists within the hierarchy chain of the specified type
        /// (is equal to it or a subclass of it)
        /// </summary>
        public static bool IsA(this Type type, Type other)
        {
            return other.IsAssignableFrom(type);
        }

        /// <summary>
        /// Returns the first found custom attribute of type T on this type
        /// Returns null if none was found
        /// </summary>
        public static T GetCustomAttribute<T>(this Type type, bool inherit) where T : Attribute
        {
            var all = GetCustomAttributes<T>(type, inherit).ToArray();
            return all.IsNullOrEmpty() ? null : all[0];
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this Type type, bool inherit) where T : Attribute
        {
            return type.GetCustomAttributes(typeof(T), inherit).Cast<T>();
        }

        /// <summary>
        /// Alternative version of <see cref="Type.IsSubclassOf"/> that supports raw generic types (generic types without
        /// any type parameters).
        /// </summary>
        /// <param name="baseType">The base type class for which the check is made.</param>
        /// <param name="toCheck">To type to determine for whether it derives from <paramref name="baseType"/>.</param>
        /// Credits to JaredPar: http://stackoverflow.com/questions/457676/check-if-a-class-is-derived-from-a-generic-class
        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type baseType)
        {
            while (toCheck != typeof(object) && toCheck != null)
            {
                Type current = toCheck.IsGenericType() ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (current == baseType)
                    return true;
                toCheck = toCheck.BaseType();
            }
            return false;
        }
    }
}
