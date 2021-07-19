using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Extensions;
using Kyub.Serialization;
using Kyub.Serialization.Internal;

namespace Kyub
{
    #region Helper Classes

    public enum JsonPolymorphicMode { Disabled, Enabled }

    [System.Serializable]
    public class UnityJsonData
    {
        [SerializeField]
        string m_json = "";
        [SerializeField]
        List<Object> m_context = new List<Object>();

        public string Json
        {
            get
            {
                return m_json;
            }
            set
            {
                if (m_json == value)
                    return;
                m_json = value;
            }
        }

        public List<Object> Context
        {
            get
            {
                return m_context;
            }
            set
            {
                if (m_context == value)
                    return;
                m_context = value;
            }
        }
    }

    #endregion

    public class SerializationUtils
    {
        #region Static Properties

        static readonly Serializer _defaultSerializer = new Serializer(new SerializerConfig()
        {
            TypeWriterOption = SerializerConfig.TypeWriterEnum.Never,
            MemberSerialization = MemberSerialization.Default
        });

        public static Serializer DefaultSerializer
        {
            get
            {
                return _defaultSerializer;
            }
        }

        static readonly Serializer _defaultPolymorphicSerializer = new Serializer(new SerializerConfig()
        {
            TypeWriterOption = SerializerConfig.TypeWriterEnum.WhenNeeded,
            MemberSerialization = MemberSerialization.Default
        });

        public static Serializer DefaultPolymorphicSerializer
        {
            get
            {
                return _defaultPolymorphicSerializer;
            }
        }

        static SerializationUtils()
        {
            DefaultSerializer.AddConverter(new UnityObjectIgnoreConverter());
        }

        #endregion

        #region Public Clone Functions

        public static T InstantiateClone<T>(T objectToClone) where T : new()
        {
            try
            {
                return (T)InstantiateClone(objectToClone, typeof(T));
            }
            catch { }
            return default(T);
        }

        public static object InstantiateClone(object objectToClone, System.Type newObjectType)
        {
            object newObject = null;
            if (!EqualityExtension.IsNull(objectToClone))
            {
                try
                {
                    List<Object> context = new List<Object>();
                    //We will Serialize the object with same type of himself to prevent kSerializer to write the type
                    Serializer serializer = new Serializer();
                    serializer.Config.TypeWriterOption = SerializerConfig.TypeWriterEnum.WhenNeeded;
                    serializer.Context.Set(context);
                    serializer.AddConverter(new UnityObjectConverter());
                    string serializedObj = ToJson(objectToClone, false, serializer);
                    //We must deserialize using new type
                    newObject = FromJson(serializedObj, newObjectType, serializer);
                }
                catch { }
            }
            return newObject;
        }

        #endregion

        #region Public Unity Json Data Functions

        public static UnityJsonData ToUnityJsonData(object obj, System.Type type, Serializer serializer = null, UnityObjectConverter customConverter = null)
        {
            if (obj != null)
            {
                serializer = LockSerializer(serializer);
                try
                {
                    List<Object> context = new List<Object>();
                    var oldConverter = serializer.GetConverter(typeof(Object), null);
                    if (oldConverter == null || !oldConverter.GetType().IsSubclassOf(typeof(UnityObjectConverter)))
                    {
                        var converter = customConverter == null ? new UnityObjectConverter() : customConverter;
                        serializer.AddConverter(converter);
                    }
                    else if (oldConverter != null && customConverter != null && customConverter.GetType() != oldConverter.GetType())
                    {
                        serializer.AddConverter(customConverter);
                    }
                    serializer.Context.Set(context);
                    var data = new UnityJsonData();
                    data.Json = ToJson_UnsafeInternal(obj, type, false, serializer);
                    data.Context = context;
                    return data;
                }
                finally
                {
                    UnlockSerializer(serializer);
                }
            }
            return null;
        }

        public static UnityJsonData ToUnityJsonData<T>(T obj)
        {
            if (obj != null)
                return ToUnityJsonData(obj, typeof(T));
            return null;
        }

        public static object FromUnityJsonData(UnityJsonData data, System.Type type, Serializer serializer = null, UnityObjectConverter customConverter = null)
        {
            object target = null;
            if (data != null)
            {
                serializer = LockSerializer(serializer);
                try
                {
                    var oldConverter = serializer.GetConverter(typeof(Object), null);
                    if (oldConverter == null || !oldConverter.GetType().IsSubclassOf(typeof(UnityObjectConverter)))
                    {
                        var converter = customConverter == null ? new UnityObjectConverter() : customConverter;
                        serializer.AddConverter(converter);
                    }
                    else if (oldConverter != null && customConverter != null && customConverter.GetType() != oldConverter.GetType())
                    {
                        serializer.AddConverter(customConverter);
                    }
                    serializer.Context.Set(data.Context);
                    FromJson_UnsafeInternal(ref target, data.Json, type, serializer);
                }
                finally
                {
                    UnlockSerializer(serializer);
                }
            }
            return target;
        }

        public static T FromUnityJsonData<T>(UnityJsonData data, Serializer serializer = null, UnityObjectConverter customConverter = null)
        {
            if (data != null)
                return (T)FromUnityJsonData(data, typeof(T), serializer, customConverter);
            return default(T);
        }

        public static void FromUnityJsonDataOverwrite(UnityJsonData data, object target, Serializer serializer = null, UnityObjectConverter customConverter = null)
        {
            if (data != null)
            {
                serializer = LockSerializer(serializer);
                try
                {
                    var oldConverter = serializer.GetConverter(typeof(Object), null);
                    if (oldConverter == null || !oldConverter.GetType().IsSubclassOf(typeof(UnityObjectConverter)))
                    {
                        var converter = customConverter == null ? new UnityObjectConverter() : customConverter;
                        serializer.AddConverter(converter);
                    }
                    else if (oldConverter != null && customConverter != null && customConverter.GetType() != oldConverter.GetType())
                    {
                        serializer.AddConverter(customConverter);
                    }
                    serializer.Context.Set(data.Context);
                    FromJson_UnsafeInternal(ref target, data.Json, target.GetType(), serializer);
                }
                finally
                {
                    UnlockSerializer(serializer);
                }
            }
        }

        #endregion

        #region Public Json Functions

        public static string ToJson<T>(T obj, bool prettyPrint = false, Serializer serializer = null)
        {
            if (obj == null)
                return "";
            else
            {
                return ToJson_Internal(obj, typeof(T), prettyPrint, serializer);
            }
        }

        public static string ToJson<T>(T obj, bool prettyPrint, SerializerConfig config)
        {
            Serializer serializer = new Serializer(config);
            return ToJson<T>(obj, prettyPrint, serializer);
        }

        public static string ToJson<T>(T obj, bool prettyPrint, JsonPolymorphicMode mode)
        {
            Serializer serializer = mode == JsonPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            return ToJson<T>(obj, prettyPrint, serializer);
        }

        public static string ToJson(object obj, System.Type type, bool prettyPrint, Serializer serializer = null)
        {
            if (obj == null)
                return "";
            else
            {
                return ToJson_Internal(obj, type, prettyPrint, serializer);
            }
        }

        public static string ToJson(object obj, System.Type type, bool prettyPrint, SerializerConfig config)
        {
            Serializer serializer = new Serializer(config);
            return ToJson(obj, type, prettyPrint, serializer);
        }

        public static string ToJson(object obj, System.Type type, bool prettyPrint, JsonPolymorphicMode mode)
        {
            Serializer serializer = mode == JsonPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            return ToJson(obj, type, prettyPrint, serializer);
        }

        public static object FromJson(string json, System.Type type, Serializer serializer = null)
        {
            object target = null;
            FromJson_Internal(ref target, json, type, serializer);
            return target;
        }

        public static object FromJson(string json, System.Type type, SerializerConfig config)
        {
            Serializer serializer = new Serializer(config);
            return FromJson(json, type, serializer);
        }

        public static object FromJson(string json, System.Type type, JsonPolymorphicMode mode)
        {
            Serializer serializer = mode == JsonPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            return FromJson(json, type, serializer);
        }

        public static T FromJson<T>(string json, Serializer serializer = null)
        {
            object target = default(T);
            FromJson_Internal(ref target, json, typeof(T), serializer);
            return (T)target;
        }

        public static T FromJson<T>(string json, SerializerConfig config)
        {
            Serializer serializer = new Serializer(config);
            return FromJson<T>(json, serializer);
        }

        public static T FromJson<T>(string json, JsonPolymorphicMode mode)
        {
            Serializer serializer = mode == JsonPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            return FromJson<T>(json, serializer);
        }

        public static void FromJsonOverwrite(string json, object target, Serializer serializer = null)
        {
            if(target != null)
                FromJson_Internal(ref target, json, target.GetType(), serializer);
        }

        public static void FromJsonOverwrite(string json, object target, SerializerConfig config)
        {
            Serializer serializer = new Serializer(config);
            FromJsonOverwrite(json, target, serializer);
        }

        public static void FromJsonOverwrite(string json, object target, JsonPolymorphicMode mode)
        {
            Serializer serializer = mode == JsonPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            FromJsonOverwrite(json, target, serializer);
        }

        public static void FromJsonOverwrite<T>(string json, ref T target, Serializer serializer = null)
        {
            if (!typeof(T).IsValueType && target != null)
                FromJsonOverwrite(json, target, serializer);
            else
                target = FromJson<T>(json, serializer);
        }

        public static void FromJsonOverwrite<T>(string json, ref T target, SerializerConfig config)
        {
            if (!typeof(T).IsValueType && target != null)
                FromJsonOverwrite(json, target, config);
            else
                target = FromJson<T>(json, config);
        }

        public static void FromJsonOverwrite<T>(string json, ref T target, JsonPolymorphicMode mode)
        {
            if (!typeof(T).IsValueType && target != null)
                FromJsonOverwrite(json, target, mode);
            else
                target = FromJson<T>(json, mode);
        }

        #endregion

        #region Public JsonObject Functions

        public static JsonObject ToJsonObject<T>(T obj, Serializer serializer = null)
        {
            if (obj == null)
                return null;
            else
            {
                return ToJsonObject_Internal(obj, typeof(T), serializer);
            }
        }

        public static JsonObject ToJsonObject<T>(T obj, SerializerConfig config)
        {
            Serializer serializer = new Serializer(config);
            return ToJsonObject<T>(obj, serializer);
        }

        public static JsonObject ToJsonObject<T>(T obj, JsonPolymorphicMode mode)
        {
            Serializer serializer = mode == JsonPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            return ToJsonObject<T>(obj, serializer);
        }

        public static JsonObject ToJsonObject(object obj, System.Type type, Serializer serializer = null)
        {
            if (obj == null)
                return null;
            else
            {
                return ToJsonObject_Internal(obj, type, serializer);
            }
        }

        public static JsonObject ToJsonObject(object obj, System.Type type, SerializerConfig config)
        {
            Serializer serializer = new Serializer(config);
            return ToJsonObject(obj, type, serializer);
        }

        public static JsonObject ToJsonObject(object obj, System.Type type, JsonPolymorphicMode mode)
        {
            Serializer serializer = mode == JsonPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            return ToJsonObject(obj, type, serializer);
        }

        public static object FromJsonObject(JsonObject jsonObject, System.Type type, Serializer serializer = null)
        {
            object target = null;
            FromJsonObject_Internal(ref target, jsonObject, type, serializer);
            return target;
        }

        public static object FromJsonObject(JsonObject jsonObject, System.Type type, SerializerConfig config)
        {
            Serializer serializer = new Serializer(config);
            return FromJsonObject(jsonObject, type, serializer);
        }

        public static object FromJsonObject(JsonObject jsonObject, System.Type type, JsonPolymorphicMode mode)
        {
            Serializer serializer = mode == JsonPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            return FromJsonObject(jsonObject, type, serializer);
        }

        public static T FromJsonObject<T>(JsonObject jsonObject, Serializer serializer = null)
        {
            object target = default(T);
            FromJsonObject_Internal(ref target, jsonObject, typeof(T), serializer);
            return (T)target;
        }

        public static T FromJsonObject<T>(JsonObject jsonObject, SerializerConfig config)
        {
            Serializer serializer = new Serializer(config);
            return FromJsonObject<T>(jsonObject, serializer);
        }

        public static T FromJsonObject<T>(JsonObject jsonObject, JsonPolymorphicMode mode)
        {
            Serializer serializer = mode == JsonPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            return FromJsonObject<T>(jsonObject, serializer);
        }

        public static void FromJsonObjectOverwrite(JsonObject jsonObject, object target, Serializer serializer = null)
        {
            if (target != null)
                FromJsonObject_Internal(ref target, jsonObject, target.GetType(), serializer);
        }

        public static void FromJsonObjectOverwrite(JsonObject jsonObject, object target, SerializerConfig config)
        {
            Serializer serializer = new Serializer(config);
            FromJsonObjectOverwrite(jsonObject, target, serializer);
        }

        public static void FromJsonObjectOverwrite(JsonObject jsonObject, object target, JsonPolymorphicMode mode)
        {
            Serializer serializer = mode == JsonPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            FromJsonObjectOverwrite(jsonObject, target, serializer);
        }

        public static void FromJsonObjectOverwrite<T>(JsonObject jsonObject, ref T target, Serializer serializer = null)
        {
            if (!typeof(T).IsValueType && target != null)
                FromJsonObjectOverwrite(jsonObject, target, serializer);
            else
                target = FromJsonObject<T>(jsonObject, serializer);
        }

        public static void FromJsonObjectOverwrite<T>(JsonObject jsonObject, ref T target, SerializerConfig config)
        {
            if (!typeof(T).IsValueType && target != null)
                FromJsonObjectOverwrite(jsonObject, target, config);
            else
                target = FromJsonObject<T>(jsonObject, config);
        }

        public static void FromJsonObjectOverwrite<T>(JsonObject jsonObject, ref T target, JsonPolymorphicMode mode)
        {
            if (!typeof(T).IsValueType && target != null)
                FromJsonObjectOverwrite(jsonObject, target, mode);
            else
                target = FromJsonObject<T>(jsonObject, mode);
        }

        #endregion

        #region Internal Functions

        private static void FromJson_UnsafeInternal(ref object target, string json, System.Type type, Serializer serializer)
        {
            if (!string.IsNullOrEmpty(json) && type != null)
            {
                if (serializer == null)
                    serializer = DefaultSerializer;

                JsonObject data = null;
                JsonParser.Parse(json, serializer.Config, out data);
                if (type.IsSameOrSubClassOrImplementInterface(typeof(JsonObject)))
                    target = data;
                else
                    serializer.TryDeserialize(data, type, ref target, null);
            }
        }

        private static void FromJson_Internal(ref object target, string json, System.Type type, Serializer serializer = null)
        {
            if (!string.IsNullOrEmpty(json) && type != null)
            {
                serializer = LockSerializer(serializer);
                try
                {
                    // The critical section.
                    FromJson_UnsafeInternal(ref target, json, type, serializer);
                }
                finally
                {
                    // Ensure that the lock is released.
                    UnlockSerializer(serializer);
                }
            }
        }

        private static void FromJsonObject_UnsafeInternal(ref object target, JsonObject data, System.Type type, Serializer serializer)
        {
            if (data != null && type != null)
            {
                if (serializer == null)
                    serializer = DefaultSerializer;

                if (type.IsSameOrSubClassOrImplementInterface(typeof(JsonObject)))
                    target = data;
                else
                    serializer.TryDeserialize(data, type, ref target, null);
            }
        }

        private static void FromJsonObject_Internal(ref object target, JsonObject data, System.Type type, Serializer serializer = null)
        {
            if (data != null && type != null)
            {
                serializer = LockSerializer(serializer);
                try
                {
                    // The critical section.
                    FromJsonObject_UnsafeInternal(ref target, data, type, serializer);
                }
                finally
                {
                    // Ensure that the lock is released.
                    UnlockSerializer(serializer);
                }
            }
        }

        private static string ToJson_UnsafeInternal(object target, System.Type type, bool prettyPrint, Serializer serializer)
        {
            if (type != null)
            {
                if (serializer == null)
                    serializer = DefaultSerializer;

                JsonObject data;
                serializer.TrySerialize(type, target, out data, null);
                if (prettyPrint)
                    return JsonPrinter.PrettyJson(data);
                else
                    return JsonPrinter.CompressedJson(data);
            }
            return "";
        }

        private static string ToJson_Internal(object target, System.Type type, bool prettyPrint, Serializer serializer)
        {
            if (type != null)
            {
                //Try Block Serializer to make Thread-Safe Serialization/Deserialization
                serializer = LockSerializer(serializer);
                try
                {
                    // The critical section.
                    return ToJson_UnsafeInternal(target, type, prettyPrint, serializer);
                }
                finally
                {
                    // Ensure that the lock is released.
                    UnlockSerializer(serializer);
                }
            }
            return null;
        }

        private static JsonObject ToJsonObject_UnsafeInternal(object target, System.Type type, Serializer serializer)
        {
            if (type != null)
            {
                if (serializer == null)
                    serializer = DefaultSerializer;

                JsonObject data;
                serializer.TrySerialize(type, target, out data, null);

                return data;
            }
            return null;
        }

        private static JsonObject ToJsonObject_Internal(object target, System.Type type, Serializer serializer = null)
        {
            if (type != null)
            {
                //Try Block Serializer to make Thread-Safe Serialization/Deserialization
                serializer = LockSerializer(serializer);
                try
                {
                    // The critical section.
                    return ToJsonObject_UnsafeInternal(target, type, serializer);
                }
                finally
                {
                    // Ensure that the lock is released.
                    UnlockSerializer(serializer);
                }
            }
            return null;
        }

        protected static Serializer LockSerializer(Serializer serializer)
        {
            if (serializer == null)
                serializer = DefaultSerializer;

#if !UNITY_WEBGL || UNITY_EDITOR
            if (System.Threading.Monitor.TryEnter(serializer, 0))
            {
                return serializer;
            }
            else
            {
                var threadSerializer = new Serializer(serializer);
                System.Threading.Monitor.Enter(threadSerializer);

                return threadSerializer;
            }
#else
            return serializer;
#endif
        }

        protected static void UnlockSerializer(Serializer serializer)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (serializer != null)
                System.Threading.Monitor.Exit(serializer);
#endif
        }

        #endregion
    }
}
