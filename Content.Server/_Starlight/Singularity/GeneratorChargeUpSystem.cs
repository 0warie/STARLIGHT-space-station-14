using Content.Shared._Starlight.Singularity;
using Content.Shared.Singularity.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Singularity;

/// <summary>
/// Handles <see cref="GeneratorChargeUpComponent"/>.
/// </summary>
public sealed class GeneratorChargeUpSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    /// <summary>
    /// Starts the charge-up, which takes over spawning the engine.
    /// </summary>
    /// <returns>False if the generator doesn't charge up, in which case the caller should spawn the engine itself.</returns>
    public bool TryStartChargeUp(EntityUid uid)
    {
        if (!TryComp<GeneratorChargeUpComponent>(uid, out var comp) || comp.Charging)
            return false;

        var curTime = _timing.CurTime;
        comp.Charging = true;
        comp.Spawned = false;
        comp.SpawnTime = curTime + comp.SpawnDelay;
        comp.SparkEndTime = curTime + comp.SparkDuration;

        _appearance.SetData(uid, SingularityGeneratorVisuals.Charging, true);
        _audio.PlayPvs(comp.ChargeSound, uid);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<GeneratorChargeUpComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Charging)
                continue;

            if (!comp.Spawned && curTime >= comp.SpawnTime)
            {
                comp.Spawned = true;

                if (TryComp<SingularityGeneratorComponent>(uid, out var generator) && generator.SpawnPrototype != null)
                    Spawn(generator.SpawnPrototype, Transform(uid).Coordinates);
            }

            if (curTime < comp.SparkEndTime)
                continue;

            comp.Charging = false;
            _appearance.SetData(uid, SingularityGeneratorVisuals.Charging, false);
        }
    }
}
