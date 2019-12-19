using System;

namespace Kyub.Serialization.Internal {
    /// <summary>
    /// Serializes and deserializes WeakReferences.
    /// </summary>
    public class WeakReferenceConverter : Converter {

        public override bool CanProcess(Type type) {
            return type == typeof(WeakReference);
        }

        public override bool RequestCycleSupport(Type storageType) {
            return false;
        }

        public override bool RequestInheritanceSupport(Type storageType) {
            return false;
        }

        public override Result TrySerialize(object instance, out Data serialized, Type storageType) {
            var weakRef = (WeakReference)instance;

            var result = Result.Success;
            serialized = Data.CreateDictionary(Serializer.Config);

            if (weakRef.IsAlive) {
                Data data;
                if ((result += Serializer.TrySerialize(weakRef.Target, out data, Serializer.CurrentMetaProperty)).Failed) {
                    return result;
                }

                serialized.AsDictionary["Target"] = data;
                serialized.AsDictionary["TrackResurrection"] = new Data(weakRef.TrackResurrection);
            }

            return result;
        }

        public override Result TryDeserialize(Data data, ref object instance, Type storageType) {
            var result = Result.Success;

            if ((result += CheckType(data, DataType.Object)).Failed) return result;

            if (data.AsDictionary.ContainsKey("Target")) {
                var targetData = data.AsDictionary["Target"];
                object targetInstance = null;

                if ((result += Serializer.TryDeserialize(targetData, typeof(object), ref targetInstance, Serializer.CurrentMetaProperty)).Failed) return result;

                bool trackResurrection = false;
                if (data.AsDictionary.ContainsKey("TrackResurrection") && data.AsDictionary["TrackResurrection"].IsBool) {
                    trackResurrection = data.AsDictionary["TrackResurrection"].AsBool;
                }

                instance = new WeakReference(targetInstance, trackResurrection);
            }

            return result;
        }

        public override object CreateInstance(Data data, Type storageType) {
            return new WeakReference(null);
        }
    }
}