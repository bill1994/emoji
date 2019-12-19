using System;
using System.Collections.Generic;
using System.Linq;
using Kyub.Serialization.VWF.Extensions;
using Kyub.Serialization.VWF.Types;
using UnityObject = UnityEngine.Object;
using Kyub.Reflection;
using System.Reflection;

namespace Kyub.Serialization.VWF.Helpers
{
    public static class ReflectionHelper
    {
        public static List<string> IgnoredAssemblies = new List<string>
        {
            "UnityScript.Lang",
            "Boo.Lang.Parser",
            "Boo.Lang",
            "Boo.Lang.Compiler",
            "System.ComponentModel.DataAnnotations",
            "System.Xml.Linq",
            "ICSharpCode.NRefactory",
            "UnityScript",
            "Mono.Cecil",
            "nunit.framework",
            "AssetStoreToolsExtra",
            "AssetStoreTools",
            "Unity.PackageManager",
            "Unity.SerializationLogic",
            "Mono.Security",
            "System.Xml",
            "System.Configuration",
            "System",
            "Unity.IvyParser",
            "System.Core",
            "Unity.DataContract",
            "I18N.West",
            "I18N",
            "Unity.Locator",
            "mscorlib",
            "nunit.core",
            "nunit.core.interfaces",
            "Mono.Cecil.Mdb",
            "NSubstitute",
            "UnityVS.VersionSpecific",
            "SyntaxTree.VisualStudio.Unity.Bridge",
            "SyntaxTree.VisualStudio.Unity.Messaging",
            "UnityEngine.UI",
            "UnityEngine",
            "KSerializer",
        };

        public readonly static Func<Type, List<System.Reflection.MemberInfo>> CachedGetMembers;
        public readonly static Func<Type[]> CachedGetRuntimeTypes;

        readonly static Func<ItemTuple<Type, string>, System.Reflection.MemberInfo> _cachedGetMember;

        public static System.Reflection.MemberInfo CachedGetMember(Type objType, string memberName)
        {
            return _cachedGetMember(ItemTuple.Create(objType, memberName));
        }

        static ReflectionHelper()
        {
            CachedGetMembers = new Func<Type, List<System.Reflection.MemberInfo>>(type =>
                GetMembers(type).ToList()).Memoize();

            _cachedGetMember = new Func<ItemTuple<Type, string>, System.Reflection.MemberInfo>(tup =>
            {
                var members = tup.Item1.GetMember(tup.Item2, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (members.IsNullOrEmpty())
                    return null;
                return members[0];
            }).Memoize();

            CachedGetRuntimeTypes = new Func<Type[]>(() =>
            {
                Predicate<string> isIgnoredAssembly = name =>
                    name.Contains("Dbg") || name.Contains("Editor") || IgnoredAssemblies.Contains(name);
                return AppDomain.CurrentDomain.GetAssemblies()
                                              .Where(x => !isIgnoredAssembly(x.GetName().Name))
                                              .SelectMany(x => x.GetTypes())
                                              .ToArray();
            }).Memoize();
        }

        static IEnumerable<System.Reflection.MemberInfo> GetMembers(Type type)
        {
            var peak =  typeof(object);
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            var members = type.GetAllMembers(peak, flags);
            return members;
        }

        public static Type[] GetUnityEngineTypes()
        {
            return GetUnityEngineAssembly().GetTypes();
        }

        /// <summary>
        /// Returns the types of the specified members
        /// </summary>
        public static Type[] GetMembersTypes(System.Reflection.MemberInfo[] members)
        {
            return members.Select(x => GetDataType(x, null)).ToArray();
        }

        public static Type GetDataType(System.Reflection.MemberInfo memberInfo, Func<System.Reflection.MemberInfo, Type> fallbackType)
        {
            var field = memberInfo as System.Reflection.FieldInfo;
            if (field != null)
                return field.FieldType;

            var property = memberInfo as System.Reflection.PropertyInfo;
            if (property != null)
                return property.PropertyType;

            var method = memberInfo as System.Reflection.MethodInfo;
            if (method != null)
                return method.ReturnType;

            if (fallbackType == null)
                throw new InvalidOperationException("Member is not a field, property, method nor does it have a fallback type");

            return fallbackType(memberInfo);
        }

        /// <summary>
        /// Returns a reference to the unity engine assembly
        /// </summary>
        public static System.Reflection.Assembly GetUnityEngineAssembly()
        {
            return typeof(UnityObject).Assembly();
        }

        /// <summary>
        /// Returns all runtime UnityEngine types
        /// </summary>
        public static Type[] GetAllUnityEngineTypes()
        {
            return GetUnityEngineAssembly().GetTypes();
        }

        /// <summary>
        /// Retruns all UnityEngine types of the specified wantedType
        /// </summary>
        public static Type[] GetAllUnityEngineTypesOf<T>()
        {
            return GetAllUnityEngineTypesOf(typeof(T));
        }

        /// <summary>
        /// Retruns all UnityEngine types of the specified wantedType
        /// </summary>
        public static Type[] GetAllUnityEngineTypesOf(Type type)
        {
            return GetAllUnityEngineTypes().Where(type.IsAssignableFrom).ToArray();
        }

        /// <summary>
        /// Returns all user-types of the specified wantedType
        /// </summary>
        public static Type[] GetAllUserTypesOf<T>()
        {
            return GetAllUserTypesOf(typeof(T));
        }

        /// <summary>
        /// Returns all user-types of the specified wantedType
        /// </summary>
        public static Type[] GetAllUserTypesOf(Type type)
        {
            return CachedGetRuntimeTypes().Where(type.IsAssignableFrom).ToArray();
        }

        /// <summary>
        /// Returns all types (user (and/or) UnityEngine types) of the specified wantedType
        /// </summary>
        public static Type[] GetAllTypesOf<T>()
        {
            return GetAllTypesOf(typeof(T));
        }

        /// <summary>
        /// Returns all types (user (and/or) UnityEngine types) of the specified wantedType
        /// </summary>
        public static Type[] GetAllTypesOf(Type wantedType)
        {
            return GetAllUserTypesOf(wantedType).Concat(GetAllUnityEngineTypesOf(wantedType)).ToArray();
        }

        /// <summary>
        /// Returns all types in all assemblies in the current domain
        /// </summary>
        public static IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes());
        }
    }
}