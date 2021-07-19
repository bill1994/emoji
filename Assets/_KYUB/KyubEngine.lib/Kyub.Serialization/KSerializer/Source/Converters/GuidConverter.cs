using System;

namespace Kyub.Serialization.Internal {
    /// <summary>
    /// Serializes and deserializes guids.
    /// </summary>
    public class GuidConverter : Converter {
        public override bool CanProcess(Type type) {
            return type == typeof(Guid);
        }

        public override bool RequestCycleSupport(Type storageType) {
            return false;
        }

        public override bool RequestInheritanceSupport(Type storageType) {
            return false;
        }

        public override Result TrySerialize(object instance, out JsonObject serialized, Type storageType) {
            var guid = (Guid)instance;
            serialized = new JsonObject(guid.ToString());
            return Result.Success;
        }

        public override Result TryDeserialize(JsonObject data, ref object instance, Type storageType) {
            if (data.IsString) {
                instance = new Guid(data.AsString);
                return Result.Success;
            }

            return Result.Fail("GuidConverter encountered an unknown JSON data type");
        }

        public override object CreateInstance(JsonObject data, Type storageType) {
            return new Guid();
        }
    }
}