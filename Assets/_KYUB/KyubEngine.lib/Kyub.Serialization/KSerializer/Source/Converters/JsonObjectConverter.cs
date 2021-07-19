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
	public class JsonObjectConverter : ReflectedConverter
    {
        public override bool CanProcess(Type type)
        {
            return typeof(JsonObject).IsAssignableFrom(type);
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
            return p_type != null && (p_type == typeof(JsonObject) || p_type.IsSubclassOf(typeof(JsonObject)));
        }

        public override Result TrySerialize(object instance, out JsonObject serialized, Type storageType)
        {
            //UnityObject is the root of serialization (we must expose it)
            if (instance is JsonObject)
            {
                serialized = instance as JsonObject;
                return Result.Success;
            }
            serialized = new JsonObject();
            return Result.Fail("Error Serializing Data");
        }

        public override Result TryDeserialize(JsonObject data, ref object instance, Type storageType)
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

        public override object CreateInstance(JsonObject data, Type storageType)
        {
            return null;
        }
    }
}