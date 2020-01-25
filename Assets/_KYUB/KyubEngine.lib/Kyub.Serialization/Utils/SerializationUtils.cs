using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Extensions;
using Kyub.Serialization;
using Kyub.Serialization.Internal;

namespace Kyub
{
    #region Helper Classes

    public enum JSONPolymorphicMode { Disabled, Enabled }

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

        static readonly Serializer _defaultSerializer = new Serializer(new Config()
        {
            TypeWriterOption = Config.TypeWriterEnum.Never,
            MemberSerialization = MemberSerialization.Default
        });

        public static Serializer DefaultSerializer
        {
            get
            {
                return _defaultSerializer;
            }
        }

        static readonly Serializer _defaultPolymorphicSerializer = new Serializer(new Config()
        {
            TypeWriterOption = Config.TypeWriterEnum.WhenNeeded,
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

        public static T InstantiateClone<T>(T p_objectToClone) where T : new()
        {
            try
            {
                return (T)InstantiateClone(p_objectToClone, typeof(T));
            }
            catch { }
            return default(T);
        }

        public static object InstantiateClone(object p_objectToClone, System.Type p_newObjectType)
        {
            object v_newObject = null;
            if (!EqualityExtension.IsNull(p_objectToClone))
            {
                try
                {
                    List<Object> v_context = new List<Object>();
                    //We will Serialize the object with same type of himself to prevent kSerializer to write the type
                    Serializer v_serializer = new Serializer();
                    v_serializer.Config.TypeWriterOption = Config.TypeWriterEnum.WhenNeeded;
                    v_serializer.Context.Set(v_context);
                    v_serializer.AddConverter(new UnityObjectConverter());
                    string v_serializedObj = ToJson(p_objectToClone, false, v_serializer);
                    //We must deserialize using new type
                    v_newObject = FromJson(v_serializedObj, p_newObjectType, v_serializer);
                }
                catch { }
            }
            return v_newObject;
        }

        #endregion

        #region Public Unity Json Data Functions

        public static UnityJsonData ToUnityJsonData(object p_obj, System.Type p_type, Serializer p_serializer = null, UnityObjectConverter p_customConverter = null)
        {
            if (p_obj != null)
            {
                p_serializer = LockSerializer(p_serializer);
                try
                {
                    List<Object> v_context = new List<Object>();
                    var v_oldConverter = p_serializer.GetConverter(typeof(Object));
                    if (v_oldConverter == null || !v_oldConverter.GetType().IsSubclassOf(typeof(UnityObjectConverter)))
                    {
                        var v_converter = p_customConverter == null ? new UnityObjectConverter() : p_customConverter;
                        p_serializer.AddConverter(v_converter);
                    }
                    else if (v_oldConverter != null && p_customConverter != null && p_customConverter.GetType() != v_oldConverter.GetType())
                    {
                        p_serializer.AddConverter(p_customConverter);
                    }
                    p_serializer.Context.Set(v_context);
                    var v_data = new UnityJsonData();
                    v_data.Json = ToJson_UnsafeInternal(p_obj, p_type, false, p_serializer);
                    v_data.Context = v_context;
                    return v_data;
                }
                finally
                {
                    UnlockSerializer(p_serializer);
                }
            }
            return null;
        }

        public static UnityJsonData ToUnityJsonData<T>(T p_obj)
        {
            if (p_obj != null)
                return ToUnityJsonData(p_obj, typeof(T));
            return null;
        }

        public static object FromUnityJsonData(UnityJsonData p_data, System.Type p_type, Serializer p_serializer = null, UnityObjectConverter p_customConverter = null)
        {
            object v_target = null;
            if (p_data != null)
            {
                p_serializer = LockSerializer(p_serializer);
                try
                {
                    var v_oldConverter = p_serializer.GetConverter(typeof(Object));
                    if (v_oldConverter == null || !v_oldConverter.GetType().IsSubclassOf(typeof(UnityObjectConverter)))
                    {
                        var v_converter = p_customConverter == null ? new UnityObjectConverter() : p_customConverter;
                        p_serializer.AddConverter(v_converter);
                    }
                    else if (v_oldConverter != null && p_customConverter != null && p_customConverter.GetType() != v_oldConverter.GetType())
                    {
                        p_serializer.AddConverter(p_customConverter);
                    }
                    p_serializer.Context.Set(p_data.Context);
                    FromJson_UnsafeInternal(ref v_target, p_data.Json, p_type, p_serializer);
                }
                finally
                {
                    UnlockSerializer(p_serializer);
                }
            }
            return v_target;
        }

        public static T FromUnityJsonData<T>(UnityJsonData p_data, Serializer p_serializer = null, UnityObjectConverter p_customConverter = null)
        {
            if (p_data != null)
                return (T)FromUnityJsonData(p_data, typeof(T), p_serializer, p_customConverter);
            return default(T);
        }

        public static void FromUnityJsonDataOverwrite(UnityJsonData p_data, object p_target, Serializer p_serializer = null, UnityObjectConverter p_customConverter = null)
        {
            if (p_data != null)
            {
                p_serializer = LockSerializer(p_serializer);
                try
                {
                    var v_oldConverter = p_serializer.GetConverter(typeof(Object));
                    if (v_oldConverter == null || !v_oldConverter.GetType().IsSubclassOf(typeof(UnityObjectConverter)))
                    {
                        var v_converter = p_customConverter == null ? new UnityObjectConverter() : p_customConverter;
                        p_serializer.AddConverter(v_converter);
                    }
                    else if (v_oldConverter != null && p_customConverter != null && p_customConverter.GetType() != v_oldConverter.GetType())
                    {
                        p_serializer.AddConverter(p_customConverter);
                    }
                    p_serializer.Context.Set(p_data.Context);
                    FromJson_UnsafeInternal(ref p_target, p_data.Json, p_target.GetType(), p_serializer);
                }
                finally
                {
                    UnlockSerializer(p_serializer);
                }
            }
        }

        #endregion

        #region Public Json Functions

        public static string ToJson<T>(T p_obj, bool p_prettyPrint = false, Serializer p_serializer = null)
        {
            if (p_obj == null)
                return "";
            else
            {
                return ToJson_Internal(p_obj, typeof(T), p_prettyPrint, p_serializer);
            }
        }

        public static string ToJson<T>(T p_obj, bool p_prettyPrint, Config p_config)
        {
            Serializer v_serializer = new Serializer(p_config);
            return ToJson<T>(p_obj, p_prettyPrint, v_serializer);
        }

        public static string ToJson<T>(T p_obj, bool p_prettyPrint, JSONPolymorphicMode p_mode)
        {
            Serializer v_serializer = p_mode == JSONPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            return ToJson<T>(p_obj, p_prettyPrint, v_serializer);
        }

        public static string ToJson(object p_obj, System.Type p_type, bool p_prettyPrint, Serializer p_serializer = null)
        {
            if (p_obj == null)
                return "";
            else
            {
                return ToJson_Internal(p_obj, p_type, p_prettyPrint, p_serializer);
            }
        }

        public static string ToJson(object p_obj, System.Type p_type, bool p_prettyPrint, Config p_config)
        {
            Serializer v_serializer = new Serializer(p_config);
            return ToJson(p_obj, p_type, p_prettyPrint, v_serializer);
        }

        public static string ToJson(object p_obj, System.Type p_type, bool p_prettyPrint, JSONPolymorphicMode p_mode)
        {
            Serializer v_serializer = p_mode == JSONPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            return ToJson(p_obj, p_type, p_prettyPrint, v_serializer);
        }

        public static object FromJson(string p_json, System.Type p_type, Serializer p_serializer = null)
        {
            object v_target = null;
            FromJson_Internal(ref v_target, p_json, p_type, p_serializer);
            return v_target;
        }

        public static object FromJson(string p_json, System.Type p_type, Config p_config)
        {
            Serializer v_serializer = new Serializer(p_config);
            return FromJson(p_json, p_type, v_serializer);
        }

        public static object FromJson(string p_json, System.Type p_type, JSONPolymorphicMode p_mode)
        {
            Serializer v_serializer = p_mode == JSONPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            return FromJson(p_json, p_type, v_serializer);
        }

        public static T FromJson<T>(string p_json, Serializer p_serializer = null)
        {
            object v_target = default(T);
            FromJson_Internal(ref v_target, p_json, typeof(T), p_serializer);
            return (T)v_target;
        }

        public static T FromJson<T>(string p_json, Config p_config)
        {
            Serializer v_serializer = new Serializer(p_config);
            return FromJson<T>(p_json, v_serializer);
        }

        public static T FromJson<T>(string p_json, JSONPolymorphicMode p_mode)
        {
            Serializer v_serializer = p_mode == JSONPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            return FromJson<T>(p_json, v_serializer);
        }

        public static void FromJsonOverwrite(string p_json, object p_target, Serializer p_serializer = null)
        {
            if(p_target != null)
                FromJson_Internal(ref p_target, p_json, p_target.GetType(), p_serializer);
        }

        public static void FromJsonOverwrite(string p_json, object p_target, Config p_config)
        {
            Serializer v_serializer = new Serializer(p_config);
            FromJsonOverwrite(p_json, p_target, v_serializer);
        }

        public static void FromJsonOverwrite(string p_json, object p_target, JSONPolymorphicMode p_mode)
        {
            Serializer v_serializer = p_mode == JSONPolymorphicMode.Disabled ? DefaultSerializer : DefaultPolymorphicSerializer;
            FromJsonOverwrite(p_json, p_target, v_serializer);
        }

        public static void FromJsonOverwrite<T>(string p_json, ref T p_target, Serializer p_serializer = null)
        {
            p_target = FromJson<T>(p_json, p_serializer);
        }

        public static void FromJsonOverwrite<T>(string p_json, ref T p_target, Config p_config)
        {
            p_target = FromJson<T>(p_json, p_config);
        }

        public static void FromJsonOverwrite<T>(string p_json, ref T p_target, JSONPolymorphicMode p_mode)
        {
            p_target = FromJson<T>(p_json, p_mode);
        }

        #endregion

        #region Internal Functions

        private static void FromJson_UnsafeInternal(ref object p_target, string p_json, System.Type p_type, Serializer p_serializer)
        {
            if (!string.IsNullOrEmpty(p_json) && p_type != null)
            {
                if (p_serializer == null)
                    p_serializer = DefaultSerializer;

                Data v_data = null;
                JsonParser.Parse(p_json, p_serializer.Config, out v_data);
                if (p_type.IsSameOrSubClassOrImplementInterface(typeof(Data)))
                    p_target = v_data;
                else
                    p_serializer.TryDeserialize(v_data, p_type, ref p_target, null);
            }
        }

        private static void FromJson_Internal(ref object p_target, string p_json, System.Type p_type, Serializer p_serializer = null)
        {
            if (!string.IsNullOrEmpty(p_json) && p_type != null)
            {
                p_serializer = LockSerializer(p_serializer);
                try
                {
                    // The critical section.
                    FromJson_UnsafeInternal(ref p_target, p_json, p_type, p_serializer);
                }
                finally
                {
                    // Ensure that the lock is released.
                    UnlockSerializer(p_serializer);
                }
            }
        }

        private static string ToJson_UnsafeInternal(object p_target, System.Type p_type, bool p_prettyPrint, Serializer p_serializer)
        {
            if (p_type != null)
            {
                if (p_serializer == null)
                    p_serializer = DefaultSerializer;

                Data v_data = null;

                p_serializer.TrySerialize(p_type, p_target, out v_data, null);
                if (p_prettyPrint)
                    return JsonPrinter.PrettyJson(v_data);
                else
                    return JsonPrinter.CompressedJson(v_data);
            }
            return "";
        }

        private static string ToJson_Internal(object p_target, System.Type p_type, bool p_prettyPrint, Serializer p_serializer = null)
        {
            if (p_type != null)
            {
                //Try Block Serializer to make Thread-Safe Serialization/Deserialization
                p_serializer = LockSerializer(p_serializer);
                try
                {
                    // The critical section.
                    return ToJson_UnsafeInternal(p_target, p_type, p_prettyPrint, p_serializer);
                }
                finally
                {
                    // Ensure that the lock is released.
                    UnlockSerializer(p_serializer);
                }
            }
            return "";
        }

        protected static Serializer LockSerializer(Serializer p_serializer)
        {
            if (p_serializer == null)
                p_serializer = DefaultSerializer;

#if !UNITY_WEBGL || UNITY_EDITOR
            if (System.Threading.Monitor.TryEnter(p_serializer, 0))
            {
                return p_serializer;
            }
            else
            {
                var v_threadSerializer = new Serializer(p_serializer);
                System.Threading.Monitor.Enter(v_threadSerializer);

                return v_threadSerializer;
            }
#else
            return p_serializer;
#endif
        }

        protected static void UnlockSerializer(Serializer p_serializer)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (p_serializer != null)
                System.Threading.Monitor.Exit(p_serializer);
#endif
        }

        #endregion
    }
}
