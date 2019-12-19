using System;

namespace Kyub.Serialization.Internal {
    /// <summary>
    /// Simple option type. This is akin to nullable types.
    /// </summary>
    public struct Option<T> {
        private bool _hasValue;
        private T _value;

        public bool HasValue {
            get { return _hasValue; }
        }
        public bool IsEmpty {
            get { return _hasValue == false; }
        }
        public T Value {
            get {
                if (IsEmpty) throw new InvalidOperationException("Option is empty");
                return _value;
            }
        }

        public Option(T value) {
            _hasValue = true;
            _value = value;
        }

        public static Option<T> Empty;
    }

    public static class Option {
        public static Option<T> Just<T>(T value) {
            return new Option<T>(value);
        }
    }
}