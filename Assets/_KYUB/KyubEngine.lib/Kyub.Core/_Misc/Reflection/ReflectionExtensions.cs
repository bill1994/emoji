#if (UNITY_WINRT || UNITY_WP_8_1) && !UNITY_EDITOR && !UNITY_WP8
#define RT_ENABLED
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Kyub.Reflection
{
	public static class ReflectionExtensions
	{

#if RT_ENABLED
		private static BindingFlags DefaultFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
#endif

        #region Hybrid Functions

        public static bool IsPrimitive(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsPrimitive;
#else
            return type.IsPrimitive;
#endif
        }

        public static bool IsSubclassOf(this Type type, Type c)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsSubclassOf(c);
#else
            return type.IsSubclassOf(c);
#endif
        }

        public static bool IsAssignableFrom(this Type type, Type c)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsAssignableFrom(c.GetTypeInfo());
#else
            return type.IsAssignableFrom(c);
#endif
        }

        public static MethodInfo Method(this Delegate d)
        {
#if RT_ENABLED
            return d.GetMethodInfo();
#else
            return d.Method;
#endif
        }

        public static MemberTypes MemberType(this MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
                return MemberTypes.Property;
            else if (memberInfo is FieldInfo)
                return MemberTypes.Field;
            else if (memberInfo is EventInfo)
                return MemberTypes.Event;
            else if (memberInfo is MethodInfo)
                return MemberTypes.Method;
            else
                return MemberTypes.Custom;
        }

        public static bool IsInterface(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsInterface;
#else
            return type.IsInterface;
#endif
        }

        public static bool IsArray(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsArray;
#else
            return type.IsArray;
#endif
        }

        public static bool IsGenericParameter(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsGenericParameter;
#else
            return type.IsGenericParameter;
#endif
        }

        public static bool IsNested(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsNested;
#else
            return type.IsNested;
#endif
        }

        public static bool IsGenericType(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsGenericType;
#else
            return type.IsGenericType;
#endif
        }

        public static bool IsGenericTypeDefinition(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsGenericTypeDefinition;
#else
            return type.IsGenericTypeDefinition;
#endif
        }

        public static Type BaseType(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().BaseType;
#else
            return type.BaseType;
#endif
        }

        public static bool IsEnum(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsEnum;
#else
            return type.IsEnum;
#endif
        }

        public static bool IsClass(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsClass;
#else
            return type.IsClass;
#endif
        }

        public static bool IsSealed(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsSealed;
#else
            return type.IsSealed;
#endif
        }

        public static bool IsAbstract(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsAbstract;
#else
            return type.IsAbstract;
#endif
        }

        public static bool IsVisible(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsVisible;
#else
            return type.IsVisible;
#endif
        }

        public static bool IsValueType(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().IsValueType;
#else
            return type.IsValueType;
#endif
        }

        public static Assembly Assembly(this Type type)
        {
#if RT_ENABLED
            return type.GetTypeInfo().Assembly;
#else
            return type.Assembly;
#endif
        }

        #endregion

        #region RT Missing Methods

#if RT_ENABLED

        public static MethodInfo GetGetMethod(this PropertyInfo propertyInfo)
		{
			return propertyInfo.GetGetMethod(false);
		}

		public static MethodInfo GetGetMethod(this PropertyInfo propertyInfo, bool nonPublic)
		{
			MethodInfo getMethod = propertyInfo.GetMethod;
			if (getMethod != null && (getMethod.IsPublic || nonPublic))
				return getMethod;

			return null;
		}

		public static MethodInfo GetSetMethod(this PropertyInfo propertyInfo)
		{
			return propertyInfo.GetSetMethod(false);
		}

		public static MethodInfo GetSetMethod(this PropertyInfo propertyInfo, bool nonPublic)
		{
			MethodInfo setMethod = propertyInfo.SetMethod;
			if (setMethod != null && (setMethod.IsPublic || nonPublic))
				return setMethod;

			return null;
		}

		public static bool ContainsGenericParameters(this Type type)
		{
			return type.GetTypeInfo().ContainsGenericParameters;
		}

		public static MethodInfo GetBaseDefinition(this MethodInfo method)
		{
			return method.GetRuntimeBaseDefinition();
		}

		public static bool IsDefined(this Type type, Type attributeType, bool inherit)
		{
			return type.GetTypeInfo().CustomAttributes.Any(a => a.AttributeType == attributeType);
		}

		public static MethodInfo GetMethod(this Type type, string name)
		{
			return type.GetMethod(name, DefaultFlags);
		}

		public static MethodInfo GetMethod(this Type type, string name, BindingFlags bindingFlags)
		{
			return type.GetTypeInfo().GetDeclaredMethod(name);
		}

		public static MethodInfo GetMethod(this Type type, IList<Type> parameterTypes)
		{
			return type.GetMethod(null, parameterTypes);
		}

		public static MethodInfo GetMethod(this Type type, string name, IList<Type> parameterTypes)
		{
			return type.GetMethod(name, DefaultFlags, null, parameterTypes, null);
		}

		public static MethodInfo GetMethod(this Type type, string name, BindingFlags bindingFlags, object placeHolder1, IList<Type> parameterTypes, object placeHolder2)
		{
			return type.GetTypeInfo().DeclaredMethods.Where(m =>
			{
				if (name != null && m.Name != name)
					return false;

				if (!TestAccessibility(m, bindingFlags))
					return false;

				return m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes);
			}).SingleOrDefault();
		}

		public static PropertyInfo GetProperty(this Type type, string name, BindingFlags bindingFlags, object placeholder1, Type propertyType, IList<Type> indexParameters, object placeholder2)
		{
			return type.GetTypeInfo().DeclaredProperties.Where(p =>
			{
				if (name != null && name != p.Name)
					return false;
				if (propertyType != null && propertyType != p.PropertyType)
					return false;
				if (indexParameters != null)
				{
					if (!p.GetIndexParameters().Select(ip => ip.ParameterType).SequenceEqual(indexParameters))
						return false;
				}

				return true;
			}).SingleOrDefault();
		}

		public static ConstructorInfo[] GetConstructors(this Type type)
		{
			return type.GetConstructors(DefaultFlags);
		}

		public static ConstructorInfo[] GetConstructors(this Type type, BindingFlags bindingFlags)
		{
			return type.GetConstructors(bindingFlags, null);
		}

		private static ConstructorInfo[] GetConstructors(this Type type, BindingFlags bindingFlags, IList<Type> parameterTypes)
		{
			return new List<ConstructorInfo>(type.GetTypeInfo().DeclaredConstructors.Where(c =>
			{
				if (!TestAccessibility(c, bindingFlags))
					return false;

				if (parameterTypes != null && !c.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes))
					return false;

				return true;
			})).ToArray();
		}

		public static ConstructorInfo GetConstructor(this Type type, IList<Type> parameterTypes)
		{
			return type.GetConstructor(DefaultFlags, null, parameterTypes, null);
		}

		public static ConstructorInfo GetConstructor(this Type type, BindingFlags bindingFlags, object placeholder1, IList<Type> parameterTypes, object placeholder2)
		{
			return type.GetConstructors(bindingFlags, parameterTypes).SingleOrDefault();
		}

		public static MemberInfo[] GetMember(this Type type, string member)
		{
			return type.GetMember(member, DefaultFlags);
		}

		public static MemberInfo[] GetMember(this Type type, string member, BindingFlags bindingFlags)
		{
            try
            {
			    return type.GetTypeInfo().GetMembersRecursive().Where(m => m.Name == member && TestAccessibility(m, bindingFlags)).ToArray();
            }
            catch{}
            return new MemberInfo[0];
		}

        public static MemberInfo[] GetMembers(this Type type, BindingFlags bindingFlags)
		{
            try
            {
			    return type.GetTypeInfo().GetMembersRecursive().Where(m => TestAccessibility(m, bindingFlags)).ToArray();
            }
            catch{}
            return new MemberInfo[0];
		}

        public static MemberInfo[] GetMembers(this Type type, string name, MemberTypes memberType, BindingFlags bindingFlags)
		{
			return new List<MemberInfo>(type.GetTypeInfo().GetMembersRecursive().Where(m =>
			{
				if (name != null && name != m.Name)
					return false;
				if (m.MemberType() != memberType)
					return false;
				if (!TestAccessibility(m, bindingFlags))
					return false;

				return true;
			})).ToArray();
		}

		public static FieldInfo GetField(this Type type, string member)
		{
			return type.GetField(member, DefaultFlags);
		}

		public static FieldInfo GetField(this Type type, string member, BindingFlags bindingFlags)
		{
			return type.GetTypeInfo().GetDeclaredField(member);
		}

		public static PropertyInfo[] GetProperties(this Type type, BindingFlags bindingFlags)
		{
			IList<PropertyInfo> properties = (bindingFlags.HasFlag(BindingFlags.DeclaredOnly))
			  ? type.GetTypeInfo().DeclaredProperties.ToList()
			  : type.GetTypeInfo().GetPropertiesRecursive();

			return new List<PropertyInfo>(properties.Where(p => TestAccessibility(p, bindingFlags))).ToArray();
		}

		private static IList<MemberInfo> GetMembersRecursive(this TypeInfo type)
		{
			TypeInfo t = type;
			IList<MemberInfo> members = new List<MemberInfo>();
			while (t != null)
			{
				foreach (var member in t.DeclaredMembers)
				{
					if (!members.Any(p => p.Name == member.Name))
						members.Add(member);
				}
				t = (t.BaseType != null) ? t.BaseType.GetTypeInfo() : null;
			}

			return members;
		}

		private static IList<PropertyInfo> GetPropertiesRecursive(this TypeInfo type)
		{
			TypeInfo t = type;
			IList<PropertyInfo> properties = new List<PropertyInfo>();
			while (t != null)
			{
				foreach (var member in t.DeclaredProperties)
				{
					if (!properties.Any(p => p.Name == member.Name))
						properties.Add(member);
				}
				t = (t.BaseType != null) ? t.BaseType.GetTypeInfo() : null;
			}

			return properties;
		}

		private static IList<FieldInfo> GetFieldsRecursive(this TypeInfo type)
		{
			TypeInfo t = type;
			IList<FieldInfo> fields = new List<FieldInfo>();
			while (t != null)
			{
				foreach (var member in t.DeclaredFields)
				{
					if (!fields.Any(p => p.Name == member.Name))
						fields.Add(member);
				}
				t = (t.BaseType != null) ? t.BaseType.GetTypeInfo() : null;
			}

			return fields;
		}

		public static MethodInfo[] GetMethods(this Type type, BindingFlags bindingFlags)
		{
			return new List<MethodInfo>(type.GetTypeInfo().DeclaredMethods).ToArray();
		}

		public static PropertyInfo GetProperty(this Type type, string name)
		{
			return type.GetProperty(name, DefaultFlags);
		}

		public static PropertyInfo GetProperty(this Type type, string name, BindingFlags bindingFlags)
		{
			return type.GetTypeInfo().GetDeclaredProperty(name);
		}

		public static FieldInfo[] GetFields(this Type type)
		{
			return type.GetFields(DefaultFlags);
		}

		public static FieldInfo[] GetFields(this Type type, BindingFlags bindingFlags)
		{
			IList<FieldInfo> fields = (bindingFlags.HasFlag(BindingFlags.DeclaredOnly))
			  ? type.GetTypeInfo().DeclaredFields.ToList()
			  : type.GetTypeInfo().GetFieldsRecursive();

			return fields.Where(f => TestAccessibility(f, bindingFlags)).ToArray();
		}

		private static bool TestAccessibility(PropertyInfo member, BindingFlags bindingFlags)
		{
			if (member.GetMethod != null && TestAccessibility(member.GetMethod, bindingFlags))
				return true;

			if (member.SetMethod != null && TestAccessibility(member.SetMethod, bindingFlags))
				return true;

			return false;
		}

		private static bool TestAccessibility(MemberInfo member, BindingFlags bindingFlags)
		{
			if (member is FieldInfo)
			{
				return TestAccessibility((FieldInfo)member, bindingFlags);
			}
			else if (member is MethodBase)
			{
				return TestAccessibility((MethodBase)member, bindingFlags);
			}
			else if (member is PropertyInfo)
			{
				return TestAccessibility((PropertyInfo)member, bindingFlags);
			}

			throw new Exception("Unexpected member type.");
		}

		private static bool TestAccessibility(FieldInfo member, BindingFlags bindingFlags)
		{
			bool visibility = (member.IsPublic && bindingFlags.HasFlag(BindingFlags.Public)) ||
			  (!member.IsPublic && bindingFlags.HasFlag(BindingFlags.NonPublic));

			bool instance = (member.IsStatic && bindingFlags.HasFlag(BindingFlags.Static)) ||
			  (!member.IsStatic && bindingFlags.HasFlag(BindingFlags.Instance));

			return visibility && instance;
		}

		private static bool TestAccessibility(MethodBase member, BindingFlags bindingFlags)
		{
			bool visibility = (member.IsPublic && bindingFlags.HasFlag(BindingFlags.Public)) ||
			  (!member.IsPublic && bindingFlags.HasFlag(BindingFlags.NonPublic));

			bool instance = (member.IsStatic && bindingFlags.HasFlag(BindingFlags.Static)) ||
			  (!member.IsStatic && bindingFlags.HasFlag(BindingFlags.Instance));

			return visibility && instance;
		}

		public static Type[] GetGenericArguments(this Type type)
		{
			return type.GetTypeInfo().GenericTypeArguments;
		}

		public static Type[] GetInterfaces(this Type type)
		{
			return new List<Type>(type.GetTypeInfo().ImplementedInterfaces).ToArray();
		}

		public static MethodInfo[] GetMethods(this Type type)
		{
			return new List<MethodInfo>(type.GetTypeInfo().DeclaredMethods).ToArray();
		}

		public static bool AssignableToTypeName(this Type type, string fullTypeName, out Type match)
		{
			Type current = type;

			while (current != null)
			{
				if (string.Equals(current.FullName, fullTypeName, StringComparison.Ordinal))
				{
					match = current;
					return true;
				}

				current = current.BaseType();
			}

			foreach (Type i in type.GetInterfaces())
			{
				if (string.Equals(i.Name, fullTypeName, StringComparison.Ordinal))
				{
					match = type;
					return true;
				}
			}

			match = null;
			return false;
		}

		public static bool AssignableToTypeName(this Type type, string fullTypeName)
		{
			Type match;
			return type.AssignableToTypeName(fullTypeName, out match);
		}

		public static MethodInfo GetGenericMethod(this Type type, string name, params Type[] parameterTypes)
		{
			var methods = type.GetMethods().Where(method => method.Name == name);

			foreach (var method in methods)
			{
				if (method.HasParameters(parameterTypes))
					return method;
			}

			return null;
		}

		public static bool HasParameters(this MethodInfo method, params Type[] parameterTypes)
		{
			var methodParameters = method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

			if (methodParameters.Length != parameterTypes.Length)
				return false;

			for (int i = 0; i < methodParameters.Length; i++)
				if (methodParameters[i].ToString() != parameterTypes[i].ToString())
					return false;

			return true;
		}

        public static Type GetGenericTypeDefinition(this Type target)
        {
            return target.GetTypeInfo().GetGenericTypeDefinition();
        }

        public static object[] GetCustomAttributes(this Type target, bool inherit)
        {
            var v_customAttrs = target.GetTypeInfo().GetCustomAttributes(inherit);
            List<object> v_list = new List<object>();
            foreach (var v_attr in v_customAttrs)
            {
                v_list.Add(v_attr);
            }
            return v_list.ToArray();
        }

        public static object[] GetCustomAttributes(this Type target, Type attributeType, bool inherit)
        {
            var v_customAttrs = target.GetTypeInfo().GetCustomAttributes(attributeType, inherit);
            List<object> v_list = new List<object>();
            foreach (var v_attr in v_customAttrs)
            {
                v_list.Add(v_attr);
            }
            return v_list.ToArray();
        }

		public static IEnumerable<Type> GetAllInterfaces(this Type target)
		{
			foreach (var i in target.GetInterfaces())
			{
				yield return i;
				foreach (var ci in i.GetInterfaces())
				{
					yield return ci;
				}
			}
		}

		public static IEnumerable<MethodInfo> GetAllMethods(this Type target)
		{
			var allTypes = target.GetAllInterfaces().ToList();
			allTypes.Add(target);

			return from type in allTypes
				   from method in type.GetMethods()
				   select method;
		}
#endif
        #endregion

    }
}