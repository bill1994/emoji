using System;

namespace Kyub.Serialization.Internal {
    public class TypeConverter : Converter {
        public override bool CanProcess(Type type) {
            return typeof(Type).IsAssignableFrom(type);
        }

        public override bool RequestCycleSupport(Type type) {
            return false;
        }

        public override bool RequestInheritanceSupport(Type type) {
            return false;
        }

        public override Result TrySerialize(object instance, out JsonObject serialized, Type storageType) {
            var type = (Type)instance;
            serialized = new JsonObject(type.FullName);
            return Result.Success;
        }

        public override Result TryDeserialize(JsonObject data, ref object instance, Type storageType) {
            if (data.IsString == false) {
                return Result.Fail("Type converter requires a string");
            }

            instance = TypeLookup.GetType(data.AsString);
            if (instance == null) {
                return Result.Fail("Unable to find type " + data.AsString);
            }
            return Result.Success;
        }

        public override object CreateInstance(JsonObject data, Type storageType) {
            return storageType;
        }
    }
}