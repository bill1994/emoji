using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Kyub.Serialization.Internal {

    public class EnumAsStringConverter : EnumConverter
    {
        public override bool ConvertAsString { get { return true; } }
    }

    /// <summary>
    /// Serializes and deserializes enums by their current name.
    /// </summary>
    public class EnumConverter : Converter {
        
        public virtual bool ConvertAsString { get { return false; } }

        public override bool CanProcess(Type type) {
            return type.Resolve().IsEnum;
        }

        public override bool RequestCycleSupport(Type storageType) {
            return false;
        }

        public override bool RequestInheritanceSupport(Type storageType) {
            return false;
        }

        public override object CreateInstance(JsonObject data, Type storageType) {
            // In .NET compact, Enum.ToObject(Type, Object) is defined but the overloads like
            // Enum.ToObject(Type, int) are not -- so we get around this by boxing the value.
            return Enum.ToObject(storageType, (object)0);
        }

        public override Result TrySerialize(object instance, out JsonObject serialized, Type storageType) {

            var isFlag = PortableReflection.GetAttribute<FlagsAttribute>(storageType) != null;
            if (!ConvertAsString)
            {
                long instanceValue = instance != null ? Convert.ToInt64(instance) : 0;
                if (isFlag)
                {
                    long resultInteger = 0;
                    //var result = new StringBuilder();
                    //bool first = true;
                    var enumValues = Enum.GetValues(storageType);
                    foreach (var value in enumValues)
                    {
                        long integralValue = Convert.ToInt64(value);
                        bool isSet = (instanceValue & integralValue) != 0;

                        if (isSet)
                        {
                            resultInteger |= integralValue;
                            //if (first == false) result.Append(",");
                            //first = false;
                            //result.Append(value.ToString());
                        }
                    }

                    //serialized = new Data(result.ToString());
                    serialized = new JsonObject(resultInteger);
                }
                else
                {
                    //serialized = new Data(Enum.GetName(storageType, instance));
                    serialized = new JsonObject(instanceValue);
                }
            }
            else
            {
                HashSet<string> valueNames = new HashSet<string>(instance.ToString().Split(new char[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);

                StringBuilder builder = new StringBuilder();

                var instanceType = instance.GetType();
                var enumValues = Enum.GetValues(storageType);
                foreach (var enumValue in enumValues)
                {
                    var memberName = enumValue.ToString();
                    
                    if (valueNames.Contains(memberName))
                    {
                        var field = instanceType.GetField(enumValue.ToString());
                        var attrs = field.GetCustomAttributes(typeof(SerializePropertyAttribute), true);
                        var attr = attrs != null && attrs.Length > 0 ? (SerializePropertyAttribute)attrs[0] : null;

                        var jsonName = attr != null && !string.IsNullOrEmpty(attr.Name) ? attr.Name : memberName;
                        if (builder.Length > 0)
                            builder.Append(",");
                        builder.Append(jsonName);
                    }
                }
                serialized = new JsonObject(builder.ToString());
            }
            return Result.Success;
        }

        public override Result TryDeserialize(JsonObject data, ref object instance, Type storageType) 
        {
            if (data.IsString) 
            {
                long instanceValue = 0;

                HashSet<string> valueNames = new HashSet<string>(data.AsString.Split(new[] { ',', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);

                var missingCount = valueNames.Count;
                var instanceType = instance.GetType();
                var enumValues = Enum.GetValues(storageType);
                foreach (var enumValue in enumValues)
                {
                    var memberName = enumValue.ToString();
                    var field = instanceType.GetField(enumValue.ToString());
                    var attrs = field.GetCustomAttributes(typeof(SerializePropertyAttribute), true);
                    var attr = attrs != null && attrs.Length > 0 ? (SerializePropertyAttribute)attrs[0] : null;

                    var jsonName = attr != null && !string.IsNullOrEmpty(attr.Name) ? attr.Name : memberName;
                    var fallbackNames = attr != null ? attr.FallbackNames : null;

                    if (valueNames.Contains(memberName) || 
                        valueNames.Contains(jsonName) || 
                        (fallbackNames != null && valueNames.Overlaps(fallbackNames)))
                    {
                        missingCount--;
                        long flagValue = (long)Convert.ChangeType(Enum.Parse(storageType, memberName, true), typeof(long));
                        instanceValue |= flagValue;
                    }
                }

                //A value was not found
                if (missingCount > 0)
                {
                    return Result.Fail("Cannot find enum name " + instance + " on type " + storageType);
                }

                instance = Enum.ToObject(storageType, (object)instanceValue);
                return Result.Success;
            }
            else if (data.IsInt64) 
            {
                int enumValue = (int)data.AsInt64;

                // In .NET compact, Enum.ToObject(Type, Object) is defined but the overloads like
                // Enum.ToObject(Type, int) are not -- so we get around this by boxing the value.
                instance = Enum.ToObject(storageType, (object)enumValue);

                return Result.Success;
            }

            return Result.Fail("EnumConverter encountered an unknown JSON data type");
        }

        /// <summary>
        /// Returns true if the given value is contained within the specified array.
        /// </summary>
        private static bool ArrayContains<T>(T[] values, T value) {
            // note: We don't use LINQ because this function will *not* allocate
            for (int i = 0; i < values.Length; ++i) {
                if (EqualityComparer<T>.Default.Equals(values[i], value)) {
                    return true;
                }
            }

            return false;
        }

        public enum bla { [SerializeProperty("cla")] a, b, c}


        public static Dictionary<object, string> GetValuePerDescription(Type storageType)
        {
            var map = new Dictionary<object, string>();
            Array values = null;
            try
            {
                values = Enum.GetValues(storageType);
            }
            catch { }

            if (values != null)
            {
                foreach (var value in values)
                {
                    var textValue = string.Empty;
                    try
                    {
                        var field = storageType.GetField(value.ToString());
                        if (field != null)
                        {
                            object[] atts = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
                            textValue = atts.Length > 0 && atts[0] is DescriptionAttribute ? ((DescriptionAttribute)atts[0]).Description : string.Empty;
                        }
                    }
                    catch { }
                    map[value] = textValue;
                }
            }
            return map;
        }
    }
}