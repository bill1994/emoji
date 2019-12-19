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
	public class UnityObjectIgnoreConverter : ReflectedConverter
    {
        public override bool CanProcess(Type type)
        {
            return typeof(UnityObject).IsAssignableFrom(type);
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
            return p_type != null && (p_type == typeof(UnityEngine.MonoBehaviour) || p_type == typeof(UnityEngine.ScriptableObject) || p_type.IsSubclassOf(typeof(UnityEngine.MonoBehaviour)) || p_type.IsSubclassOf(typeof(UnityEngine.ScriptableObject)));
        }

        public override Result TrySerialize(object instance, out Data serialized, Type storageType)
        {
            //UnityObject is the root of serialization (we must expose it)
            if (Serializer.CurrentDepth == 1 && IsValidType(storageType))
            {
                return base.TrySerialize(instance, out serialized, storageType);
            }
            serialized = new Data();
            return Result.Fail("Error Serializing Unity.Object");
        }

        public override Result TryDeserialize(Data data, ref object instance, Type storageType)
        {
            try
            {
                //var result = Result.Success;
                if (IsValidType(storageType) && instance != null)
                {
                    return base.TryDeserialize(data, ref instance, storageType);
                }
            }
            catch { }
            return Result.Fail("Error Deserializing Unity.Object");
        }

        public override object CreateInstance(Data data, Type storageType)
        {
            return null;
        }
    }
}