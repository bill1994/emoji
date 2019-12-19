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

        public override Result TrySerialize(object instance, out Data serialized, Type storageType) {
            var type = (Type)instance;
            serialized = new Data(type.FullName);
            return Result.Success;
        }

        public override Result TryDeserialize(Data data, ref object instance, Type storageType) {
            if (data.IsString == false) {
                return Result.Fail("Type converter requires a string");
            }

            instance = TypeLookup.GetType(data.AsString);
            if (instance == null) {
                return Result.Fail("Unable to find type " + data.AsString);
            }
            return Result.Success;
        }

        public override object CreateInstance(Data data, Type storageType) {
            return storageType;
        }
    }
}