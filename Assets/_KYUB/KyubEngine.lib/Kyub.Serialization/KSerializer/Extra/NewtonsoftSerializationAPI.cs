using System;

namespace Kyub.Serialization
{
    public static class NewtonsoftSerializationAPI
    {
        private static Serializer _serializer = null;

        static NewtonsoftSerializationAPI()
        {
            _serializer = InitNewSerializer();
        }

        static Serializer InitNewSerializer()
        {
            Config _newConfig = new Config();
            _newConfig.SerializeAttributes = new Type[] { typeof(SerializePropertyAttribute), typeof(System.Xml.Serialization.XmlAttributeAttribute) };
            _newConfig.MemberSerialization = MemberSerialization.OptOut;
            _newConfig.IsCaseSensitive = true;
            var v_serializer = new Serializer(_newConfig);
            return v_serializer;
        }

        static bool _isSerializing = false;
        public static string Serialize(Type type, object value, bool p_compressedJson = true)
        {
            Serializer v_serializer = _serializer;
            if (_isSerializing)
                v_serializer = InitNewSerializer();

            if(v_serializer == _serializer)
                _isSerializing = true;
            // serialize the data
            Data data;
            v_serializer.TrySerialize(type, value, out data, null);
            //var fail = v_serializer.TrySerialize(type, value, out data);
            //if (fail.Failed) throw new Exception(fail.FormattedMessages);
            // emit the data via JSON

            if (v_serializer == _serializer)
                _isSerializing = false;

            if (p_compressedJson)
                return JsonPrinter.CompressedJson(data);
            else
                return JsonPrinter.PrettyJson(data);

            
        }

        static bool _isDesserializing = false;
        public static object Deserialize(Type type, string serializedState)
        {
            Serializer v_serializer = _serializer;
            if (_isDesserializing)
                v_serializer = InitNewSerializer();

            if (v_serializer == _serializer)
                _isDesserializing = true;

            try
            {
                //Result fail;
                // step 1: parse the JSON data
                Data data;
                JsonParser.Parse(serializedState, v_serializer.Config, out data);
                //fail = JsonParser.Parse(serializedState, v_serializer.Config, out data);
                //if (fail.Failed) throw new Exception(fail.FormattedMessages);

                // step 2: deserialize the data
                object deserialized = null;
                v_serializer.TryDeserialize(data, type, ref deserialized, null);
                //fail = v_serializer.TryDeserialize(data, type, ref deserialized);
                //if (fail.Failed) throw new Exception(fail.FormattedMessages);

                if (v_serializer == _serializer)
                    _isDesserializing = false;

                return deserialized;
            }
            catch (System.Exception p_exception)
            {
                UnityEngine.Debug.Log(p_exception);
            }

            if (v_serializer == _serializer)
                _isDesserializing = false;
            return null;
        }
    }
}