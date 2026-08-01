using System.Numerics;

namespace Content.Shared._Starlight.Animations;

/// <summary>
/// Smoothly grows an entity's sprite, and its point light if it has one, from a start value to an end value.
/// Pair with TimedDespawn and SpawnOnDespawn to make something "grow in" before the real entity replaces it.
/// </summary>
/// <remarks>
/// Purely visual, so the growth is only ever applied client side.
/// TimedDespawn isn't networked, so <see cref="Duration"/> has to be kept in sync with the despawn timer by hand.
/// </remarks>
[RegisterComponent]
public sealed partial class GrowOnSpawnComponent : Component
{
    /// <summary>
    /// How long the entity takes to reach its full size.
    /// </summary>
    [DataField]
    public float Duration = 1.5f;

    [DataField]
    public Vector2 StartScale = new(0.1f, 0.1f);

    [DataField]
    public Vector2 EndScale = Vector2.One;

    /// <summary>
    /// Point light ramp. Ignored if the entity has no point light.
    /// </summary>
    [DataField]
    public float StartLightRadius = 1f;

    [DataField]
    public float EndLightRadius = 10f;

    [DataField]
    public float StartLightEnergy = 0.5f;

    [DataField]
    public float EndLightEnergy = 16f;

    /// <summary>
    /// How far into the growth we are, in seconds.
    /// </summary>
    [ViewVariables]
    public float Elapsed;
}
