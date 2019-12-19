using System;
using System.Collections.Generic;
using UnityEngine;
using Kyub.Serialization.VWF.Extensions;
using Kyub.Serialization.VWF.Helpers;
using Kyub.Reflection;
using System.Reflection;

namespace Kyub.Serialization.VWF.Types
{
    /// <summary>
    /// A wrapper for metadata members (fields and properties)
    /// that's used to conveneitly set/get the member value on a certain target.
    /// The way setting/getting of members is done If you're in-editor or targetting standalone
    /// is via dynamically generated delegates (which is pretty fast)
    /// otherwise via standard reflection (slower)
    /// </summary>
    public class RuntimeMember
    {
        private Action<object, object> _setter;
        private Func<object, object> _getter;
        public object Target;

        /// <summary>
        /// The name of the wrapped member
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// If the member name was "_someValue" or "someValue" then the nice name would be "Some Value"
        /// </summary>
        public readonly string NiceName;

        /// <summary>
        /// Say the member was a dictionary of float and string, the type nice name would be Dictionary<float, string>
        /// instead of System.Collections.Generic.Dictionary<Sys.. ahhh to hell with this!>
        /// </summary>
        public readonly string TypeNiceName;

        /// <summary>
        /// The type of the wrapped member (System.Reflection.FieldInfo.FieldType in case of a field, or System.Reflection.PropertyInfo.PropertyType in case of a property)
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// A reference to the MemberInfo reference of the wrapped member
        /// </summary>
        public readonly System.Reflection.MemberInfo Info;

        /// <summary>
        /// The current value of the member in the current target object
        /// </summary>
        public object Value
        {
            get
            {
                return _getter(Target);
            }
            set
            {
                try
                {
                    _setter(Target, value);
                }
                catch(InvalidCastException)
                {
                    if (value != null)
                    {
                        string message = "[InvalidCast] Cannot cast from `" + value + "` to `" + TypeNiceName + "`";
                        Debug.Log(message);
                    }
                }
            }
        }

        private RuntimeMember(System.Reflection.MemberInfo memberInfo, Type memberType, object memberTarget)
        {
            Info = memberInfo;
            Type = memberType;
            Target = memberTarget;
            Name = memberInfo.Name;
            NiceName = Name.Replace("_", "").SplitPascalCase();
            TypeNiceName = memberType.GetNiceName();
        }

        /// <summary>
        /// Returns false if the field was constant (literal) while setting 'result' to null.
        /// Otherwise true while setting result to a new RuntimeMember wrapping the specified field
        /// using the appropriate method of building the [s|g]etters (delegates in case of editor/standalone, reflection otherwise)
        /// </summary>
        public static bool TryWrapField(System.Reflection.FieldInfo field, object target, out RuntimeMember result)
        {
            if (field.IsLiteral)
            { 
                result = null;
                return false;
            }

            result = new RuntimeMember(field, field.FieldType, target);
            result._setter = field.SetValue;
            result._getter = field.GetValue;
            return true;
        }

        /// <summary>
        /// Returns false if the property isn't readable or if it's an indexer, setting 'result' to null in the process.
        /// Otherwise true while setting result to a new RuntimeMember wrapping the specified property
        /// using the appropriate method of building the [s|g]etters (delegates in case of editor/standalone, reflection otherwise)
        /// Note that readonly properties (getter only) are fine, as the setter will just be an empty delegate doing nothing.
        /// </summary>
        public static bool TryWrapProperty(System.Reflection.PropertyInfo property, object target, out RuntimeMember result)
        {
            if (!property.CanRead || property.IsIndexer())
            {
                result = null;
                return false;
            }

            result = new RuntimeMember(property, property.PropertyType, target);

            if (property.CanWrite)
            {
                result._setter = (x, y) => property.SetValue(x, y, null);
            }
            else result._setter = (x, y) => { };
            result._getter = x => property.GetValue(x, null);
            return true;
        }

        /// <summary>
        /// Returns a list of RuntimeMember wrapping whatever is valid from the input members IEnumerable in the specified target
        /// </summary>
        public static List<RuntimeMember> WrapMembers(IEnumerable<System.Reflection.MemberInfo> members, object target)
        {
            var result = new List<RuntimeMember>();
            foreach (var member in members)
                result.AddIfNotNull(WrapMember(member, target));
            return result;
        }

        /// <summary>
        /// Tries to wrap the specified member.
        /// Returns the wrapped result if it succeeds (valid field/property)
        /// otherwise null
        /// </summary>
        public static RuntimeMember WrapMember(System.Reflection.MemberInfo member, object target)
        {
            var field = member as System.Reflection.FieldInfo;
            if (field != null)
            {
                RuntimeMember wrappedField;
                if (RuntimeMember.TryWrapField(field, target, out wrappedField))
                    return wrappedField;
            }
            else
            {
                var property = member as System.Reflection.PropertyInfo;
                if (property == null)
                    return null;

                RuntimeMember wrappedProperty;
                if (RuntimeMember.TryWrapProperty(property, target, out wrappedProperty))
                    return wrappedProperty;
            }

            return null;
        }

        public override string ToString()
        {
            return TypeNiceName + " " + Name;
        }

        public override int GetHashCode()
        {
            return Info.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var member = obj as RuntimeMember;
            return member != null && this.Info == member.Info;
        }
    }
}
