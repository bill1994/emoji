using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Kyub.Serialization.Internal {
    /// <summary>
    /// Provides serialization support for anything which extends from `IEnumerable` and has an `Add` method.
    /// </summary>
    public class IEnumerableConverter : Converter {
        public override bool CanProcess(Type type) {
            if (typeof(IEnumerable).IsAssignableFrom(type) == false) return false;
            return GetAddMethod(type) != null;
        }

        public override object CreateInstance(JsonObject data, Type storageType) {
            return Serializer.Config.MetaTypeCache.Get(storageType).CreateInstance();
        }

        public override Result TrySerialize(object instance_, out JsonObject serialized, Type storageType) {
            var instance = (IEnumerable)instance_;
            var result = Result.Success;

            Type elementType = GetElementType(storageType);

            serialized = JsonObject.CreateList(HintSize(instance));
            var serializedList = serialized.AsList;

            foreach (object item in instance) {
                JsonObject itemData;

                // note: We don't fail the entire deserialization even if the item failed
                var itemResult = Serializer.TrySerialize(elementType, item, out itemData, Serializer.CurrentMetaProperty);
                result.AddMessages(itemResult);
                if (itemResult.Failed) continue;

                serializedList.Add(itemData);
            }

            return result;
        }

        public override Result TryDeserialize(JsonObject data, ref object instance_, Type storageType) {
            var instance = (IEnumerable)instance_;
            var result = Result.Success;

            if ((result += CheckType(data, JsonObjectType.Array)).Failed) return result;

            var elementStorageType = GetElementType(storageType);
            var addMethod = GetAddMethod(storageType);

            var serializedList = data.AsList;
            for (int i = 0; i < serializedList.Count; ++i) {
                var itemData = serializedList[i];
                object itemInstance = null;

                // note: We don't fail the entire deserialization even if the item failed
                var itemResult = Serializer.TryDeserialize(itemData, elementStorageType, ref itemInstance, Serializer.CurrentMetaProperty);
                result.AddMessages(itemResult);
                if (itemResult.Failed) continue;

                addMethod.Invoke(instance, new object[] { itemInstance });
            }

            return result;
        }

        private static int HintSize(IEnumerable collection) {
            if (collection is ICollection) {
                return ((ICollection)collection).Count;
            }
            return 0;
        }

        /// <summary>
        /// Fetches the element type for objects inside of the collection.
        /// </summary>
        private static Type GetElementType(Type objectType) {
            if (objectType.HasElementType) return objectType.GetElementType();

            Type enumerableList = ReflectionUtility.GetInterface(objectType, typeof(IEnumerable<>));
            if (enumerableList != null) return enumerableList.GetGenericArguments()[0];

            return typeof(object);
        }

        private static MethodInfo GetAddMethod(Type type) {
            // There is a really good chance the type will extend ICollection{}, which will contain
            // the add method we want. Just doing type.GetFlattenedMethod() may return the incorrect one --
            // for example, with dictionaries, it'll return Add(TKey, TValue), and we want
            // Add(KeyValuePair<TKey, TValue>).
            Type collectionInterface = ReflectionUtility.GetInterface(type, typeof(ICollection<>));
            if (collectionInterface != null) {
                MethodInfo add = collectionInterface.GetDeclaredMethod("Add");
                if (add != null) return add;
            }

            // Otherwise try and look up a general Add method.
            return
                type.GetFlattenedMethod("Add") ??
                type.GetFlattenedMethod("Push") ??
                type.GetFlattenedMethod("Enqueue");
        }
    }
}