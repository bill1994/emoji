using System;
using System.Collections;
using System.Collections.Generic;

namespace Kyub.Serialization.Internal {
    public class ReflectedConverter : Converter {

        public override bool CanProcess(Type type) {
            if (type.Resolve().IsArray ||
                typeof(ICollection).IsAssignableFrom(type)) {

                return false;
            }

            return true;
        }

        public override Result TrySerialize(object instance, out JsonObject serialized, Type storageType) {
            serialized = JsonObject.CreateDictionary(Serializer.Config);
            var result = Result.Success;

            MetaType metaType = Serializer.Config.MetaTypeCache.Get(instance.GetType());
            metaType.EmitAotData();

            for (int i = 0; i < metaType.Properties.Length; ++i) {
                MetaProperty property = metaType.Properties[i];
                if (!property.CanRead || !property.CanSerializeInContext(instance)) continue;

                JsonObject serializedData;

                var itemResult = Serializer.TrySerialize(property.StorageType, property.Read(instance), out serializedData, property);
                result.AddMessages(itemResult);
                if (itemResult.Failed) {
                    continue;
                }

                serialized.AsDictionary[property.JsonName] = serializedData;
            }

            return result;
        }

        public override Result TryDeserialize(JsonObject data, ref object instance, Type storageType) {
            var result = Result.Success;

            // Verify that we actually have an Object
            if ((result += CheckType(data, JsonObjectType.Object)).Failed) {
                return result;
            }

            MetaType metaType = Serializer.Config.MetaTypeCache.Get(storageType);
            metaType.EmitAotData();

            for (int i = 0; i < metaType.Properties.Length; ++i) {
                MetaProperty property = metaType.Properties[i];
                if (property.CanWrite == false) continue;

                JsonObject propertyData;
                var jsonPossibleNames = new List<string>();
                jsonPossibleNames.Add(property.JsonName);
                jsonPossibleNames.AddRange(property.FallbackNames);
                
                foreach (var jsonName in jsonPossibleNames)
                {
                    if (!string.IsNullOrEmpty(jsonName) && data.AsDictionary.TryGetValue(jsonName, out propertyData))
                    {
                        object deserializedValue = null;

                        var itemResult = Serializer.TryDeserialize(propertyData, property.StorageType, ref deserializedValue, property);
                        result.AddMessages(itemResult);
                        if (itemResult.Failed) continue;

                        property.Write(instance, deserializedValue);
                    }
                }
            }

            return result;
        }

        public override object CreateInstance(JsonObject data, Type storageType) {
            MetaType metaType = Serializer.Config.MetaTypeCache.Get(storageType);
            return metaType.CreateInstance();
        }
    }
}