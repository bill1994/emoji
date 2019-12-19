using System;

namespace Kyub.Serialization
{
    /// <summary>
    /// The given method annotated with [OnBeginSerialize] will be called after before begin of serialization process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class OnBeginSerializeAttribute : Attribute
    {
    }

    /// <summary>
    /// The given method annotated with [OnEndSerialize] will be called after end of serialization process
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class OnEndSerializeAttribute : Attribute
    {
    }

    /// <summary>
    /// The given method annotated with [OnBeginDeserialize] will be called before begin of deserialization process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class OnBeginDeserializeAttribute : Attribute
    {
    }

    /// <summary>
    /// The given method annotated with [OnEndDeserialize] will be called after end of deserialization process
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class OnEndDeserializeAttribute : Attribute
    {
    }
}