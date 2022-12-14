using System;
using System.Globalization;

namespace Kyub.Serialization.Internal {
    /// <summary>
    /// Supports serialization for DateTime, DateTimeOffset, and TimeSpan.
    /// </summary>
    public class DateConverter : Converter {
        // The format strings that we use when serializing DateTime and DateTimeOffset types.
        private const string DateTimeFormatString = @"o";
        private const string DateTimeOffsetFormatString = @"o";

        public override bool CanProcess(Type type) {
            return
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(TimeSpan);
        }

        public override Result TrySerialize(object instance, out JsonObject serialized, Type storageType) {
            if (instance is DateTime) {
                var dateTime = (DateTime)instance;
                serialized = new JsonObject(dateTime.ToString(DateTimeFormatString));
                return Result.Success;
            }

            if (instance is DateTimeOffset) {
                var dateTimeOffset = (DateTimeOffset)instance;
                serialized = new JsonObject(dateTimeOffset.ToString(DateTimeOffsetFormatString));
                return Result.Success;
            }

            if (instance is TimeSpan) {
                var timeSpan = (TimeSpan)instance;
                serialized = new JsonObject(timeSpan.ToString());
                return Result.Success;
            }

            throw new InvalidOperationException("KSerializer Internal Error -- Unexpected serialization type");
        }

        public override Result TryDeserialize(JsonObject data, ref object instance, Type storageType) {
            if (data.IsString == false) {
                return Result.Fail("Date deserialization requires a string, not " + data.Type);
            }

            if (storageType == typeof(DateTime)) {
                DateTime result;
                if (DateTime.TryParse(data.AsString, null, DateTimeStyles.RoundtripKind, out result)) {
                    instance = result;
                    return Result.Success;
                }

                return Result.Fail("Unable to parse " + data.AsString + " into a DateTime");
            }

            if (storageType == typeof(DateTimeOffset)) {
                DateTimeOffset result;
                if (DateTimeOffset.TryParse(data.AsString, null, DateTimeStyles.RoundtripKind, out result)) {
                    instance = result;
                    return Result.Success;
                }

                return Result.Fail("Unable to parse " + data.AsString + " into a DateTimeOffset");
            }

            if (storageType == typeof(TimeSpan)) {
                TimeSpan result;
                if (TimeSpan.TryParse(data.AsString, out result)) {
                    instance = result;
                    return Result.Success;
                }

                return Result.Fail("Unable to parse " + data.AsString + " into a TimeSpan");
            }

            throw new InvalidOperationException("KSerializer Internal Error -- Unexpected deserialization type");
        }
    }
}