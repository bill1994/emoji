using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kyub.Serialization {
    /// <summary>
    /// The actual type that a JsonData instance can store.
    /// </summary>
    public enum JsonObjectType {
        Array,
        Object,
        Double,
        Int64,
        Boolean,
        String,
        Null
    }

    /// <summary>
    /// A union type that stores a serialized value. The stored type can be one of six different
    /// types: null, boolean, double, Int64, string, Dictionary, or List.
    /// </summary>
    public sealed class JsonObject {
        /// <summary>
        /// The raw value that this serialized data stores. It can be one of six different types; a
        /// boolean, a double, Int64, a string, a Dictionary, or a List.
        /// </summary>
        private object _value;

        #region Constructors
        /// <summary>
        /// Creates a Data instance that holds null.
        /// </summary>
        public JsonObject() {
            _value = null;
        }

        /// <summary>
        /// Creates a Data instance that holds a boolean.
        /// </summary>
        public JsonObject(bool boolean) {
            _value = boolean;
        }

        /// <summary>
        /// Creates a Data instance that holds a double.
        /// </summary>
        public JsonObject(double f) {
            _value = f;
        }

        /// <summary>
        /// Creates a new Data instance that holds an integer.
        /// </summary>
        public JsonObject(Int64 i) {
            _value = i;
        }

        /// <summary>
        /// Creates a Data instance that holds a string.
        /// </summary>
        public JsonObject(string str) {
            _value = str;
        }

        /// <summary>
        /// Creates a Data instance that holds a dictionary of values.
        /// </summary>
        public JsonObject(Dictionary<string, JsonObject> dict) {
            _value = dict;
        }

        /// <summary>
        /// Creates a Data instance that holds a list of values.
        /// </summary>
        public JsonObject(List<JsonObject> list) {
            _value = list;
        }

        /// <summary>
        /// Helper method to create a Data instance that holds a dictionary.
        /// </summary>
        public static JsonObject CreateDictionary(SerializerConfig p_config) {
            if (p_config == null)
                p_config = SerializerConfig.DefaultConfig;

            return new JsonObject(new Dictionary<string, JsonObject>(
                p_config.IsCaseSensitive ? StringComparer.CurrentCulture : StringComparer.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Helper method to create a Data instance that holds a list.
        /// </summary>
        public static JsonObject CreateList() {
            return new JsonObject(new List<JsonObject>());
        }

        /// <summary>
        /// Helper method to create a Data instance that holds a list with the initial capacity.
        /// </summary>
        public static JsonObject CreateList(int capacity) {
            return new JsonObject(new List<JsonObject>(capacity));
        }

        public readonly static JsonObject True = new JsonObject(true);
        public readonly static JsonObject False = new JsonObject(true);
        public readonly static JsonObject Null = new JsonObject();
        #endregion

        #region Enumerator Functions

        public JsonObject this[int i]
        {
            get
            {
                if (this.IsList)
                {
                    var castedListData = this.AsList;
                    if (castedListData.Count > i && i >=0)
                        return castedListData[i];
                }

                return null;
            }
            set
            {
                if(this.IsList)
                    this.AsList[i] = value;
            }
        }

        public JsonObject this[string key]
        {
            get
            {
                if (this.IsDictionary)
                {
                    var castedDictData = this.AsDictionary;
                    if (castedDictData.ContainsKey(key))
                        return castedDictData[key];
                }

                return null;
            }
            set
            {
                if (this.IsDictionary)
                    this.AsDictionary[key] = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public int Count
        {
            get
            {
                if (this.IsList)
                    return this.AsList.Count;
                else if (this.IsDictionary)
                    return this.AsDictionary.Count;

                return -1;
            }
        }

        public IEnumerator<object> GetEnumerator()
        {
            if (this.IsList)
            {
                var list = this.AsList;
                for (int i = 0; i < list.Count; ++i)
                {
                    yield return list[i];
                }
            }
            else if (this.IsDictionary)
            {
                var dict = this.AsDictionary;
                foreach(var pair in dict)
                {
                    yield return pair;
                }
            }
        }

        #endregion

        #region Casting Predicates
        public JsonObjectType Type {
            get {
                if (_value == null) return JsonObjectType.Null;
                if (_value is double) return JsonObjectType.Double;
                if (_value is Int64) return JsonObjectType.Int64;
                if (_value is bool) return JsonObjectType.Boolean;
                if (_value is string) return JsonObjectType.String;
                if (_value is Dictionary<string, JsonObject>) return JsonObjectType.Object;
                if (_value is List<JsonObject>) return JsonObjectType.Array;

                throw new InvalidOperationException("unknown JSON data type");
            }
        }

        /// <summary>
        /// Returns true if this Data instance maps back to null.
        /// </summary>
        public bool IsNull {
            get {
                return _value == null;
            }
        }

        /// <summary>
        /// Returns true if this Data instance maps back to a double.
        /// </summary>
        public bool IsDouble {
            get {
                return _value is double;
            }
        }

        /// <summary>
        /// Returns true if this Data instance maps back to an Int64.
        /// </summary>
        public bool IsInt64 {
            get {
                return _value is Int64;
            }
        }

        /// <summary>
        /// Returns true if this Data instance maps back to a boolean.
        /// </summary>
        public bool IsBool {
            get {
                return _value is bool;
            }
        }

        /// <summary>
        /// Returns true if this Data instance maps back to a string.
        /// </summary>
        public bool IsString {
            get {
                return _value is string;
            }
        }

        /// <summary>
        /// Returns true if this Data instance maps back to a Dictionary.
        /// </summary>
        public bool IsDictionary {
            get {
                return _value is Dictionary<string, JsonObject>;
            }
        }

        /// <summary>
        /// Returns true if this Data instance maps back to a List.
        /// </summary>
        public bool IsList {
            get {
                return _value is List<JsonObject>;
            }
        }
        #endregion

        #region Casts
        /// <summary>
        /// Casts this Data to a double. Throws an exception if it is not a double.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public double AsDouble {
            get {
                return Cast<double>();
            }
        }

        /// <summary>
        /// Casts this Data to an Int64. Throws an exception if it is not an Int64.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Int64 AsInt64 {
            get {
                return Cast<Int64>();
            }
        }


        /// <summary>
        /// Casts this Data to a boolean. Throws an exception if it is not a boolean.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool AsBool {
            get {
                return Cast<bool>();
            }
        }

        /// <summary>
        /// Casts this Data to a string. Throws an exception if it is not a string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string AsString {
            get {
                return Cast<string>();
            }
        }

        /// <summary>
        /// Casts this Data to a Dictionary. Throws an exception if it is not a
        /// Dictionary.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Dictionary<string, JsonObject> AsDictionary {
            get {
                return Cast<Dictionary<string, JsonObject>>();
            }
        }

        /// <summary>
        /// Casts this Data to a List. Throws an exception if it is not a List.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public List<JsonObject> AsList {
            get {
                return Cast<List<JsonObject>>();
            }
        }

        /// <summary>
        /// Internal helper method to cast the underlying storage to the given type or throw a
        /// pretty printed exception on failure.
        /// </summary>
        private T Cast<T>() {
            if (_value is T) {
                return (T)_value;
            }

            throw new InvalidCastException("Unable to cast <" + this + "> (with type = " +
                _value.GetType() + ") to type " + typeof(T));
        }
        #endregion

        #region Public Helper Functions

        public bool HasKey(string key)
        {
            return !string.IsNullOrEmpty(key) && IsDictionary && AsDictionary != null && AsDictionary.ContainsKey(key);
        }

        public JsonObject GetValue(string key)
        {
            if (!string.IsNullOrEmpty(key) && IsDictionary && AsDictionary != null)
            {
                JsonObject value;
                AsDictionary.TryGetValue(key, out value);
                return value;
            }
            return null;
        }

        public JsonObject GetArrayElement(int index)
        {
            if (IsList && index >= 0 && index < Count)
            {
                return AsList[index];
            }
            return null;
        }

        #endregion

        #region Internal Helper Methods
        /// <summary>
        /// Transforms the internal JsonObject instance into a dictionary.
        /// </summary>
        internal void BecomeDictionary()
        {
            _value = new Dictionary<string, JsonObject>();
        }

        /// <summary>
        /// Returns a shallow clone of this data instance.
        /// </summary>
        internal JsonObject Clone()
        {
            var clone = new JsonObject();
            clone._value = _value;
                        return clone;

        }

        #endregion

        #region ToString Implementation
        public override string ToString() {
            return JsonPrinter.CompressedJson(this);
        }
        #endregion

        #region Equality Comparisons
        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj) {
            return Equals(obj as JsonObject);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public bool Equals(JsonObject other) {
            if (other == null || Type != other.Type) {
                return false;
            }

            switch (Type) {
                case JsonObjectType.Null:
                    return true;

                case JsonObjectType.Double:
                    return AsDouble == other.AsDouble || Math.Abs(AsDouble - other.AsDouble) < double.Epsilon;

                case JsonObjectType.Int64:
                    return AsInt64 == other.AsInt64;

                case JsonObjectType.Boolean:
                    return AsBool == other.AsBool;

                case JsonObjectType.String:
                    return AsString == other.AsString;

                case JsonObjectType.Array:
                    var thisList = AsList;
                    var otherList = other.AsList;

                    if (thisList.Count != otherList.Count) return false;

                    for (int i = 0; i < thisList.Count; ++i) {
                        if (thisList[i].Equals(otherList[i]) == false) {
                            return false;
                        }
                    }

                    return true;

                case JsonObjectType.Object:
                    var thisDict = AsDictionary;
                    var otherDict = other.AsDictionary;

                    if (thisDict.Count != otherDict.Count) return false;

                    foreach (string key in thisDict.Keys) {
                        if (otherDict.ContainsKey(key) == false) {
                            return false;
                        }

                        if (thisDict[key].Equals(otherDict[key]) == false) {
                            return false;
                        }
                    }

                    return true;
            }

            throw new Exception("Unknown data type");
        }

        /// <summary>
        /// Returns true iff a == b.
        /// </summary>
        public static bool operator ==(JsonObject a, JsonObject b) {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(a, b)) {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null)) {
                return false;
            }

            if (a.IsDouble && b.IsDouble) {
                return Math.Abs(a.AsDouble - b.AsDouble) < double.Epsilon;
            }

            return a.Equals(b);
        }

        /// <summary>
        /// Returns true iff a != b.
        /// </summary>
        public static bool operator !=(JsonObject a, JsonObject b) {
            return !(a == b);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data
        /// structures like a hash table.</returns>
        public override int GetHashCode() {
            return _value.GetHashCode();
        }
        #endregion
    }

}