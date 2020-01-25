using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Kyub.Serialization {

    /// <summary>
    /// Explicitly mark a property to be serialized. This can also be used to give the name that the
    /// property should use during serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SerializePropertyAttribute : Attribute {
        /// <summary>
        /// The name of that the property will use in JSON serialization.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Other names that can be used while deserializing
        /// </summary>
        public ReadOnlyCollection<string> FallbackNames { get; private set; }

        /// <summary>
        /// Method to check if CanSerialize in Context
        /// </summary>
        public string ValidatorFuncName { get; set; }

        public SerializePropertyAttribute()
            : this(string.Empty) {

            FallbackNames = new ReadOnlyCollection<string>(new List<string>());
        }

        public SerializePropertyAttribute(string name, params string[] fallbackNames) {
            Name = name != null? name.Trim() : "";
            var list = new List<string>();
            //Prevent Empty values or Same value as Name
            if (fallbackNames != null && fallbackNames.Length > 0)
            {
                foreach (var fallback in fallbackNames)
                {
                    var trimFallback = fallback != null ? fallback.Trim() : "";
                    if (!string.IsNullOrEmpty(trimFallback) && trimFallback != name && !list.Contains(trimFallback))
                    {
                        list.Add(trimFallback);
                    }
                }
            }

            FallbackNames = new ReadOnlyCollection<string>(list);
        }

        public MethodInfo FindSerializeInContextMethodInfo(Type contextType)
        {
            if (contextType != null && !string.IsNullOrEmpty(ValidatorFuncName))
            {
                var methodInfo = contextType.GetMethod(ValidatorFuncName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[0], null);
                if (methodInfo != null && methodInfo.ReturnType == typeof(bool))
                    return methodInfo;
            }

            return null;
        }
    }
}