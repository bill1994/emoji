namespace Kyub.Serialization {
    /// <summary>
    /// Controls how the reflected converter handles member serialization.
    /// </summary>
    public enum MemberSerialization {
        /// <summary>
        /// Only members with [SerializeField] or [Property] attributes are serialized.
        /// </summary>
        OptIn,

        /// <summary>
        /// All public members are serialized by default, members can be excluded using [NotSerialized] or [Ignore].
        /// </summary>
        OptOut,

        /// <summary>
        /// The default member serialization behavior is applied.
        /// </summary>
        Default
    }
}