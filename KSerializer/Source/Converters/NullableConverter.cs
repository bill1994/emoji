using System;

namespace Kyub.Serialization.Internal {
    /// <summary>
    /// The reflected converter will properly serialize nullable types. However, we do it here
    /// instead as we can emit less serialization data.
    /// </summary>
    public class NullableConverter : Converter {
        public override bool CanProcess(Type type) {
            return
                type.Resolve().IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public override Result TrySerialize(object instance, out Data serialized, Type storageType) {
            // null is automatically serialized
            return Serializer.TrySerialize(Nullable.GetUnderlyingType(storageType), instance, out serialized, Serializer.CurrentMetaProperty);
        }

        public override Result TryDeserialize(Data data, ref object instance, Type storageType) {
            // null is automatically deserialized
            return Serializer.TryDeserialize(data, Nullable.GetUnderlyingType(storageType), ref instance, Serializer.CurrentMetaProperty);
        }

        public override object CreateInstance(Data data, Type storageType) {
            return storageType;
        }
    }
}