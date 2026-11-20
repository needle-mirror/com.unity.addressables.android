#if !UNITY_6000_5_OR_NEWER
using System;

namespace Unity.Scripting.LifecycleManagement
{
    /// <summary>
    /// Polyfill for Unity versions without the public Lifecycle Management APIs, so [AutoStaticsCleanup] can be used unconditionally.
    /// Also visible to the Editor assembly through InternalsVisibleTo.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = true)]
    internal sealed class AutoStaticsCleanupAttribute : Attribute
    {
    }

    /// <summary>
    /// Polyfill for Unity versions without the public Lifecycle Management APIs, so [NoAutoStaticsCleanup] can be used unconditionally.
    /// Also visible to the Editor assembly through InternalsVisibleTo.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = false)]
    internal sealed class NoAutoStaticsCleanupAttribute : Attribute
    {
    }
}
#endif
