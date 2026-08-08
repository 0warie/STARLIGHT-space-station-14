using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Starlight.Singularity;

/// <summary>
/// Holds an entity's <see cref="Content.Shared.Audio.AmbientSoundComponent"/> off for a while after it spawns.
/// Pair with an <c>enabled: false</c> ambience so a one-shot spawn sound can finish before the loop takes over.
/// </summary>
/// <remarks>
/// Belongs on a spawn-only variant of an entity rather than the entity itself, otherwise anything that
/// spawns one directly (admin spawns, mapping) gets the silence without the sound it was meant to leave room for.
/// </remarks>
[RegisterComponent, Access(typeof(DelayedAmbienceSystem)), AutoGenerateComponentPause]
public sealed partial class DelayedAmbienceComponent : Component
{
    /// <summary>
    /// How long after spawning the ambience starts.
    /// </summary>
    [DataField]
    public TimeSpan Delay;

    /// <summary>
    /// When the ambience should be switched on. Set on spawn.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan StartTime;
}
