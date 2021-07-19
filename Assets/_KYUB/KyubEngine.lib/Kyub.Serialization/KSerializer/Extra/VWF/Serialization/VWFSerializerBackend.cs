using System;
using System.Collections.Generic;
using Kyub.Serialization;
using Kyub.Serialization.Internal;
using UnityObject = UnityEngine.Object;

namespace Kyub.Serialization.VWF.Serialization
{ 
    /// <summary>
    /// An adaptor/proxy to add support for the JSON serializer KSerializer
    /// </summary>
    public class VWFSerializerBackend : SerializerBackend
    {
        public static readonly Kyub.Serialization.Serializer Serializer;

        public VWFSerializerBackend()
        {
            Logic = VFWSerializationLogic.Instance;
        }

        static VWFSerializerBackend()
        {
            SerializerConfig _newConfig = new SerializerConfig();
            _newConfig.SerializeAttributes = VFWSerializationLogic.Instance.SerializeMember;
            _newConfig.IgnoreSerializeAttributes = VFWSerializationLogic.Instance.DontSerializeMember;
            Serializer = new Kyub.Serialization.Serializer(_newConfig);
            Serializer.AddConverter(new UnityObjectConverter());
            Serializer.AddConverter(new MethodInfoConverter());
        }

        public override string Serialize(Type type, object value, object context)
        {
            Serializer.Context.Set(context as List<UnityObject>);

            JsonObject data;
            Serializer.TrySerialize(type, value, out data, null);
            //if (fail.Failed) throw new Exception(fail.FormattedMessages);

            return JsonPrinter.CompressedJson(data);
        }

        public virtual string SafeSerialize(Type type, object value, object context)
        {
            try
            {
                return Serialize(type, value, context);
            }
            catch { }
            return "";
        }

        public override object Deserialize(Type type, string serializedState, object context)
        {
            JsonObject data;
            Result status = JsonParser.Parse(serializedState, Serializer.Config, out data);
            if (status.Failed) throw new Exception(status.FormattedMessages);

            Serializer.Context.Set(context as List<UnityObject>);

            object deserialized = null;
            status = Serializer.TryDeserialize(data, type, ref deserialized, null);
            //if (status.Failed) throw new Exception(status.FormattedMessages);
            return deserialized;
        }

        public virtual object SafeDeserialize(Type type, string serializedState, object context)
        {
            try
            {
                return Deserialize(type, serializedState, context);
            }
            catch {}

            if (type != null && type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }
    }
}
