using System;
using System.Collections;

namespace Kyub.Serialization.Internal {
    public class ArrayConverter : Converter {
        public override bool CanProcess(Type type) {
            return type.IsArray;
        }

        public override bool RequestCycleSupport(Type storageType) {
            return false;
        }

        public override bool RequestInheritanceSupport(Type storageType) {
            return false;
        }

        public override Result TrySerialize(object instance, out Data serialized, Type storageType) {
            // note: IList[index] is **significantly** faster than Array.Get, so make sure we use
            //       that instead.

            IList arr = (Array)instance;
            Type elementType = storageType.GetElementType();

            var result = Result.Success;

            serialized = Data.CreateList(arr.Count);
            var serializedList = serialized.AsList;

            for (int i = 0; i < arr.Count; ++i) {
                object item = arr[i];

                Data serializedItem;

                var itemResult = Serializer.TrySerialize(elementType, item, out serializedItem, Serializer.CurrentMetaProperty);
                result.AddMessages(itemResult);
                if (itemResult.Failed) continue;

                serializedList.Add(serializedItem);
            }

            return result;
        }

        public override Result TryDeserialize(Data data, ref object instance, Type storageType) {
            var result = Result.Success;

            // Verify that we actually have an List
            if ((result += CheckType(data, DataType.Array)).Failed) {
                return result;
            }

            Type elementType = storageType.GetElementType();

            var serializedList = data.AsList;
            var list = new ArrayList(serializedList.Count);

            for (int i = 0; i < serializedList.Count; ++i) {
                var serializedItem = serializedList[i];
                object deserialized = null;

                var itemResult = Serializer.TryDeserialize(serializedItem, elementType, ref deserialized, Serializer.CurrentMetaProperty);
                result.AddMessages(itemResult);
                if (itemResult.Failed) continue;

                list.Add(deserialized);
            }

            instance = list.ToArray(elementType);
            return result;
        }

        public override object CreateInstance(Data data, Type storageType) {
            return Serializer.Config.MetaTypeCache.Get(storageType).CreateInstance();
        }
    }
}