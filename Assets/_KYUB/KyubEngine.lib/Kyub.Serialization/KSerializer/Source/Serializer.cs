#if (UNITY_WINRT || UNITY_WP_8_1) && !UNITY_EDITOR && !UNITY_WP8
#define RT_ENABLED
#endif

using System;
using System.Collections.Generic;
using Kyub.Serialization.Internal;
using System.Reflection;

namespace Kyub.Serialization {
    public class Serializer
    {

        #region Keys
        private static HashSet<string> _reservedKeywords;
        static Serializer()
        {
            _reservedKeywords = new HashSet<string> {
                Key_ObjectReference,
                Key_ObjectDefinition,
                Key_InstanceType,
                Key_Version,
                Key_Content
            };
        }
        /// <summary>
        /// Returns true if the given key is a special keyword that full serializer uses to
        /// add additional metadata on top of the emitted JSON.
        /// </summary>
        public static bool IsReservedKeyword(string key)
        {
            return _reservedKeywords.Contains(key);
        }

        /// <summary>
        /// This is an object reference in part of a cyclic graph.
        /// </summary>
        internal const string Key_ObjectReference = "$ref";

        /// <summary>
        /// This is an object definition, as part of a cyclic graph.
        /// </summary>
        internal const string Key_ObjectDefinition = "$id";

        /// <summary>
        /// This specifies the actual type of an object (the instance type was different from
        /// the field type).
        /// </summary>
        internal const string Key_InstanceType = "$type";

        /// <summary>
        /// The version string for the serialized data.
        /// </summary>
        internal const string Key_Version = "$version";

        /// <summary>
        /// If we have to add metadata but the original serialized state was not a dictionary,
        /// then this will contain the original data.
        /// </summary>
        internal const string Key_Content = "$content";

        private static bool IsObjectReference(JsonObject data)
        {
            if (data.IsDictionary == false) return false;
            return data.AsDictionary.ContainsKey(Key_ObjectReference);
        }
        private static bool IsObjectDefinition(JsonObject data)
        {
            if (data.IsDictionary == false) return false;
            return data.AsDictionary.ContainsKey(Key_ObjectDefinition);
        }
        private static bool IsVersioned(JsonObject data)
        {
            if (data.IsDictionary == false) return false;
            return data.AsDictionary.ContainsKey(Key_Version);
        }
        private static bool IsTypeSpecified(JsonObject data)
        {
            if (data.IsDictionary == false) return false;
            return data.AsDictionary.ContainsKey(Key_InstanceType);
        }
        private static bool IsWrappedData(JsonObject data)
        {
            if (data.IsDictionary == false) return false;
            return data.AsDictionary.ContainsKey(Key_Content);
        }

        /// <summary>
        /// Strips all deserialization metadata from the object, like $type and $content fields.
        /// </summary>
        /// <remarks>After making this call, you will *not* be able to deserialize the same object instance. The metadata is
        /// strictly necessary for deserialization!</remarks>
        public static void StripDeserializationMetadata(ref JsonObject data)
        {
            if (data.IsDictionary && data.AsDictionary.ContainsKey(Key_Content))
            {
                data = data.AsDictionary[Key_Content];
            }

            if (data.IsDictionary)
            {
                var dict = data.AsDictionary;
                dict.Remove(Key_ObjectReference);
                dict.Remove(Key_ObjectDefinition);
                dict.Remove(Key_InstanceType);
                dict.Remove(Key_Version);
            }
        }

        /// <summary>
        /// This function converts legacy serialization data into the new format, so that
        /// the import process can be unified and ignore the old format.
        /// </summary>
        private void ConvertLegacyData(ref JsonObject data)
        {
            if (data.IsDictionary == false) return;

            var dict = data.AsDictionary;

            // fast-exit: metadata never had more than two items
            if (dict.Count > 2) return;

            // Key strings used in the legacy system
            string referenceIdString = "ReferenceId";
            string sourceIdString = "SourceId";
            string sourceDataString = "JsonObject";
            string typeString = "Type";
            string typeDataString = "JsonObject";

            // type specifier
            if (dict.Count == 2 && dict.ContainsKey(typeString) && dict.ContainsKey(typeDataString))
            {
                data = dict[typeDataString];
                EnsureDictionary(data);
                ConvertLegacyData(ref data);

                data.AsDictionary[Key_InstanceType] = dict[typeString];
            }

            // object definition
            else if (dict.Count == 2 && dict.ContainsKey(sourceIdString) && dict.ContainsKey(sourceDataString))
            {
                data = dict[sourceDataString];
                EnsureDictionary(data);
                ConvertLegacyData(ref data);

                data.AsDictionary[Key_ObjectDefinition] = dict[sourceIdString];
            }

            // object reference
            else if (dict.Count == 1 && dict.ContainsKey(referenceIdString))
            {
                data = JsonObject.CreateDictionary(Config);
                data.AsDictionary[Key_ObjectReference] = dict[referenceIdString];
            }
        }
        #endregion

        #region Utility Methods

        private static void Invoke_DefaultCallback<T>(List<System.Reflection.MethodInfo> defaultCallbackMethods, object instance) where T : Attribute
        {
            foreach (System.Reflection.MethodInfo methodInfo in defaultCallbackMethods)
            {
                if (PortableReflection.GetAttribute<T>(methodInfo) != null)
                {
                    try
                    {
                        var parameters = methodInfo.GetParameters();
                        if (parameters.Length == 0)
                        {
                            methodInfo.Invoke(instance, null);
                        }
                        //SerializationContext will be null
                        else if (parameters.Length == 1)
                        {
                            methodInfo.Invoke(instance, new object[] { null });
                        }
                    }
                    catch { }
                    break;
                }
            }
        }

        private static void Invoke_OnBeforeSerialize(List<System.Reflection.MethodInfo> defaultCallbackMethods, List<ObjectProcessor> processors, Type storageType, object instance)
        {
            for (int i = 0; i < processors.Count; ++i)
            {
                processors[i].OnBeforeSerialize(storageType, instance);
            }
            Invoke_DefaultCallback<System.Runtime.Serialization.OnSerializingAttribute>(defaultCallbackMethods, instance);
            Invoke_DefaultCallback<OnBeginSerializeAttribute>(defaultCallbackMethods, instance);
        }
        private static void Invoke_OnAfterSerialize(List<System.Reflection.MethodInfo> defaultCallbackMethods, List<ObjectProcessor> processors, Type storageType, object instance, ref JsonObject data)
        {
            // We run the after calls in reverse order; this significantly reduces the interaction burden between
            // multiple processors - it makes each one much more independent and ignorant of the other ones.

            for (int i = processors.Count - 1; i >= 0; --i)
            {
                processors[i].OnAfterSerialize(storageType, instance, ref data);
            }
            Invoke_DefaultCallback<System.Runtime.Serialization.OnSerializedAttribute>(defaultCallbackMethods, instance);
            Invoke_DefaultCallback<OnEndSerializeAttribute>(defaultCallbackMethods, instance);
        }
        private static void Invoke_OnBeforeDeserialize(List<ObjectProcessor> processors, Type storageType, ref JsonObject data)
        {
            for (int i = 0; i < processors.Count; ++i)
            {
                processors[i].OnBeforeDeserialize(storageType, ref data);
            }
        }
        private static void Invoke_OnBeforeDeserializeAfterInstanceCreation(List<System.Reflection.MethodInfo> defaultCallbackMethods, List<ObjectProcessor> processors, Type storageType, object instance, ref JsonObject data)
        {
            for (int i = 0; i < processors.Count; ++i)
            {
                processors[i].OnBeforeDeserializeAfterInstanceCreation(storageType, instance, ref data);
            }
            Invoke_DefaultCallback<System.Runtime.Serialization.OnDeserializingAttribute>(defaultCallbackMethods, instance);
            Invoke_DefaultCallback<OnBeginDeserializeAttribute>(defaultCallbackMethods, instance);
        }
        private static void Invoke_OnAfterDeserialize(List<System.Reflection.MethodInfo> defaultCallbackMethods, List<ObjectProcessor> processors, Type storageType, object instance)
        {

            for (int i = processors.Count - 1; i >= 0; --i)
            {
                processors[i].OnAfterDeserialize(storageType, instance);
            }
            Invoke_DefaultCallback<System.Runtime.Serialization.OnDeserializedAttribute>(defaultCallbackMethods, instance);
            Invoke_DefaultCallback<OnEndDeserializeAttribute>(defaultCallbackMethods, instance);
        }
        #endregion

        /// <summary>
        /// Ensures that the data is a dictionary. If it is not, then it is wrapped inside of one.
        /// </summary>
        private static void EnsureDictionary(JsonObject data)
        {
            if (data.IsDictionary == false)
            {
                var existingData = data.Clone();
                data.BecomeDictionary();
                data.AsDictionary[Key_Content] = existingData;

                //var dict = JsonObject.CreateDictionary(Config);
                //dict.AsDictionary[Key_Content] = data;
                //data = dict;
            }
        }

        /// <summary>
        /// This manages instance writing so that we do not write unnecessary $id fields. We
        /// only need to write out an $id field when there is a corresponding $ref field. This is able
        /// to write $id references lazily because the JsonObject instance is not actually written out to text
        /// until we have entirely finished serializing it.
        /// </summary>
        internal class LazyCycleDefinitionWriter
        {
            //private Dictionary<int, Dictionary<string, JsonObject>> _definitions = new Dictionary<int, Dictionary<string, JsonObject>>();
            private Dictionary<int, JsonObject> _pendingDefinitions = new Dictionary<int, JsonObject>();
            private HashSet<int> _references = new HashSet<int>();

            /*public void WriteDefinition(int id, Dictionary<string, JsonObject> dict)
            {
                if (_references.Contains(id))
                {
                    dict[Key_ObjectDefinition] = new JsonObject(id.ToString());
                }

                else
                {
                    _definitions[id] = dict;
                }
            }*/

            public void WriteDefinition(int id, JsonObject data)
            {
                if (_references.Contains(id))
                {
                    EnsureDictionary(data);
                    data.AsDictionary[Key_ObjectDefinition] = new JsonObject(id.ToString());
                }

                else
                {
                    _pendingDefinitions[id] = data;
                }
            }

            public void WriteReference(int id, Dictionary<string, JsonObject> dict)
            {
                // Write the actual definition if necessary
                //if (_definitions.ContainsKey(id))
                //{
                //    _definitions[id][Key_ObjectDefinition] = new JsonObject(id.ToString());
                //    _definitions.Remove(id);
                //}
                if (_pendingDefinitions.ContainsKey(id))
                {
                    var data = _pendingDefinitions[id];
                    EnsureDictionary(data);
                    data.AsDictionary[Key_ObjectDefinition] = new JsonObject(id.ToString());
                    _pendingDefinitions.Remove(id);
                }
                else
                {
                    _references.Add(id);
                }

                // Write the reference
                dict[Key_ObjectReference] = new JsonObject(id.ToString());
            }

            public void Clear()
            {
                _pendingDefinitions.Clear();
                //_definitions.Clear();
            }
        }

        /// <summary>
        /// A cache from type to it's converter.
        /// </summary>
        private Dictionary<Type, BaseConverter> _cachedConverters;

        /// <summary>
        /// A cache from type to the set of processors that are interested in it.
        /// </summary>
        private Dictionary<Type, List<ObjectProcessor>> _cachedProcessors;

        /// <summary>
        /// A cache from type to the set of processors that are interested in it.
        /// </summary>
        private Dictionary<Type, List<System.Reflection.MethodInfo>> _cachedDefaultSerializationCallbacks;

        /// <summary>
        /// Converters that can be used for type registration.
        /// </summary>
        private readonly List<Converter> _availableConverters;

        /// <summary>
        /// Direct converters (optimized _converters). We use these so we don't have to
        /// perform a scan through every item in _converters and can instead just do an O(1)
        /// lookup. This is potentially important to perf when there are a ton of direct
        /// converters.
        /// </summary>
        private readonly Dictionary<Type, DirectConverter> _availableDirectConverters;

        /// <summary>
        /// Processors that are available.
        /// </summary>
        private readonly List<ObjectProcessor> _processors;

        /// <summary>
        /// Reference manager for cycle detection.
        /// </summary>
        private readonly CyclicReferenceManager _references;
        public int CurrentDepth
        {
            get
            {
                return _references.Depth;
            }
        }

        private readonly LazyCycleDefinitionWriter _lazyReferenceWriter;

        private readonly SerializerConfig _config = null;
        public SerializerConfig Config
        {
            get
            {
                return _config;
            }
        }

        List<MetaProperty> _metaProperties = new List<MetaProperty>();
        public MetaProperty CurrentMetaProperty
        {
            get
            {
                if (_metaProperties == null)
                    _metaProperties = new List<MetaProperty>();
                return _metaProperties.Count > 0? _metaProperties[_metaProperties.Count -1] : null;
            }
        }

        public Serializer(Serializer serializer)
        {
            //Clone Converters
            
            _config = serializer == null? SerializerConfig.DefaultConfig : serializer.Config;
            _cachedConverters = new Dictionary<Type, BaseConverter>();
            _cachedProcessors = new Dictionary<Type, List<ObjectProcessor>>();
            _cachedDefaultSerializationCallbacks = new Dictionary<Type, List<System.Reflection.MethodInfo>>();

            _references = new CyclicReferenceManager();
            _lazyReferenceWriter = new LazyCycleDefinitionWriter();

            _processors = new List<ObjectProcessor>() {
                new SerializationCallbackProcessor()
            };

            //Clone Conversors
            if (serializer != null)
            {
                Context = serializer.Context;
                _availableDirectConverters = new Dictionary<Type, DirectConverter>();
                foreach (var pair in serializer._availableDirectConverters)
                {
                    if (pair.Value != null)
                    {
                        var clonedConverter = Activator.CreateInstance(pair.Value.GetType()) as DirectConverter;
                        if (clonedConverter != null)
                        {
                            clonedConverter.Serializer = this;
                            _availableDirectConverters[pair.Key] = clonedConverter;
                        }
                    }
                }

                _availableConverters = new List<Converter>();
                foreach (var converter in serializer._availableConverters)
                {
                    if (converter != null)
                    {
                        var type = converter.GetType();
                        var clonedConverter = Activator.CreateInstance(type) as Converter;
                        if (clonedConverter != null)
                        {
                            clonedConverter.Serializer = this;
                            _availableConverters.Add(clonedConverter);
                        }
                    }
                }
            }
            else
            {
                // note: The order here is important. Items at the beginning of this
                //       list will be used before converters at the end. Converters
                //       added via AddConverter() are added to the front of the list.
                _availableConverters = new List<Converter> {
                new NullableConverter { Serializer = this },
                new GuidConverter { Serializer = this },
                new TypeConverter { Serializer = this },
                new DateConverter { Serializer = this },
                new EnumConverter { Serializer = this },
                new PrimitiveConverter { Serializer = this },
                new ArrayConverter { Serializer = this },
                new DictionaryConverter { Serializer = this },
                new IEnumerableConverter { Serializer = this },
                new KeyValuePairConverter { Serializer = this },
                new JsonObjectConverter  {Serializer = this },
                new WeakReferenceConverter { Serializer = this },
                new ReflectedConverter { Serializer = this },
                };
                _availableDirectConverters = new Dictionary<Type, DirectConverter>();

                // Register the converters from the register
                foreach (var converterType in ConverterRegister.Converters)
                {
                    AddConverter((BaseConverter)Activator.CreateInstance(converterType));
                }
            }

            if(Context == null)
                Context = new Context();
        }

        public Serializer(SerializerConfig config = null) {
            _config = config == null? SerializerConfig.DefaultConfig : config;
            _cachedConverters = new Dictionary<Type, BaseConverter>();
            _cachedProcessors = new Dictionary<Type, List<ObjectProcessor>>();
            _cachedDefaultSerializationCallbacks = new Dictionary<Type, List<System.Reflection.MethodInfo>>();

            _references = new CyclicReferenceManager();
            _lazyReferenceWriter = new LazyCycleDefinitionWriter();

            // note: The order here is important. Items at the beginning of this
            //       list will be used before converters at the end. Converters
            //       added via AddConverter() are added to the front of the list.
            _availableConverters = new List<Converter> {
                new NullableConverter { Serializer = this },
                new GuidConverter { Serializer = this },
                new TypeConverter { Serializer = this },
                new DateConverter { Serializer = this },
                new EnumConverter { Serializer = this },
                new PrimitiveConverter { Serializer = this },
                new ArrayConverter { Serializer = this },
                new DictionaryConverter { Serializer = this },
                new IEnumerableConverter { Serializer = this },
                new KeyValuePairConverter { Serializer = this },
                new JsonObjectConverter  {Serializer = this },
                new WeakReferenceConverter { Serializer = this },
                new ReflectedConverter { Serializer = this },
            };
            _availableDirectConverters = new Dictionary<Type, DirectConverter>();

            _processors = new List<ObjectProcessor>() {
                new SerializationCallbackProcessor()
            };

            Context = new Context();

            // Register the converters from the register
            foreach (var converterType in ConverterRegister.Converters) {
                AddConverter((BaseConverter)Activator.CreateInstance(converterType));
            }
        }

        /// <summary>
        /// A context object that Converters can use to customize how they operate.
        /// </summary>
        public Context Context;

        /// <summary>
        /// Add a new processor to the serializer. Multiple processors can run at the same time in the
        /// same order they were added in.
        /// </summary>
        /// <param name="processor">The processor to add.</param>
        public void AddProcessor(ObjectProcessor processor) {
            _processors.Add(processor);

            // We need to reset our cached processor set, as it could be invalid with the new
            // processor. Ideally, _cachedProcessors should be empty (as the user should fully setup
            // the serializer before actually using it), but there is no guarantee.
            _cachedProcessors = new Dictionary<Type, List<ObjectProcessor>>();
        }

        private List<System.Reflection.MethodInfo> GetDefaultSerializationCallbackMethods(Type type)
        {
            List<System.Reflection.MethodInfo>  returnList;
            if (!_cachedDefaultSerializationCallbacks.TryGetValue(type, out returnList))
            {
                List<System.Reflection.MethodInfo> methodList = new List<System.Reflection.MethodInfo>();
                System.Type processingType = type;
                while (processingType != null)
                {
                    methodList.AddRange(processingType.GetDeclaredMethods());
#if RT_ENABLED
                    processingType = processingType.GetTypeInfo().BaseType;
#else
                    processingType = processingType.BaseType;
#endif
                }
                returnList = new List<System.Reflection.MethodInfo>();
                foreach (System.Reflection.MethodInfo methodInfo in methodList)
                {
                    if ((PortableReflection.GetAttribute<System.Runtime.Serialization.OnDeserializedAttribute>(methodInfo) != null) ||
                        (PortableReflection.GetAttribute<System.Runtime.Serialization.OnDeserializingAttribute>(methodInfo) != null) ||
                        (PortableReflection.GetAttribute<System.Runtime.Serialization.OnSerializedAttribute>(methodInfo) != null) ||
                        (PortableReflection.GetAttribute<System.Runtime.Serialization.OnSerializingAttribute>(methodInfo) != null) ||
                        (PortableReflection.GetAttribute<OnBeginDeserializeAttribute>(methodInfo) != null) ||
                        (PortableReflection.GetAttribute<OnEndDeserializeAttribute>(methodInfo) != null) ||
                        (PortableReflection.GetAttribute<OnBeginSerializeAttribute>(methodInfo) != null) ||
                        (PortableReflection.GetAttribute<OnEndDeserializeAttribute>(methodInfo) != null))
                    {
                        returnList.Add(methodInfo);
                    }
                }
                _cachedDefaultSerializationCallbacks[type] = returnList;
            }
            return returnList;
        }

        /// <summary>
        /// Fetches all of the processors for the given type.
        /// </summary>
        private List<ObjectProcessor> GetProcessors(Type type) {
            List<ObjectProcessor> processors;

            // Check to see if the user has defined a custom processor for the type. If they
            // have, then we don't need to scan through all of the processor to check which
            // one can process the type; instead, we directly use the specified processor.
            var attr = PortableReflection.GetAttribute<SerializeObjectAttribute>(type);
            if (attr != null && attr.Processor != null) {
                var processor = (ObjectProcessor)Activator.CreateInstance(attr.Processor);
                processors = new List<ObjectProcessor>();
                processors.Add(processor);
                _cachedProcessors[type] = processors;
            }

            else if (_cachedProcessors.TryGetValue(type, out processors) == false) {
                processors = new List<ObjectProcessor>();

                for (int i = 0; i < _processors.Count; ++i) {
                    var processor = _processors[i];
                    if (processor.CanProcess(type)) {
                        processors.Add(processor);
                    }
                }

                _cachedProcessors[type] = processors;
            }

            return processors;
        }


        /// <summary>
        /// Adds a new converter that can be used to customize how an object is serialized and
        /// deserialized.
        /// </summary>
        public void AddConverter(BaseConverter converter) {
            if (converter.Serializer != null) {
                throw new InvalidOperationException("Cannot add a single converter instance to " +
                    "multiple Converters -- please construct a new instance for " + converter);
            }

            // TODO: wrap inside of a ConverterManager so we can control _converters and _cachedConverters lifetime
            if (converter is DirectConverter) {
                var directConverter = (DirectConverter)converter;
                _availableDirectConverters[directConverter.ModelType] = directConverter;
            }
            else if (converter is Converter) {
                _availableConverters.Insert(0, (Converter)converter);
            }
            else {
                throw new InvalidOperationException("Unable to add converter " + converter +
                    "; the type association strategy is unknown. Please use either " +
                    "DirectConverter or Converter as your base type.");
            }

            converter.Serializer = this;

            // We need to reset our cached converter set, as it could be invalid with the new
            // converter. Ideally, _cachedConverters should be empty (as the user should fully setup
            // the serializer before actually using it), but there is no guarantee.
            _cachedConverters = new Dictionary<Type, BaseConverter>();
        }

        /// <summary>
        /// Fetches a converter that can serialize/deserialize the given type.
        /// </summary>
        public BaseConverter GetConverter(Type type, MetaProperty property = null) {

            if (property != null && property.ConverterInstance != null)
                return property.ConverterInstance;

            BaseConverter converter;
            // Check to see if the user has defined a custom converter for the type. If they
            // have, then we don't need to scan through all of the converters to check which
            // one can process the type; instead, we directly use the specified converter.
            var attr = PortableReflection.GetAttribute<SerializeObjectAttribute>(type);
            if (attr != null && attr.Converter != null) {
                converter = (BaseConverter)Activator.CreateInstance(attr.Converter);
                converter.Serializer = this;
                _cachedConverters[type] = converter;
            }

            // There is no specific converter specified; try all of the general ones to see
            // which ones matches.
            else {
                if (_cachedConverters.TryGetValue(type, out converter) == false) {
                    if (_availableDirectConverters.ContainsKey(type)) {
                        converter = _availableDirectConverters[type];
                        _cachedConverters[type] = converter;
                    }
                    else {
                        for (int i = 0; i < _availableConverters.Count; ++i) {
                            if (_availableConverters[i].CanProcess(type)) {
                                converter = _availableConverters[i];
                                _cachedConverters[type] = converter;
                                break;
                            }
                        }
                    }
                }
            }

            if (converter == null) {
                throw new InvalidOperationException("Internal error -- could not find a converter for " + type);
            }
            return converter;
        }

        /// <summary>
        /// Helper method that simply forwards the call to TrySerialize(typeof(T), instance, out data);
        /// </summary>
        public Result TrySerialize<T>(T instance, out JsonObject data, MetaProperty property = null) {
            return TrySerialize(typeof(T), instance, out data, property);
        }

        /// <summary>
        /// Serialize the given value.
        /// </summary>
        /// <param name="storageType">The type of field/property that stores the object instance. This is
        /// important particularly for inheritance, as a field storing an IInterface instance
        /// should have type information included.</param>
        /// <param name="instance">The actual object instance to serialize.</param>
        /// <param name="data">The serialized state of the object.</param>
        /// <returns>If serialization was successful.</returns>
        public Result TrySerialize(Type storageType, object instance, out JsonObject data, MetaProperty property = null)
        {
            var processors = GetProcessors(storageType);
            var defaultCallbacks = GetDefaultSerializationCallbackMethods(instance != null? instance.GetType() : storageType);
            Invoke_OnBeforeSerialize(defaultCallbacks, processors, storageType, instance);

            // We always serialize null directly as null
            if (ReferenceEquals(instance, null)) {
                data = new JsonObject();
                Invoke_OnAfterSerialize(defaultCallbacks, processors, storageType, instance, ref data);
                return Result.Success;
            }

            var result = InternalSerialize_1_ProcessCycles(storageType, instance, out data, property);
            Invoke_OnAfterSerialize(defaultCallbacks, processors, storageType, instance, ref data);
            return result;
        }

        private Result InternalSerialize_1_ProcessCycles(Type storageType, object instance, out JsonObject data, MetaProperty property) {
            // We have an object definition to serialize.
            try {
                // Note that we enter the reference group at the beginning of serialization so that we support
                // references that are at equal serialization levels, not just nested serialization levels, within
                // the given subobject. A prime example is serialization a list of references.
                _references.Enter();
                if (_metaProperties != null)
                    _metaProperties.Add(property);

                // This type does not need cycle support.
                if (GetConverter(instance.GetType(), property).RequestCycleSupport(instance.GetType()) == false) {
                    return InternalSerialize_2_Inheritance(storageType, instance, out data, property);
                }

                // We've already serialized this object instance (or it is pending higher up on the call stack).
                // Just serialize a reference to it to escape the cycle.
                // 
                // note: We serialize the int as a string to so that we don't lose any information
                //       in a conversion to/from double.
                if (_references.IsReference(instance)) {
                    data = JsonObject.CreateDictionary(Config);
                    _lazyReferenceWriter.WriteReference(_references.GetReferenceId(instance), data.AsDictionary);
                    return Result.Success;
                }

                // Mark inside the object graph that we've serialized the instance. We do this *before*
                // serialization so that if we get back into this function recursively, it'll already
                // be marked and we can handle the cycle properly without going into an infinite loop.
                _references.MarkSerialized(instance);

                // We've created the cycle metadata, so we can now serialize the actual object.
                // InternalSerialize will handle inheritance correctly for us.
                var result = InternalSerialize_2_Inheritance(storageType, instance, out data, property);
                if (result.Failed) return result;

                EnsureDictionary(data);
                _lazyReferenceWriter.WriteDefinition(_references.GetReferenceId(instance), data);

                return result;
            }
            finally {
                if (_references.Exit()) {
                    _lazyReferenceWriter.Clear();
                }
                if (_metaProperties != null && _metaProperties.Count > 0)
                    _metaProperties.RemoveAt(_metaProperties.Count - 1);
            }
        }
        private Result InternalSerialize_2_Inheritance(Type storageType, object instance, out JsonObject data, MetaProperty property) {
            // Serialize the actual object with the field type being the same as the object
            // type so that we won't go into an infinite loop.
            var serializeResult = InternalSerialize_3_ProcessVersioning(instance, out data, property);
            if (serializeResult.Failed) return serializeResult;

            // Do we need to add type information? If the field type and the instance type are different
            // then we will not be able to recover the correct instance type from the field type when
            // we deserialize the object.
            //
            // Note: We allow converters to request that we do *not* add type information.
            if (Config.TypeWriterOption != SerializerConfig.TypeWriterEnum.Never)
            {
                if ((Config.TypeWriterOption == SerializerConfig.TypeWriterEnum.Always) || 
                    (storageType != instance.GetType() && GetConverter(storageType, property).RequestInheritanceSupport(storageType)))
                {
                    System.Type instanceType = GetSerializationType(instance);
                    EnsureDictionary(data);
                    if (instanceType != null)
                    {
                        // Add the inheritance metadata
                        data.AsDictionary[Key_InstanceType] = new JsonObject(RemoveAssemblyDetails(instanceType.AssemblyQualifiedName));
                    }
                }
            }

            return serializeResult;
        }

        private Result InternalSerialize_3_ProcessVersioning(object instance, out JsonObject data, MetaProperty property)
        {
            // note: We do not have to take a Type parameter here, since at this point in the serialization
            //       algorithm inheritance has *always* been handled. If we took a type parameter, it will
            //       *always* be equal to instance.GetType(), so why bother taking the parameter?

            // Check to see if there is versioning information for this type. If so, then we need to serialize it.
            Option<VersionedType> optionalVersionedType = VersionManager.GetVersionedType(instance.GetType());
            if (optionalVersionedType.HasValue)
            {
                VersionedType versionedType = optionalVersionedType.Value;

                // Serialize the actual object content; we'll just wrap it with versioning metadata here.
                var result = InternalSerialize_4_Converter(instance, out data, property);
                if (result.Failed) return result;

                // Add the versioning information
                EnsureDictionary(data);
                data.AsDictionary[Key_Version] = new JsonObject(versionedType.VersionString);

                return result;
            }

            // This type has no versioning information -- directly serialize it using the selected converter.
            return InternalSerialize_4_Converter(instance, out data, property);
        }

        private Result InternalSerialize_4_Converter(object instance, out JsonObject data, MetaProperty property)
        {
            var instanceType = instance.GetType();
            return GetConverter(instanceType, property).TrySerialize(instance, out data, instanceType);
        }

        private static string RemoveAssemblyDetails(string fullyQualifiedTypeName)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            // loop through the type name and filter out qualified assembly details from nested type names
            bool writingAssemblyName = false;
            bool skippingAssemblyDetails = false;
            for (int i = 0; i < fullyQualifiedTypeName.Length; i++)
            {
                char current = fullyQualifiedTypeName[i];
                switch (current)
                {
                    case '[':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        builder.Append(current);
                        break;
                    case ']':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        builder.Append(current);
                        break;
                    case ',':
                        if (!writingAssemblyName)
                        {
                            writingAssemblyName = true;
                            builder.Append(current);
                        }
                        else
                        {
                            skippingAssemblyDetails = true;
                        }
                        break;
                    default:
                        if (!skippingAssemblyDetails)
                            builder.Append(current);
                        break;
                }
            }

            return builder.ToString();
        }

        //Get TypeFallback if the attribute is defined
        System.Type GetSerializationType(object instance)
        {
            System.Type instanceType = instance != null? instance.GetType() : null;
            if (instance != null)
            {
                try
                {
#if RT_ENABLED
                    var customAttrs = instanceType.GetTypeInfo().GetCustomAttributes(typeof(TypeFallback), true);
                    List<object> list = new List<object>();
                    foreach (var attr in customAttrs)
                    {
                        list.Add(attr);
                    }
                    object[] attrs = list.ToArray();
#else
                    object[] attrs = instanceType.GetCustomAttributes(typeof(TypeFallbackAttribute), true);
#endif
                    if (attrs != null && attrs.Length > 0)
                    {
                        TypeFallbackAttribute attrFallback = attrs[0] as TypeFallbackAttribute;
                        instanceType = attrFallback.TypeFallBack;
                    }
                }
                catch { }
            }
            return instanceType;
        }

        /// <summary>
        /// Generic wrapper around TryDeserialize that simply forwards the call.
        /// </summary>
        public Result TryDeserialize<T>(JsonObject data, ref T instance, MetaProperty property = null)
        {
            object boxed = instance;
            var fail = TryDeserialize(data, typeof(T), ref boxed, property);
            if (fail.Succeeded)
            {
                instance = (T)boxed;
            }
            return fail;
        }

        /// <summary>
        /// Attempts to deserialize a value from a serialized state.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="storageType"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public Result TryDeserialize(JsonObject data, Type storageType, ref object result, MetaProperty property = null) {
            if (data == null)
                data = new JsonObject();
            var processors = GetProcessors(storageType);
            var defaultCallbacks = new List<MethodInfo>();
            Invoke_OnBeforeDeserialize(processors, storageType, ref data);
            if (data.IsNull) {
                result = null;
                Invoke_OnAfterDeserialize(defaultCallbacks, processors, storageType, null);
                return Result.Success;
            }

            // Convert legacy data into modern style data
            ConvertLegacyData(ref data);

            try {
                // We wrap the entire deserialize call in a reference group so that we can properly
                // deserialize a "parallel" set of references, ie, a list of objects that are cyclic
                // with regards to the list
                _references.Enter();
                if (_metaProperties != null)
                    _metaProperties.Add(property);

                return InternalDeserialize_1_CycleReference(data, storageType, ref result, processors, property);
            }
            finally {
                _references.Exit();
                if (_metaProperties != null && _metaProperties.Count > 0)
                    _metaProperties.RemoveAt(_metaProperties.Count - 1);
                defaultCallbacks = GetDefaultSerializationCallbackMethods(result != null ? result.GetType() : storageType);
                Invoke_OnAfterDeserialize(defaultCallbacks, processors, storageType, result);
            }
        }

        private Result InternalDeserialize_1_CycleReference(JsonObject data, Type storageType, ref object result, List<ObjectProcessor> processors, MetaProperty property) {
            // We handle object references first because we could be deserializing a cyclic type that is
            // inherited. If that is the case, then if we handle references after inheritances we will try
            // to create an object instance for an abstract/interface type.

            // While object construction should technically be two-pass, we can do it in
            // one pass because of how serialization happens. We traverse the serialization
            // graph in the same order during serialization and deserialization, so the first
            // time we encounter an object it'll always be the definition. Any times after that
            // it will be a reference. Because of this, if we encounter a reference then we
            // will have *always* already encountered the definition for it.
            if (IsObjectReference(data)) {
                int refId = int.Parse(data.AsDictionary[Key_ObjectReference].AsString);
                result = _references.GetReferenceObject(refId);
                return Result.Success;
            }

            return InternalDeserialize_2_Version(data, storageType, ref result, processors, property);
        }

        private Result InternalDeserialize_2_Version(JsonObject data, Type storageType, ref object result, List<ObjectProcessor> processors, MetaProperty property) {
            if (IsVersioned(data)) {
                // data is versioned, but we might not need to do a migration
                string version = data.AsDictionary[Key_Version].AsString;

                Option<VersionedType> versionedType = VersionManager.GetVersionedType(storageType);
                if (versionedType.HasValue &&
                    versionedType.Value.VersionString != version) {

                    // we have to do a migration
                    var deserializeResult = Result.Success;

                    List<VersionedType> path;
                    deserializeResult += VersionManager.GetVersionImportPath(version, versionedType.Value, out path);
                    if (deserializeResult.Failed) return deserializeResult;

                    // deserialize as the original type
                    deserializeResult += InternalDeserialize_3_Inheritance(data, path[0].ModelType, ref result, processors, property);
                    if (deserializeResult.Failed) return deserializeResult;

                    for (int i = 1; i < path.Count; ++i) {
                        result = path[i].Migrate(result);
                    }

                    return deserializeResult;
                }
            }

            return InternalDeserialize_3_Inheritance(data, storageType, ref result, processors, property);
        }

        private Result InternalDeserialize_3_Inheritance(JsonObject data, Type storageType, ref object result, List<ObjectProcessor> processors, MetaProperty property) {
            var deserializeResult = Result.Success;

            Type objectType = storageType;

            // If the serialized state contains type information, then we need to make sure to update our
            // objectType and data to the proper values so that when we construct an object instance later
            // and run deserialization we run it on the proper type.
            if (IsTypeSpecified(data)) {
                JsonObject typeNameData = data.AsDictionary[Key_InstanceType];

                // we wrap everything in a do while false loop so we can break out it
                do {
                    if (typeNameData.IsString == false) {
                        deserializeResult.AddMessage(Key_InstanceType + " value must be a string (in " + data + ")");
                        break;
                    }

                    string typeName = typeNameData.AsString;
                    Type type = TypeLookup.GetType(typeName);
                    if (type == null) {
                        deserializeResult.AddMessage("Unable to locate specified type \"" + typeName + "\"");
                        break;
                    }

                    if (storageType.IsAssignableFrom(type) == false) {
                        deserializeResult.AddMessage("Ignoring type specifier; a field/property of type " + storageType + " cannot hold an instance of " + type);
                        break;
                    }

                    objectType = type;
                } while (false);
            }

            // Construct an object instance if we don't have one already. We also need to construct
            // an instance if the result type is of the wrong type, which may be the case when we
            // have a versioned import graph.
            if (ReferenceEquals(result, null) || result.GetType() != objectType || (objectType != null && objectType.IsAbstract()))
            {
                //Added Try-Catch to prevent desserialization errors when type changed inside deserialized object
                try
                {
                    result = GetConverter(objectType, property).CreateInstance(data, objectType);
                }
                catch
                {
                    result = null;
                }
            }

            var defaultCallbacks = GetDefaultSerializationCallbackMethods(result != null? result.GetType() : storageType);
            // We call OnBeforeDeserializeAfterInstanceCreation here because we still want to invoke the
            // method even if the user passed in an existing instance.
            Invoke_OnBeforeDeserializeAfterInstanceCreation(defaultCallbacks, processors, storageType, result, ref data);

            // NOTE: It is critically important that we pass the actual objectType down instead of
            //       using result.GetType() because it is not guaranteed that result.GetType()
            //       will equal objectType, especially because some converters are known to
            //       return dummy values for CreateInstance() (for example, the default behavior
            //       for structs is to just return the type of the struct).

            return deserializeResult += InternalDeserialize_4_Cycles(data, objectType, ref result, property);
        }

        private Result InternalDeserialize_4_Cycles(JsonObject data, Type resultType, ref object result, MetaProperty property) {
            if (IsObjectDefinition(data)) {
                // NOTE: object references are handled at stage 1

                // If this is a definition, then we have a serialization invariant that this is the
                // first time we have encountered the object (TODO: verify in the deserialization logic)

                // Since at this stage in the deserialization process we already have access to the
                // object instance, so we just need to sync the object id to the references database
                // so that when we encounter the instance we lookup this same object. We want to do
                // this before actually deserializing the object because when deserializing the object
                // there may be references to itself.

                int sourceId = int.Parse(data.AsDictionary[Key_ObjectDefinition].AsString);
                _references.AddReferenceWithId(sourceId, result);
            }

            // Nothing special, go through the standard deserialization logic.
            return InternalDeserialize_5_Converter(data, resultType, ref result, property);
        }

        private Result InternalDeserialize_5_Converter(JsonObject data, Type resultType, ref object result, MetaProperty property) {
            if (IsWrappedData(data)) {
                data = data.AsDictionary[Key_Content];
            }

            return GetConverter(resultType, property).TryDeserialize(data, ref result, resultType);
        }
    }
}