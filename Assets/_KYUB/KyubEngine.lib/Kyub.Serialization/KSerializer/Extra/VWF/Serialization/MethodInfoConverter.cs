using System;
using Kyub.Serialization;
using Kyub.Reflection;
using System.Reflection;

namespace Kyub.Serialization.VWF.Serialization
{
	public class MethodInfoConverter : Converter
	{
        public override bool RequestCycleSupport(Type storageType)
        {
            return false;
        }

		public override bool CanProcess(Type type)
		{
            return typeof(System.Reflection.MethodInfo).IsAssignableFrom(type);
        }

		public override Result TrySerialize(object instance, out JsonObject serialized, Type storageType)
		{
            var method = instance as System.Reflection.MethodInfo;
            serialized = JsonObject.CreateList();
            var list = serialized.AsList;

            JsonObject declTypeData;
            Serializer.TrySerialize(method.DeclaringType, out declTypeData, Serializer.CurrentMetaProperty);

            list.Add(declTypeData);
            list.Add(new JsonObject(method.Name));

            var args = method.GetParameters();
            list.Add(new JsonObject(args.Length));

            for(int i = 0; i < args.Length; i++)
            { 
                JsonObject argData;
                Serializer.TrySerialize(args[i].ParameterType, out argData, Serializer.CurrentMetaProperty);
                list.Add(argData);
            }

			return Result.Success;
		}

		public override Result TryDeserialize(JsonObject data, ref object instance, Type storageType)
		{
            var list = data.AsList;

            Type declaringType = null;
            Serializer.TryDeserialize(list[0], ref declaringType, Serializer.CurrentMetaProperty);

            string methodName = list[1].AsString;
            int argCount = (int)list[2].AsInt64;
            var argTypes = new Type[argCount];
            for(int i = 0; i < argCount; i++)
            { 
                var argData = list[i + 3];
                Serializer.TryDeserialize(argData, ref argTypes[i], Serializer.CurrentMetaProperty);
            }

            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            instance = declaringType.GetMethod(methodName, flags, null, argTypes, null);
			return Result.Success;
		}
	}
}