using Content.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Singularity;

/// <summary>
/// Handles <see cref="DelayedAmbienceComponent"/>.
/// </summary>
public sealed class DelayedAmbienceSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DelayedAmbienceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<DelayedAmbienceComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.StartTime = _timing.CurTime + ent.Comp.Delay;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<DelayedAmbienceComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (curTime < comp.StartTime)
                continue;

            _ambient.SetAmbience(uid, true);
            RemCompDeferred<DelayedAmbienceComponent>(uid);
        }
    }
}
