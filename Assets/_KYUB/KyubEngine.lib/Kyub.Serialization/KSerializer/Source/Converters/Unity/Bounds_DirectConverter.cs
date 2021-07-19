#if !NO_UNITY
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyub.Serialization {
    partial class ConverterRegister {
        public static Internal.DirectConverters.Bounds_DirectConverter Register_Bounds_DirectConverter;
    }
}

namespace Kyub.Serialization.Internal.DirectConverters {
    public class Bounds_DirectConverter : DirectConverter<Bounds> {
        protected override Result DoSerialize(Bounds model, Dictionary<string, JsonObject> serialized) {
            var result = Result.Success;

            result += SerializeMember(serialized, "center", model.center);
            result += SerializeMember(serialized, "size", model.size);

            return result;
        }

        protected override Result DoDeserialize(Dictionary<string, JsonObject> data, ref Bounds model) {
            var result = Result.Success;

            var t0 = model.center;
            result += DeserializeMember(data, "center", out t0);
            model.center = t0;

            var t1 = model.size;
            result += DeserializeMember(data, "size", out t1);
            model.size = t1;

            return result;
        }

        public override object CreateInstance(JsonObject data, Type storageType) {
            return new Bounds();
        }
    }
}
#endif