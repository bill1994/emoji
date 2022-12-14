#if !NO_UNITY
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyub.Serialization {
    partial class ConverterRegister {
        public static Internal.DirectConverters.Keyframe_DirectConverter Register_Keyframe_DirectConverter;
    }
}

namespace Kyub.Serialization.Internal.DirectConverters {
    public class Keyframe_DirectConverter : DirectConverter<Keyframe> {
        protected override Result DoSerialize(Keyframe model, Dictionary<string, JsonObject> serialized) {
            var result = Result.Success;

            result += SerializeMember(serialized, "time", model.time);
            result += SerializeMember(serialized, "value", model.value);
            //result += SerializeMember(serialized, "tangentMode", model.tangentMode);
            result += SerializeMember(serialized, "inTangent", model.inTangent);
            result += SerializeMember(serialized, "outTangent", model.outTangent);

            return result;
        }

        protected override Result DoDeserialize(Dictionary<string, JsonObject> data, ref Keyframe model) {
            var result = Result.Success;

            var t0 = model.time;
            result += DeserializeMember(data, "time", out t0);
            model.time = t0;

            var t1 = model.value;
            result += DeserializeMember(data, "value", out t1);
            model.value = t1;

            /*var t2 = model.tangentMode;
            result += DeserializeMember(data, "tangentMode", out t2);
            model.tangentMode = t2;*/

            var t3 = model.inTangent;
            result += DeserializeMember(data, "inTangent", out t3);
            model.inTangent = t3;

            var t4 = model.outTangent;
            result += DeserializeMember(data, "outTangent", out t4);
            model.outTangent = t4;

            return result;
        }

        public override object CreateInstance(JsonObject data, Type storageType) {
            return new Keyframe();
        }
    }
}
#endif