using System;
using System.Collections.Generic;

namespace Kyub.Serialization {
    /// <summary>
    /// Enables some top-level customization of Serializer.
    /// </summary>
    public class SerializerConfig
    {
        #region Helper Enums

        public enum TypeWriterEnum { Never, WhenNeeded, Always }

        #endregion

        #region Constructors

        public SerializerConfig()
        {
            _metaTypeCache = new MetaTypeCache(this);
        }

        private SerializerConfig(MetaTypeCache metaTypeCache)
        {
            if (metaTypeCache == null)
                metaTypeCache = new MetaTypeCache(this);
            _metaTypeCache = metaTypeCache;
        }

        #endregion

        #region Properties

        //Used when dont want to pass a custom config as parameter to members
        static SerializerConfig _defaultConfig = new SerializerConfig();
        public static SerializerConfig DefaultConfig
        {
            get
            {
                if (_defaultConfig == null)
                    _defaultConfig = new SerializerConfig();
                return _defaultConfig;
            }
            set
            {
                if (_defaultConfig == value)
                    return;
                _defaultConfig = value;
            }
        }

        /// <summary>
        /// The attributes that will force a field or property to be serialized.
        /// </summary>
        private Type[] _serializeAttributes = {
#if !NO_UNITY
            typeof(UnityEngine.SerializeField),
#endif
            typeof(SerializePropertyAttribute),
            typeof(System.Xml.Serialization.XmlIncludeAttribute)
        };

        public Type[] SerializeAttributes
        {
            get
            {
                if (_serializeAttributes == null)
                    _serializeAttributes = new Type[0];
                return _serializeAttributes;
            }
            set
            {
                if (_serializeAttributes == value)
                    return;
                _serializeAttributes = value;
            }
        }

        //This parameter will be ignored when comparing with other config, but can be acessed by other guys
        readonly MetaTypeCache _metaTypeCache = null;
        public MetaTypeCache MetaTypeCache
        {
            get
            {
                return _metaTypeCache;
            }
        }

        /// <summary>
        /// The attributes that will force a field or property to *not* be serialized.
        /// </summary>
        private Type[] _ignoreSerializeAttributes = { typeof(NonSerializedAttribute), typeof(IgnoreAttribute), typeof(System.Xml.Serialization.XmlIgnoreAttribute) };
        public Type[] IgnoreSerializeAttributes
        {
            get
            {
                if (_ignoreSerializeAttributes == null)
                    _ignoreSerializeAttributes = new Type[0];
                return _ignoreSerializeAttributes;
            }
            set
            {
                if (_ignoreSerializeAttributes == value)
                    return;
                _ignoreSerializeAttributes = value;
            }
        }

        private MemberSerialization _memberSerialization = MemberSerialization.Default;
        /// <summary>
        /// The default member serialization.
        /// </summary>
        public MemberSerialization MemberSerialization
        {
            get
            {
                return _memberSerialization;
            }
            set
            {
                _memberSerialization = value;
                MetaTypeCache.ClearCache();
            }
        }

        private TypeWriterEnum _typeWriterOption = TypeWriterEnum.WhenNeeded;
        /// <summary>
        /// The default member serialization.
        /// </summary>
        public TypeWriterEnum TypeWriterOption
        {
            get
            {
                return _typeWriterOption;
            }
            set
            {
                _typeWriterOption = value;
            }
        }

        /// <summary>
        /// Should deserialization be case sensitive? If this is false and the JSON has multiple members with the
        /// same keys only separated by case, then this results in undefined behavior.
        /// </summary>
        public bool _isCaseSensitive = true;

        public bool IsCaseSensitive
        {
            get
            {
                return _isCaseSensitive;
            }
            set
            {
                _isCaseSensitive = value;
            }
        }

        #endregion

        #region Helper Functions

        public SerializerConfig Clone()
        {
            SerializerConfig clonedConfig = new SerializerConfig(this.MetaTypeCache);
            clonedConfig.IsCaseSensitive = this.IsCaseSensitive;
            clonedConfig.SerializeAttributes = new List<Type>(this.SerializeAttributes).ToArray();
            clonedConfig.IgnoreSerializeAttributes = new List<Type>(this.IgnoreSerializeAttributes).ToArray();
            clonedConfig.MemberSerialization = this.MemberSerialization;
            clonedConfig.TypeWriterOption = this.TypeWriterOption;
            return clonedConfig;
        }

        public override bool Equals(object config)
        {
            try
            {
                if (config == null)
                    config = SerializerConfig.DefaultConfig;
                if (!(config is SerializerConfig))
                    return false;
                SerializerConfig castedConfig = config as SerializerConfig;
                if (config == this)
                    return true;
                else
                {
                    if (this.IsCaseSensitive == castedConfig.IsCaseSensitive &&
                        this.MemberSerialization == castedConfig.MemberSerialization &&
                        this.SerializeAttributes.Length == castedConfig.SerializeAttributes.Length &&
                        this.IgnoreSerializeAttributes.Length == castedConfig.IgnoreSerializeAttributes.Length)
                    {
                        List<Type> listOfConfig = new List<Type>(castedConfig.SerializeAttributes);
                        foreach (Type type in this.SerializeAttributes)
                        {
                            if (!listOfConfig.Contains(type))
                                return false;
                        }
                        listOfConfig = new List<Type>(castedConfig.IgnoreSerializeAttributes);
                        foreach (Type type in this.IgnoreSerializeAttributes)
                        {
                            if (!listOfConfig.Contains(type))
                                return false;
                        }
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }
}