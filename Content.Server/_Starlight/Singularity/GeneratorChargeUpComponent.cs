using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Starlight.Singularity;

/// <summary>
/// Makes a singularity generator spark for a while before it spits out its engine, instead of spawning it instantly.
/// </summary>
[RegisterComponent, Access(typeof(GeneratorChargeUpSystem)), AutoGenerateComponentPause]
public sealed partial class GeneratorChargeUpComponent : Component
{
    /// <summary>
    /// How long the generator charges before the engine is spawned.
    /// </summary>
    [DataField]
    public TimeSpan SpawnDelay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// How long the generator keeps sparking. Should also cover the spawned engine's own grow-in animation,
    /// so the sparks don't cut out halfway through it.
    /// </summary>
    [DataField]
    public TimeSpan SparkDuration = TimeSpan.FromSeconds(5.5);

    /// <summary>
    /// Played when the charge-up starts.
    /// </summary>
    [DataField]
    public SoundSpecifier? ChargeSound = new SoundCollectionSpecifier("sparks");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Charging;

    /// <summary>
    /// Whether the engine has been spawned yet, since the sparks outlive it.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Spawned;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan SpawnTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan SparkEndTime;
}
