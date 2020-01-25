using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kyub.Serialization.Internal;
using System.Collections.Concurrent;

namespace Kyub.Serialization {
    /// <summary>
    /// MetaType contains metadata about a type. This is used by the reflection serializer.
    /// </summary>

    public class MetaTypeCache
    {
        #region Instance

        readonly Config _configUsed = null;
        public Config ConfigUsed
        {
            get
            {
                return _configUsed;
            }
        }

        public MetaTypeCache(Config p_config)
        {
            if (p_config == null)
                p_config = Config.DefaultConfig;
            _configUsed = p_config;
        }

        private ConcurrentDictionary<Type, MetaType> _metaTypes = new ConcurrentDictionary<Type, MetaType>();
        public MetaType Get(Type type)
        {
            MetaType metaType = null;
            if (!_metaTypes.TryGetValue(type, out metaType))
            {
                metaType = MetaTypeCache.GetInGlobalCache(type, _configUsed);
                _metaTypes[type] = metaType;
            }
            return metaType;
        }

        /// <summary>
        /// Clears out the cached type results. Useful if some prior assumptions become invalid, ie, the default member
        /// serialization mode.
        /// </summary>
        public void ClearCache()
        {
            _metaTypes = new ConcurrentDictionary<Type, MetaType>();
        }

        #endregion

        #region Static

        static ConcurrentDictionary<Type, ConcurrentDictionary<Config, MetaType>> s_globalMetaTypes = new ConcurrentDictionary<Type, ConcurrentDictionary<Config, MetaType>>();
        public static MetaType GetInGlobalCache(Type type, Config p_config)
        {
            if (p_config == null)
                p_config = Config.DefaultConfig;

            //Try pick registered MetaTypes from Type
            ConcurrentDictionary<Config, MetaType> metaTypesDict = null;
            s_globalMetaTypes.TryGetValue(type, out metaTypesDict);

            if (metaTypesDict == null)
            {
                metaTypesDict = new ConcurrentDictionary<Config, MetaType>();
                s_globalMetaTypes[type] = metaTypesDict;
            }

            //Try pick registered MetaType from Config
            MetaType metaType = null;
            metaTypesDict.TryGetValue(p_config, out metaType);

            //Not in cache, create new MetaType and register
            if (metaType == null)
            {
                metaType = new MetaType(type, p_config);
                metaTypesDict[p_config] = metaType;
            }
            return metaType;
        }

        public static void ClearGlobalCache()
        {
            if (s_globalMetaTypes == null)
                s_globalMetaTypes = new ConcurrentDictionary<Type, ConcurrentDictionary<Config, MetaType>>();
            else
                s_globalMetaTypes.Clear();
        }

        #endregion
    }

    public class MetaType
    {
        readonly Config _configUsed = null;
        public Config ConfigUsed
        {
            get
            {
                return _configUsed;
            }
        }

        internal MetaType(Type reflectedType, Config p_config) {
            ReflectedType = reflectedType;
            if (p_config == null)
                p_config = Config.DefaultConfig;
            _configUsed = p_config.Clone();
            List<MetaProperty> properties = new List<MetaProperty>();
            CollectProperties(properties, reflectedType);
            Properties = properties.ToArray();
        }

        public override bool Equals(object obj)
        {
            bool v_sucess =  base.Equals(obj);
            MetaType v_objMetaType = obj as MetaType;
            if (v_objMetaType != null)
            {
                v_sucess = !v_sucess && ReflectedType == v_objMetaType.ReflectedType && v_objMetaType._configUsed.Equals(this._configUsed);
            }
            else
                v_sucess = false;
            return v_sucess;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public Type ReflectedType;

        private void CollectProperties(List<MetaProperty> properties, Type reflectedType)
        {
            // do we require a [SerializeField] or [Property] attribute?
            bool requireOptIn = _configUsed.MemberSerialization == MemberSerialization.OptIn;
            bool requireOptOut = _configUsed.MemberSerialization == MemberSerialization.OptOut;

            ObjectAttribute attr = PortableReflection.GetAttribute<ObjectAttribute>(reflectedType);
            if (attr != null) {
                requireOptIn = attr.MemberSerialization == MemberSerialization.OptIn;
                requireOptOut = attr.MemberSerialization == MemberSerialization.OptOut;
            }

            MemberInfo[] members = reflectedType.GetDeclaredMembers();
            foreach (var member in members) {

                // We don't serialize members annotated with any of the ignore serialize attributes
                if (_configUsed.IgnoreSerializeAttributes.Any(t => PortableReflection.HasAttribute(member, t)))
                {
                    continue;
                }

                /*else
                {
                    //We must check if CanIgnoreMethodInfo is Defined in Ignore Attribute
                    var memberIgnoreAttr = PortableReflection.GetAttribute<IgnoreAttribute>(member);
                    if (memberIgnoreAttr != null)
                    {
                        canIgnoreMethodInfo = memberIgnoreAttr.GetCanIgnoreMethodInfo(reflectedType);
                        if (canIgnoreMethodInfo == null)
                            return true;
                    }
                }*/

                PropertyInfo property = member as PropertyInfo;
                FieldInfo field = member as FieldInfo;

                // If an opt-in annotation is required, then skip the property if it doesn't have one
                // of the serialize attributes
                if (requireOptIn &&
                    !_configUsed.SerializeAttributes.Any(t => PortableReflection.HasAttribute(member, t))) {

                    continue;
                }

                // If an opt-out annotation is required, then skip the property *only if* it has one of
                // the not serialize attributes
                /*if (requireOptOut &&
                    _configUsed.IgnoreSerializeAttributes.Any(t => PortableReflection.HasAttribute(member, t))) {

                    continue;
                }*/

                if (property != null) {
                    if (CanSerializeProperty(property, members, requireOptOut))
                    {
                        var serializePropertyAttr = PortableReflection.GetAttribute<SerializePropertyAttribute>(property);
                        properties.Add(new MetaProperty(property, 
                            serializePropertyAttr != null ? 
                            serializePropertyAttr.GetCanSerializeInContextMethodInfo(reflectedType) : 
                            null));
                    }
                }
                else if (field != null) {
                    //We can serialize public field if is default
                    if (CanSerializeField(field, requireOptOut || (!requireOptIn && !requireOptOut)))
                    {
                        var serializeFieldAttr = PortableReflection.GetAttribute<SerializePropertyAttribute>(field);
                        properties.Add(new MetaProperty(field, 
                            serializeFieldAttr != null ?
                            serializeFieldAttr.GetCanSerializeInContextMethodInfo(reflectedType) :
                            null));
                    }
                }
            }

            if (reflectedType.Resolve().BaseType != null) {
                CollectProperties(properties, reflectedType.Resolve().BaseType);
            }
        }

        private bool IsAutoProperty(PropertyInfo property, MemberInfo[] members) {
            if (!property.CanWrite || !property.CanRead) {
                return false;
            }

            string backingFieldName = "<" + property.Name + ">k__BackingField";
            for (int i = 0; i < members.Length; ++i) {
                if (members[i].Name == backingFieldName) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns if the given property should be serialized.
        /// </summary>
        /// <param name="annotationFreeValue">Should a property without any annotations be serialized?</param>
        private bool CanSerializeProperty(PropertyInfo property, MemberInfo[] members, bool annotationFreeValue) {
            // We don't serialize delegates
            if (typeof(Delegate).IsAssignableFrom(property.PropertyType)) {
                return false;
            }

            // We don't serialize indexers (like : this[int i])
            ParameterInfo[] v_indexParameters = property.GetIndexParameters();
            if (v_indexParameters != null && v_indexParameters.Length > 0)
                return false;

            var publicGetMethod = property.GetGetMethod(/*nonPublic:*/ false);
            var publicSetMethod = property.GetSetMethod(/*nonPublic:*/ false);

            // We do not bother to serialize static fields.
            if ((publicGetMethod != null && publicGetMethod.IsStatic) ||
                (publicSetMethod != null && publicSetMethod.IsStatic)) {
                return false;
            }

            // If a property is annotated with one of the serializable attributes, then it should
            // it should definitely be serialized.
            //
            // NOTE: We place this override check *after* the static check, because we *never*
            //       allow statics to be serialized.
            if (_configUsed.SerializeAttributes.Any(t => PortableReflection.HasAttribute(property, t))) {
                return true;
            }

            // If the property cannot be both read and written to, we are not going to serialize it
            // regardless of the default serialization mode
            if (property.CanRead == false || property.CanWrite == false) {
                return false;
            }

            // If it's an auto-property and it has either a public get or a public set method,
            // then we serialize it
            if (IsAutoProperty(property, members) &&
                (publicGetMethod != null || publicSetMethod != null)) {
                return true;
            }


            // Otherwise, we don't bother with serialization
            //Only Public Ones!
            return annotationFreeValue && publicGetMethod != null && publicSetMethod != null;
        }

        private bool CanSerializeField(FieldInfo field, bool annotationFreeValue) {
            // We don't serialize delegates
            if (typeof(Delegate).IsAssignableFrom(field.FieldType)) {
                return false;
            }

            // We don't serialize compiler generated fields.
            if (field.IsDefined(typeof(CompilerGeneratedAttribute), false)) {
                return false;
            }

            // We don't serialize static fields
            if (field.IsStatic) {
                return false;
            }

            // We want to serialize any fields annotated with one of the serialize attributes.
            //
            // NOTE: This occurs *after* the static check, because we *never* want to serialize
            //       static fields.
            if (_configUsed.SerializeAttributes.Any(t => PortableReflection.HasAttribute(field, t))) {
                return true;
            }

            // We use !IsPublic because that also checks for internal, protected, and private.
            //if (!annotationFreeValue && !field.IsPublic) {
            //    return false;
            //}

            //OptOut is only available to Public Members, like in Newtonsoft
            if (annotationFreeValue && field.IsPublic)
                return true;

            return false;
        }

        /// <summary>
        /// Attempt to emit an AOT compiled direct converter for this type.
        /// </summary>
        /// <returns>True if AOT data was emitted, false otherwise.</returns>
        public bool EmitAotData() {
            if (_hasEmittedAotData == false) {
                _hasEmittedAotData = true;

                // NOTE:
                // Even if the type has derived types, we can still generate a direct converter for it.
                // Direct converters are not used for inherited types, so the reflected converter or something
                // similar will be used for the derived type instead of our AOT compiled one.

                for (int i = 0; i < Properties.Length; ++i) {
                    if (Properties[i].IsPublic == false) return false; // cannot do a speedup
                }

                // we need a default ctor
                if (HasDefaultConstructor == false) return false;

                AotCompilationManager.AddAotCompilation(ReflectedType, Properties, _isDefaultConstructorPublic);
                return true;
            }

            return false;
        }
        private bool _hasEmittedAotData;

        public MetaProperty[] Properties {
            get;
            private set;
        }

        /// <summary>
        /// Returns true if the type represented by this metadata contains a default constructor.
        /// </summary>
        public bool HasDefaultConstructor {
            get {
                if (_hasDefaultConstructorCache.HasValue == false) {
                    // arrays are considered to have a default constructor
                    if (ReflectedType.Resolve().IsArray) {
                        _hasDefaultConstructorCache = true;
                        _isDefaultConstructorPublic = true;
                    }

                    // value types (ie, structs) always have a default constructor
                    else if (ReflectedType.Resolve().IsValueType) {
                        _hasDefaultConstructorCache = true;
                        _isDefaultConstructorPublic = true;
                    }

                    else {
                        // consider private constructors as well
                        var ctor = ReflectedType.GetDeclaredConstructor(PortableReflection.EmptyTypes);
                        _hasDefaultConstructorCache = ctor != null;
                        _isDefaultConstructorPublic = ctor != null && ctor.IsPublic;
                    }
                }

                return _hasDefaultConstructorCache.Value;
            }
        }
        private bool? _hasDefaultConstructorCache;
        private bool _isDefaultConstructorPublic;

        /// <summary>
        /// Creates a new instance of the type that this metadata points back to. If this type has a
        /// default constructor, then Activator.CreateInstance will be used to construct the type
        /// (or Array.CreateInstance if it an array). Otherwise, an uninitialized object created via
        /// FormatterServices.GetSafeUninitializedObject is used to construct the instance.
        /// </summary>
        public object CreateInstance() {
            if (ReflectedType.Resolve().IsInterface || ReflectedType.Resolve().IsAbstract) {
                throw new Exception("Cannot create an instance of an interface or abstract type for " + ReflectedType);
            }

#if !NO_UNITY
            // Unity requires special construction logic for types that derive from
            // ScriptableObject.
            if (typeof(UnityEngine.ScriptableObject).IsAssignableFrom(ReflectedType)) {
                return UnityEngine.ScriptableObject.CreateInstance(ReflectedType);
            }
#endif

            // Strings don't have default constructors but also fail when run through
            // FormatterSerivces.GetSafeUninitializedObject
            if (typeof(string) == ReflectedType) {
                return string.Empty;
            }

            if (HasDefaultConstructor == false) {
#if !UNITY_EDITOR && (UNITY_WEBPLAYER || UNITY_WP8 || UNITY_METRO)
                throw new InvalidOperationException("The selected Unity platform requires " +
                    ReflectedType.FullName + " to have a default constructor. Please add one.");
#else
                return System.Runtime.Serialization.FormatterServices.GetSafeUninitializedObject(ReflectedType);
#endif
            }

            if (ReflectedType.Resolve().IsArray) {
                // we have to start with a size zero array otherwise it will have invalid data
                // inside of it
                return Array.CreateInstance(ReflectedType.GetElementType(), 0);
            }

            try {
                return Activator.CreateInstance(ReflectedType, /*nonPublic:*/ true);
            }
#if (!UNITY_EDITOR && (UNITY_METRO)) == false
            catch (MissingMethodException e) {
                throw new InvalidOperationException("Unable to create instance of " + ReflectedType + "; there is no default constructor", e);
            }
#endif
            catch (TargetInvocationException e) {
                throw new InvalidOperationException("Constructor of " + ReflectedType + " threw an exception when creating an instance", e);
            }
            catch (MemberAccessException e) {
                throw new InvalidOperationException("Unable to access constructor of " + ReflectedType, e);
            }
        }
    }
}