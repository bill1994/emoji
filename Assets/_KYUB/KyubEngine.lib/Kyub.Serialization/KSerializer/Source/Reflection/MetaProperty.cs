#if UNITY_EDITOR || (ENABLE_MONO && (UNITY_ANDROID || UNITY_STANDALONE || UNITY_WII))
#define HAS_EMIT
#endif

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Kyub.Serialization.Internal {
    /// <summary>
    /// A property or field on a MetaType.
    /// </summary>
    public class MetaProperty {
        public MetaProperty(FieldInfo field, MethodInfo canSerializeInContextMethodInfo) {
            if (field != null)
            {
                _canSerializeInContextMethodInfo = canSerializeInContextMethodInfo;
                _memberInfo = field;
                StorageType = field.FieldType;
                JsonName = GetJsonName(field);
                FallbackNames = GetFallbackNames(field);
                MemberName = field.Name;
                IsPublic = field.IsPublic;
                CanRead = true;
                CanWrite = true;

                //Generate new Converter Instance
                var converterType = GetConverterType(field);
                if (converterType != null && typeof(BaseConverter).IsAssignableFrom(converterType))
                    ConverterInstance = (BaseConverter)Activator.CreateInstance(converterType);
            }
        }

        public MetaProperty(PropertyInfo property, MethodInfo canSerializeInContextMethodInfo) {
            if (property != null)
            {
                _canSerializeInContextMethodInfo = canSerializeInContextMethodInfo;
                _memberInfo = property;
                StorageType = property.PropertyType;
                JsonName = GetJsonName(property);

                FallbackNames = GetFallbackNames(property);
                MemberName = property.Name;
                IsPublic = (property.GetGetMethod() != null && property.GetGetMethod().IsPublic) && (property.GetSetMethod() != null && property.GetSetMethod().IsPublic);
                CanRead = property.CanRead;
                CanWrite = property.CanWrite;

                //Generate new Converter Instance
                var converterType = GetConverterType(property);
                if (converterType != null && typeof(BaseConverter).IsAssignableFrom(converterType))
                    ConverterInstance = (BaseConverter)Activator.CreateInstance(converterType);
            }
        }

        /// <summary>
        /// Internal handle to the reflected member.
        /// </summary>
        private MemberInfo _memberInfo = null;

        /// <summary>
        /// Internal handle to the reflected member.
        /// </summary>
        private MethodInfo _canSerializeInContextMethodInfo = null;

        /// <summary>
        /// Conversor used to Serialize/Deserialize
        /// </summary>
        public BaseConverter ConverterInstance
        {
            get;
            private set;
        }

        /// <summary>
        /// The type of value that is stored inside of the property. For example, for an int field,
        /// StorageType will be typeof(int).
        /// </summary>
        public Type StorageType {
            get;
            private set;
        }

        /// <summary>
        /// Can this property be read?
        /// </summary>
        public bool CanRead {
            get;
            private set;
        }

        /// <summary>
        /// Can this property be written to?
        /// </summary>
        public bool CanWrite {
            get;
            private set;
        }

        /// <summary>
        /// The serialized name of the property, as it should appear in JSON.
        /// </summary>
        public ReadOnlyCollection<string> FallbackNames
        {
            get;
            private set;
        }

        /// <summary>
        /// The serialized name of the property, as it should appear in JSON.
        /// </summary>
        public string JsonName {
            get;
            private set;
        }

        /// <summary>
        /// The name of the actual member.
        /// </summary>
        public string MemberName {
            get;
            private set;
        }

        /// <summary>
        /// Is this member public?
        /// </summary>
        public bool IsPublic {
            get;
            private set;
        }

        public BaseConverter CachedConverter
        {
            get;
            private set;
        }

        public bool CanSerializeInContext(object context)
        {
            try
            {
                if (_canSerializeInContextMethodInfo != null && context != null)
                {
                    var returnValue = _canSerializeInContextMethodInfo.Invoke(context, null);
                    if (returnValue is bool)
                        return (bool)returnValue;
                }
            }
            catch (Exception e){
                Debug.LogWarning("[MetaProperty] Invalid CanSerializeInContext MethodInfo or Target.\n" + e.Message);
            }
            return true;
        }

        public bool HasAttribute(System.Type type)
        {
            return PortableReflection.HasAttribute(_memberInfo, type);
        }

        public bool HasAttribute<TAttribute>() where TAttribute : Attribute
        {
            return HasAttribute(typeof(TAttribute));
        }

        public Attribute GetAttribute(System.Type type)
        {
            return PortableReflection.GetAttribute(_memberInfo, type);
        }

        public TAttribute GetAttribute<TAttribute>() where TAttribute : Attribute
        {
            return PortableReflection.GetAttribute<TAttribute>(_memberInfo);
        }

        public Attribute[] GetAttributes(System.Type type)
        {
            return PortableReflection.GetAttributes(_memberInfo, type);
        }

        public TAttribute[] GetAttributes<TAttribute>() where TAttribute : Attribute
        {
            return PortableReflection.GetAttributes<TAttribute>(_memberInfo);
        }

        /// <summary>
        /// Writes a value to the property that this MetaProperty represents, using given object
        /// instance as the context.
        /// </summary>
        public void Write(object context, object value)
        {
            try
            {
                FieldInfo field = _memberInfo as FieldInfo;
                PropertyInfo property = _memberInfo as PropertyInfo;
                if (field != null)
                {
                    var fieldSet = field.DelegateForSet();
                    fieldSet(ref context, value);
                }

                else if (property != null && property.CanWrite)
                {
                    var propertySet = property.DelegateForSet();
                    propertySet(ref context, value);
                }
            }
            catch (System.Exception exception)
            {
                UnityEngine.Debug.LogWarning(exception.Message);
            }
        }

        /// <summary>
        /// Reads a value from the property that this MetaProperty represents, using the given
        /// object instance as the context.
        /// </summary>
        public object Read(object context) {
            try
            {
                FieldInfo field = _memberInfo as FieldInfo;
                PropertyInfo property = _memberInfo as PropertyInfo;
                if (field != null)
                {
                    var fieldGet = field.DelegateForGet();
                    return fieldGet(context);
                }

                else if(property != null && property.CanRead)
                {
                    var propertyGet = property.DelegateForGet();
                    return propertyGet(context);
                }
            }
            catch (System.Exception exception)
            {
                UnityEngine.Debug.LogWarning(exception.Message);
            }
            return null;
        }

        /// <summary>
        /// Check if Metaproperty contains JsonName or VariantName
        /// </summary>
        public bool CanBeAssignedAsName(string nameToCheck)
        {
            if (!string.IsNullOrEmpty(nameToCheck))
            {
                    if (nameToCheck == JsonName || (FallbackNames != null && FallbackNames.Contains(nameToCheck)))
                        return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the name the given member wants to use for JSON serialization.
        /// </summary>
        private static string GetJsonName(MemberInfo member) {
            var attr = PortableReflection.GetAttribute<SerializePropertyAttribute>(member);
            if (attr != null)
            {
                if (!string.IsNullOrEmpty(attr.Name))
                    return attr.Name;
            }
            else
            {
                var xmlAttr = PortableReflection.GetAttribute<System.Xml.Serialization.XmlAttributeAttribute>(member);
                if (xmlAttr != null && !string.IsNullOrEmpty(xmlAttr.AttributeName))
                    return xmlAttr.AttributeName;
            }

            return member.Name;
        }

        /// <summary>
        /// Returns custom conversor that will be used in Json Serialization/Deserialization
        /// </summary>
        private static Type GetConverterType(MemberInfo member)
        {
            var attr = PortableReflection.GetAttribute<SerializePropertyAttribute>(member);
            if (attr != null)
            {
                return attr.Converter;
            }

            return null;
        }

        /// <summary>
        /// Returns the name the given member wants to use for JSON serialization.
        /// </summary>
        private static ReadOnlyCollection<string> GetFallbackNames(MemberInfo member)
        {
            var attr = PortableReflection.GetAttribute<SerializePropertyAttribute>(member);
            if (attr != null && attr.FallbackNames != null)
            {
                return new ReadOnlyCollection<string>(attr.FallbackNames);
            }

            return new ReadOnlyCollection<string>(new List<string>());
        }
    }
}