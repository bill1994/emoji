using System;
using System.Collections.Generic;
using Kyub.Serialization;
using UnityObject = UnityEngine.Object;
using Kyub.Reflection;
using System.Reflection;

namespace Kyub.Serialization.Internal
{
    /// <summary>
    /// The hack that's used to persist UnityEngine.Object references
    /// Whenever the serializer comes across a Unity object it stores it to a list
    /// of Unity objects (which Unity serializes) and serializes the index of where
    /// that storage took place.
    /// </summary>
	public class DataConverter : ReflectedConverter
    {
        public override bool CanProcess(Type type)
        {
            return typeof(Data).IsAssignableFrom(type);
        }

        public override bool RequestCycleSupport(Type storageType)
        {
            return false;
        }

        public override bool RequestInheritanceSupport(Type storageType)
        {
            return false;
        }

        protected virtual bool IsValidType(Type p_type)
        {
            return p_type != null && (p_type == typeof(Data) || p_type.IsSubclassOf(typeof(Data)));
        }

        public override Result TrySerialize(object instance, out Data serialized, Type storageType)
        {
            //UnityObject is the root of serialization (we must expose it)
            if (instance is Data)
            {
                serialized = instance as Data;
                return Result.Success;
            }
            serialized = new Data();
            return Result.Fail("Error Serializing Data");
        }

        public override Result TryDeserialize(Data data, ref object instance, Type storageType)
        {
            try
            {
                //var result = Result.Success;
                if (IsValidType(storageType) && data != null)
                {
                    instance = data;
                    return Result.Success;
                }
            }
            catch { }
            return Result.Fail("Error Deserializing Data");
        }

        public override object CreateInstance(Data data, Type storageType)
        {
            return null;
        }
    }
}