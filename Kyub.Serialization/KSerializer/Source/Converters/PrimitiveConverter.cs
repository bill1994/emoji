using System;

namespace Kyub.Serialization.Internal {
    public class PrimitiveConverter : Converter {
        public override bool CanProcess(Type type) {
            return
                type.Resolve().IsPrimitive ||
                type == typeof(string) ||
                type == typeof(decimal);
        }

        public override bool RequestCycleSupport(Type storageType) {
            return false;
        }

        public override bool RequestInheritanceSupport(Type storageType) {
            return false;
        }

        private static bool UseBool(Type type) {
            return type == typeof(bool);
        }

        private static bool UseInt64(Type type) {
            return type == typeof(sbyte) || type == typeof(byte) ||
                   type == typeof(Int16) || type == typeof(UInt16) ||
                   type == typeof(Int32) || type == typeof(UInt32) ||
                   type == typeof(Int64) || type == typeof(UInt64);
        }

        private static bool UseDouble(Type type) {
            return type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(decimal);
        }

        private static bool UseString(Type type) {
            return type == typeof(string) ||
                   type == typeof(char);
        }

        public override Result TrySerialize(object instance, out Data serialized, Type storageType) {
            var instanceType = instance.GetType();

            if (UseBool(instanceType)) {
                serialized = new Data((bool)instance);
                return Result.Success;
            }

            if (UseInt64(instanceType)) {
                serialized = new Data((Int64)Convert.ChangeType(instance, typeof(Int64)));
                return Result.Success;
            }

            if (UseDouble(instanceType)) {
                serialized = new Data((double)Convert.ChangeType(instance, typeof(double)));
                return Result.Success;
            }

            if (UseString(instanceType)) {
                serialized = new Data((string)Convert.ChangeType(instance, typeof(string)));
                return Result.Success;
            }

            serialized = null;
            return Result.Fail("Unhandled primitive type " + instance.GetType());
        }

        public override Result TryDeserialize(Data storage, ref object instance, Type storageType) {
            var result = Result.Success;

            if (UseBool(storageType)) {
                var sucess = CheckType(storage, DataType.Boolean);

                //(result += CheckType(storage, DataType.Boolean)).Succeeded) {
                if (sucess.Succeeded) { 
                    instance = storage.AsBool;
                }
                else if (storage.IsInt64)
                {
                    sucess = Result.Success;
                    instance = ((long)Convert.ChangeType(storage.AsInt64, storageType)) != 0;
                }
                result += sucess;
                return result;
            }

            if (UseDouble(storageType) || UseInt64(storageType)) {
                if (storage.IsDouble) {
                    instance = Convert.ChangeType(storage.AsDouble, storageType);
                }
                else if (storage.IsInt64) {
                    instance = Convert.ChangeType(storage.AsInt64, storageType);
                }
                else {
                    return Result.Fail(GetType().Name + " expected number but got " + storage.Type + " in " + storage);
                }
                return Result.Success;
            }

            if (UseString(storageType)) {
                if ((result += CheckType(storage, DataType.String)).Succeeded) {
                    instance = storage.AsString;
                }
                return result;
            }

            return Result.Fail(GetType().Name + ": Bad data; expected bool, number, string, but got " + storage);
        }
    }
}

