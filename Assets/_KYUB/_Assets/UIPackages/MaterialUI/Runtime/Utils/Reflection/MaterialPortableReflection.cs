#if !UNITY_EDITOR && UNITY_METRO
#define USE_TYPEINFO
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MaterialUI.Reflection.Extensions
{
    /// <summary>
    /// This wraps reflection types so that it is portable across different Unity runtimes.
    /// </summary>
    public static class MaterialPortableReflection
    {
        public static Type[] EmptyTypes = { };

        #region Attribute Queries
#if USE_TYPEINFO
        public static TAttribute GetAttribute<TAttribute>(Type type)
            where TAttribute : Attribute {

            return GetAttribute<TAttribute>(type.GetTypeInfo());
        }

        public static Attribute GetAttribute(Type type, Type attributeType) {
            return GetAttribute(type.GetTypeInfo(), attributeType);
        }

        public static bool HasAttribute(Type type, Type attributeType) {
            return GetAttribute(type, attributeType) != null;
        }
#endif

        /// <summary>
        /// Returns true if the given attribute is defined on the given element.
        /// </summary>
        public static bool HasAttribute(MemberInfo element, Type attributeType)
        {
            return GetAttribute(element, attributeType) != null;
        }

        /// <summary>
        /// Returns true if the given attribute is defined on the given element.
        /// </summary>
        public static bool HasAttribute<TAttribute>(MemberInfo element)
        {
            return HasAttribute(element, typeof(TAttribute));
        }

        /// <summary>
        /// Fetches the given attribute from the given MemberInfo. This method applies caching
        /// and is allocation free (after caching has been performed).
        /// </summary>
        /// <param name="element">The MemberInfo the get the attribute from.</param>
        /// <param name="attributeType">The type of attribute to fetch.</param>
        /// <returns>The attribute or null.</returns>
        public static Attribute GetAttribute(MemberInfo element, Type attributeType)
        {

            Attribute attribute = null;
            if (element != null)
            {
                var query = new AttributeQuery
                {
                    MemberInfo = element,
                    AttributeType = attributeType
                };

                if (_cachedAttributeQueries.TryGetValue(query, out attribute) == false)
                {
                    var attributes = element.GetCustomAttributes(attributeType, /*inherit:*/ true);
                    attribute = (Attribute)attributes.FirstOrDefault();
                    _cachedAttributeQueries[query] = attribute;
                }
            }
            return attribute;
        }

        public static Attribute[] GetAttributes(MemberInfo element, Type attributeType)
        {
            List<Attribute> v_attrs = new List<Attribute>();
            if (element != null)
            {
                var v_attributesObj = element.GetCustomAttributes(attributeType, true);
                foreach (var v_obj in v_attributesObj)
                {
                    v_attrs.Add(v_obj as Attribute);
                }
            }
            return v_attrs.ToArray();
        }

        public static TAttribute[] GetAttributes<TAttribute>(MemberInfo element) where TAttribute : Attribute
        {
            List<TAttribute> v_attrs = new List<TAttribute>();

            if (element != null)
            {
                Attribute[] v_nonCastedAttrs = GetAttributes(element, typeof(TAttribute));
                foreach (var v_obj in v_nonCastedAttrs)
                {
                    v_attrs.Add(v_obj as TAttribute);
                }
            }
            return v_attrs.ToArray();
        }

        /// <summary>
        /// Fetches the given attribute from the given MemberInfo.
        /// </summary>
        /// <typeparam name="TAttribute">The type of attribute to fetch.</typeparam>
        /// <param name="element">The MemberInfo to get the attribute from.</param>
        /// <returns>The attribute or null.</returns>
        public static TAttribute GetAttribute<TAttribute>(MemberInfo element)
            where TAttribute : Attribute
        {

            return (TAttribute)GetAttribute(element, typeof(TAttribute));
        }
        private struct AttributeQuery
        {
            public MemberInfo MemberInfo;
            public Type AttributeType;
        }
        private static IDictionary<AttributeQuery, Attribute> _cachedAttributeQueries =
            new Dictionary<AttributeQuery, Attribute>(new AttributeQueryComparator());
        private class AttributeQueryComparator : IEqualityComparer<AttributeQuery>
        {
            public bool Equals(AttributeQuery x, AttributeQuery y)
            {
                return
                    x.MemberInfo == y.MemberInfo &&
                    x.AttributeType == y.AttributeType;
            }

            public int GetHashCode(AttributeQuery obj)
            {
                return
                    obj.MemberInfo.GetHashCode() +
                    (17 * obj.AttributeType.GetHashCode());
            }
        }
        #endregion

#if !USE_TYPEINFO
        private static BindingFlags DeclaredFlags =
            BindingFlags.NonPublic |
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly;

        private static BindingFlags DeclaredNonStaticFlags =
            BindingFlags.NonPublic |
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly;
#endif

        public static FieldInfo GetDeclaredField(this Type type, string propertyName)
        {
#if USE_TYPEINFO
            var fields = GetDeclaredFields(type);

            for (int i = 0; i < fields.Length; ++i)
            {
                if (fields[i].Name == propertyName)
                {
                    return fields[i];
                }
            }

            return null;
#else
            return type.GetField(propertyName, DeclaredFlags);
#endif  
        }

        public static PropertyInfo GetDeclaredProperty(this Type type, string propertyName)
        {
#if USE_TYPEINFO
            var props = GetDeclaredProperties(type);

            for (int i = 0; i < props.Length; ++i) {
                if (props[i].Name == propertyName) {
                    return props[i];
                }
            }

            return null;
#else
            return type.GetProperty(propertyName, DeclaredFlags);
#endif  
        }

        public static MethodInfo GetDeclaredMethod(this Type type, string methodName)
        {
#if USE_TYPEINFO
            var methods = GetDeclaredMethods(type);

            for (int i = 0; i < methods.Length; ++i) {
                if (methods[i].Name == methodName) {
                    return methods[i];
                }
            }

            return null;
#else
            return type.GetMethod(methodName, DeclaredFlags);
#endif  
        }


        public static ConstructorInfo GetDeclaredConstructor(this Type type, Type[] parameters)
        {
#if USE_TYPEINFO
            var ctors = GetDeclaredConstructors(type);

            for (int i = 0; i < ctors.Length; ++i) {
                var ctor = ctors[i];
                var ctorParams = ctor.GetParameters();

                if (parameters.Length != ctorParams.Length) continue;

                for (int j = 0; j < ctorParams.Length; ++j) {
                    // require an exact match
                    if (ctorParams[j].ParameterType != parameters[j]) continue;
                }

                return ctor;
            }

            return null;
#else
            return type.GetConstructor(DeclaredNonStaticFlags, null, parameters, null);
#endif 
        }

        public static ConstructorInfo[] GetDeclaredConstructors(this Type type)
        {
#if USE_TYPEINFO
            return type.GetTypeInfo().DeclaredConstructors.ToArray();
#else
            return type.GetConstructors(DeclaredFlags);
#endif
        }

        public static MemberInfo[] GetFlattenedMember(this Type type, string memberName)
        {
            var result = new List<MemberInfo>();

            while (type != null)
            {
                var members = GetDeclaredMembers(type);

                for (int i = 0; i < members.Length; ++i)
                {
                    if (members[i].Name == memberName)
                    {
                        result.Add(members[i]);
                    }
                }

                type = type.Resolve().BaseType;
            }

            return result.ToArray();
        }

        public static MethodInfo GetFlattenedMethod(this Type type, string methodName)
        {
            while (type != null)
            {
                var method = GetDeclaredMethod(type, methodName);

                if (method != null)
                    return method;

                type = type.Resolve().BaseType;
            }

            return null;
        }

        //Public and protected (and private fields of TopMost type)
        public static MethodInfo GetFamilyMethod(this Type type, string methodName)
        {
            var v_loopCounter = 0;
            while (type != null)
            {
                var method = GetDeclaredMethod(type, methodName);

                if (method != null && (v_loopCounter == 0 || method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly))
                    return method;

                type = type.Resolve().BaseType;

                v_loopCounter++;
            }

            return null;
        }

        public static IEnumerable<MethodInfo> GetFlattenedMethods(this Type type, string methodName)
        {
            while (type != null)
            {
                var methods = GetDeclaredMethods(type);

                for (int i = 0; i < methods.Length; ++i)
                {
                    if (methods[i].Name == methodName)
                    {
                        yield return methods[i];
                    }
                }

                type = type.Resolve().BaseType;
            }
        }

        public static PropertyInfo GetFlattenedProperty(this Type type, string propertyName)
        {
            while (type != null)
            {
                var property = GetDeclaredProperty(type, propertyName);

                if (property != null)
                    return property;

                type = type.Resolve().BaseType;
            }

            return null;
        }

        //Public and protected (and private fields of TopMost type)
        public static PropertyInfo GetFamilyProperty(this Type type, string fieldName)
        {
            var v_loopCounter = 0;
            while (type != null)
            {
                var property = GetDeclaredProperty(type, fieldName);

                if (property != null)
                {
                    var v_isValid = v_loopCounter == 0;
                    if (!v_isValid)
                    {
                        var v_getMethod = property.GetGetMethod(true);
                        v_isValid = v_getMethod != null && (v_getMethod.IsPublic || v_getMethod.IsFamily || v_getMethod.IsFamilyOrAssembly);
                        if (!v_isValid)
                        {
                            var v_setMethod = property.GetSetMethod(true);
                            v_isValid = v_setMethod != null && (v_setMethod.IsPublic || v_setMethod.IsFamily || v_setMethod.IsFamilyOrAssembly);
                        }
                    }

                    if (v_isValid)
                        return property;
                }

                type = type.Resolve().BaseType;

                v_loopCounter++;
            }

            return null;
        }

        public static FieldInfo GetFlattenedField(this Type type, string fieldName)
        {
            while (type != null)
            {
                var field = GetDeclaredField(type, fieldName);

                if (field != null)
                    return field;

                type = type.Resolve().BaseType;
            }

            return null;
        }

        //Public and protected (and private fields of TopMost type)
        public static FieldInfo GetFamilyField(this Type type, string fieldName)
        {
            var v_loopCounter = 0;
            while (type != null)
            {
                var field = GetDeclaredField(type, fieldName);

                if (field != null && (v_loopCounter == 0 || field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly))
                    return field;

                type = type.Resolve().BaseType;

                v_loopCounter++;
            }

            return null;
        }

        public static MemberInfo GetDeclaredMember(this Type type, string memberName)
        {
            var members = GetDeclaredMembers(type);

            for (int i = 0; i < members.Length; ++i)
            {
                if (members[i].Name == memberName)
                {
                    return members[i];
                }
            }
            return null;
        }

        public static MethodInfo[] GetDeclaredMethods(this Type type)
        {
#if USE_TYPEINFO
            return type.GetTypeInfo().DeclaredMethods.ToArray();
#else
            return type.GetMethods(DeclaredFlags);
#endif
        }

        public static PropertyInfo[] GetDeclaredProperties(this Type type)
        {
#if USE_TYPEINFO
            return type.GetTypeInfo().DeclaredProperties.ToArray();
#else
            return type.GetProperties(DeclaredFlags);
#endif
        }

        public static FieldInfo[] GetDeclaredFields(this Type type)
        {
#if USE_TYPEINFO
            return type.GetTypeInfo().DeclaredFields.ToArray();
#else
            return type.GetFields(DeclaredFlags);
#endif
        }

        public static MemberInfo[] GetDeclaredMembers(this Type type)
        {
#if USE_TYPEINFO
            return type.GetTypeInfo().DeclaredMembers.ToArray();
#else
            return type.GetMembers(DeclaredFlags);
#endif
        }

        public static MemberInfo AsMemberInfo(Type type)
        {
#if USE_TYPEINFO
            return type.GetTypeInfo();
#else
            return type;
#endif
        }

        public static bool IsType(MemberInfo member)
        {
#if USE_TYPEINFO
            return member is TypeInfo;
#else
            return member is Type;
#endif
        }

        public static Type AsType(MemberInfo member)
        {
#if USE_TYPEINFO
            return ((TypeInfo)member).AsType();
#else
            return (Type)member;
#endif
        }

#if USE_TYPEINFO
        public static TypeInfo Resolve(this Type type) 
        {
            return type.GetTypeInfo();
        }
#else
        public static Type Resolve(this Type type)
        {
            return type;
        }
#endif


        #region Extensions

#if USE_TYPEINFO
        public static bool IsAssignableFrom(this Type parent, Type child) {
            return parent.GetTypeInfo().IsAssignableFrom(child.GetTypeInfo());
        }

        public static Type GetElementType(this Type type) {
            return type.GetTypeInfo().GetElementType();
        }

        public static MethodInfo GetSetMethod(this PropertyInfo member, bool nonPublic = false) {
            // only public requested but the set method is not public
            if (nonPublic == false && member.SetMethod != null && member.SetMethod.IsPublic == false) return null;

            return member.SetMethod;
        }

        public static MethodInfo GetGetMethod(this PropertyInfo member, bool nonPublic = false) {
            // only public requested but the set method is not public
            if (nonPublic == false && member.GetMethod != null && member.GetMethod.IsPublic == false) return null;

            return member.GetMethod;
        }

        public static MethodInfo GetBaseDefinition(this MethodInfo method) {
            return method.GetRuntimeBaseDefinition();
        }

        public static Type[] GetInterfaces(this Type type) {
            return type.GetTypeInfo().ImplementedInterfaces.ToArray();
        }

        public static Type[] GetGenericArguments(this Type type) {
            return type.GetTypeInfo().GenericTypeArguments.ToArray();
        }
#endif
        #endregion
    }
}