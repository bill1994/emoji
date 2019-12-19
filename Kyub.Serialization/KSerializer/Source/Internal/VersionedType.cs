using System;

namespace Kyub.Serialization.Internal {
    public struct VersionedType {
        /// <summary>
        /// The direct ancestors that this type can import.
        /// </summary>
        public VersionedType[] Ancestors;

        /// <summary>
        /// The identifying string that is unique among all ancestors.
        /// </summary>
        public string VersionString;

        /// <summary>
        /// The modeling type that this versioned type maps back to.
        /// </summary>
        public Type ModelType;

        /// <summary>
        /// Migrate from an instance of an ancestor.
        /// </summary>
        public object Migrate(object ancestorInstance) {
            return Activator.CreateInstance(ModelType, ancestorInstance);
        }

        public override string ToString() {
            return "VersionedType [ModelType=" + ModelType + ", VersionString=" + VersionString + ", Ancestors.Length=" + Ancestors.Length + "]";
        }

        public static bool operator ==(VersionedType a, VersionedType b) {
            return a.ModelType == b.ModelType;
        }

        public static bool operator !=(VersionedType a, VersionedType b) {
            return a.ModelType != b.ModelType;
        }

        public override bool Equals(object obj) {
            return
                obj is VersionedType &&
                ModelType == ((VersionedType)obj).ModelType;
        }

        public override int GetHashCode() {
            return ModelType.GetHashCode();
        }
    }
}