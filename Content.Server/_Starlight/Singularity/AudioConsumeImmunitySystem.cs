using Content.Server.Singularity.EntitySystems;
using Content.Server.Singularity.Events;
using Robust.Shared.Audio.Components;

namespace Content.Server._Starlight.Singularity;

/// <summary>
/// Stops event horizons from eating audio entities.
/// </summary>
/// <remarks>
/// Positional sounds are entities parented to the grid at a position, so a sound played where a
/// singularity is about to appear gets consumed like any other loose entity and cuts out mid-playback.
/// That silently truncates the singularity formation sound, which is played at the exact spot the
/// singularity spawns into, and would do the same to anything else audible near one.
/// </remarks>
public sealed class AudioConsumeImmunitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AudioComponent, EventHorizonAttemptConsumeEntityEvent>(EventHorizonSystem.PreventConsume);
    }
}
