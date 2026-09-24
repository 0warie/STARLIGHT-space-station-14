using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Singularity;

/// <summary>
/// Appearance data keys for the gravitational singularity generator.
/// </summary>
[Serializable, NetSerializable]
public enum SingularityGeneratorVisuals : byte
{
    /// <summary>
    /// A <see cref="SingularityGeneratorVisualState"/> describing how charged the generator is.
    /// </summary>
    State,
}

/// <summary>
/// How far along a singularity generator is towards spawning a singularity, for visual purposes.
/// </summary>
[Serializable, NetSerializable]
public enum SingularityGeneratorVisualState : byte
{
    /// <summary>
    /// The generator holds too little power to show anything.
    /// </summary>
    Idle,

    /// <summary>
    /// The generator is spinning up.
    /// </summary>
    Spinning,

    /// <summary>
    /// The generator is close enough to its threshold that a singularity is forming.
    /// </summary>
    Forming,
}
