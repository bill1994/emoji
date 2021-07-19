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

        public override Result TrySerialize(object instance, out JsonObject serialized, Type storageType) {
            var weakRef = (WeakReference)instance;

            var result = Result.Success;
            serialized = JsonObject.CreateDictionary(Serializer.Config);

            if (weakRef.IsAlive) {
                JsonObject data;
                if ((result += Serializer.TrySerialize(weakRef.Target, out data, Serializer.CurrentMetaProperty)).Failed) {
                    return result;
                }

                serialized.AsDictionary["Target"] = data;
                serialized.AsDictionary["TrackResurrection"] = new JsonObject(weakRef.TrackResurrection);
            }

            return result;
        }

        public override Result TryDeserialize(JsonObject data, ref object instance, Type storageType) {
            var result = Result.Success;

            if ((result += CheckType(data, JsonObjectType.Object)).Failed) return result;

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

        public override object CreateInstance(JsonObject data, Type storageType) {
            return new WeakReference(null);
        }
    }
}