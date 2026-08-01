using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Singularity;

[Serializable, NetSerializable]
public enum SingularityGeneratorVisuals : byte
{
    /// <summary>
    /// Set while the generator is charging up an engine, so it can show a sparking texture.
    /// </summary>
    Charging,
}
