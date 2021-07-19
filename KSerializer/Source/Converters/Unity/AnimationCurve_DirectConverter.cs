#if !NO_UNITY
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyub.Serialization {
    partial class ConverterRegister {
        public static Internal.DirectConverters.AnimationCurve_DirectConverter Register_AnimationCurve_DirectConverter;
    }
}

namespace Kyub.Serialization.Internal.DirectConverters {
    public class AnimationCurve_DirectConverter : DirectConverter<AnimationCurve> {
        protected override Result DoSerialize(AnimationCurve model, Dictionary<string, JsonObject> serialized) {
            var result = Result.Success;

            result += SerializeMember(serialized, "keys", model.keys);
            result += SerializeMember(serialized, "preWrapMode", model.preWrapMode);
            result += SerializeMember(serialized, "postWrapMode", model.postWrapMode);

            return result;
        }

        protected override Result DoDeserialize(Dictionary<string, JsonObject> data, ref AnimationCurve model) {
            var result = Result.Success;

            var t0 = model.keys;
            result += DeserializeMember(data, "keys", out t0);
            model.keys = t0;

            var t1 = model.preWrapMode;
            result += DeserializeMember(data, "preWrapMode", out t1);
            model.preWrapMode = t1;

            var t2 = model.postWrapMode;
            result += DeserializeMember(data, "postWrapMode", out t2);
            model.postWrapMode = t2;

            return result;
        }

        public override object CreateInstance(JsonObject data, Type storageType) {
            return new AnimationCurve();
        }
    }
}
#endif