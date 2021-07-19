using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kyub.Serialization.Internal {
    public class KeyValuePairConverter : Converter {

        public override bool CanProcess(Type type) {
            return
                type.Resolve().IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
        }

        public override bool RequestCycleSupport(Type storageType) {
            return false;
        }

        public override bool RequestInheritanceSupport(Type storageType) {
            return false;
        }

        public override Result TryDeserialize(JsonObject data, ref object instance, Type storageType) {
            var result = Result.Success;

            JsonObject keyData, valueData;
            if ((result += CheckKey(data, "Key", out keyData)).Failed) return result;
            if ((result += CheckKey(data, "Value", out valueData)).Failed) return result;

            var genericArguments = storageType.GetGenericArguments();
            Type keyType = genericArguments[0], valueType = genericArguments[1];

            object keyObject = null, valueObject = null;
            result.AddMessages(Serializer.TryDeserialize(keyData, keyType, ref keyObject, Serializer.CurrentMetaProperty));
            result.AddMessages(Serializer.TryDeserialize(valueData, valueType, ref valueObject, Serializer.CurrentMetaProperty));

            instance = Activator.CreateInstance(storageType, keyObject, valueObject);
            return result;
        }

        public override Result TrySerialize(object instance, out JsonObject serialized, Type storageType) {
            PropertyInfo keyProperty = storageType.GetDeclaredProperty("Key");
            PropertyInfo valueProperty = storageType.GetDeclaredProperty("Value");

            object keyObject = keyProperty.GetValue(instance, null);
            object valueObject = valueProperty.GetValue(instance, null);

            var genericArguments = storageType.GetGenericArguments();
            Type keyType = genericArguments[0], valueType = genericArguments[1];

            var result = Result.Success;

            JsonObject keyData, valueData;
            result.AddMessages(Serializer.TrySerialize(keyType, keyObject, out keyData, Serializer.CurrentMetaProperty));
            result.AddMessages(Serializer.TrySerialize(valueType, valueObject, out valueData, Serializer.CurrentMetaProperty));

            serialized = JsonObject.CreateDictionary(Serializer.Config);
            if (keyData != null) serialized.AsDictionary["Key"] = keyData;
            if (valueData != null) serialized.AsDictionary["Value"] = valueData;

            return result;
        }
    }
}