#if !NO_UNITY
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyub.Serialization {
    partial class ConverterRegister {
        public static Internal.DirectConverters.LayerMask_DirectConverter Register_LayerMask_DirectConverter;
    }
}

namespace Kyub.Serialization.Internal.DirectConverters {
    public class LayerMask_DirectConverter : DirectConverter<LayerMask> {
        protected override Result DoSerialize(LayerMask model, Dictionary<string, JsonObject> serialized) {
            var result = Result.Success;

            result += SerializeMember(serialized, "value", model.value);

            return result;
        }

        protected override Result DoDeserialize(Dictionary<string, JsonObject> data, ref LayerMask model) {
            var result = Result.Success;

            var t0 = model.value;
            result += DeserializeMember(data, "value", out t0);
            model.value = t0;

            return result;
        }

        public override object CreateInstance(JsonObject data, Type storageType) {
            return new LayerMask();
        }
    }
}
#endif