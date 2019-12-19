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
	public class UnityObjectConverter : ReflectedConverter
    {
        private List<UnityObject> serializedObjects
        {
            get
            {
                List<UnityObject> v_context = Serializer.Context.Get<List<UnityObject>>();
                return v_context;
            }
        }

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
            else
            {
                try
                {
                    if (serializedObjects != null)
                    {
                        var obj = instance as UnityObject;
                        int idx = serializedObjects.IndexOf(obj);
                        if (idx == -1)
                        {
                            Serializer.TrySerialize<int>(serializedObjects.Count, out serialized, Serializer.CurrentMetaProperty);
                            serializedObjects.Add(obj);
                        }
                        else
                            Serializer.TrySerialize<int>(idx, out serialized, Serializer.CurrentMetaProperty);
                        return Result.Success;
                    }
                    else
                    {
                        serialized = new Data();
                        return Result.Fail("No context to save Unity.Objects");
                    }
                }
                catch { }
                serialized = new Data();
                return Result.Fail("Error Serializing Unity.Object");
            }
        }

        public override Result TryDeserialize(Data data, ref object instance, Type storageType)
        {
            try
            {
                var result = Result.Success;
                if (data.IsInt64)
                {
                    int index = -1;
                    Serializer.TryDeserialize<int>(data, ref index, Serializer.CurrentMetaProperty);
                    //if (index == -1)
                    //    throw new InvalidOperationException("Error deserializing Unity object of type " + storageType + ". Index shouldn't be -1. Message: " + result.FormattedMessages);
                    instance = serializedObjects != null && serializedObjects.Count > index && index >= 0 ? serializedObjects[index] : null;
                    return result;
                }
                else if (IsValidType(storageType))
                {
                    return base.TryDeserialize(data, ref instance, storageType);
                }
                else
                {
                    instance = null;
                    return result;
                }
            }
            catch { }
            return Result.Fail("Error Deserializing Unity.Object");
        }

		public override object CreateInstance(Data data, Type storageType)
		{
            if (storageType != null && storageType.IsSubclassOf(typeof(UnityEngine.ScriptableObject)))
            {
                return UnityEngine.ScriptableObject.CreateInstance(storageType);
            }
            return null;
		}
	}
}