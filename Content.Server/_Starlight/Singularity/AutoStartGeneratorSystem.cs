using Content.Server.Singularity.EntitySystems;
using Content.Shared.Singularity.Components;

namespace Content.Server._Starlight.Singularity;

/// <summary>
/// Handles <see cref="AutoStartGeneratorComponent"/>.
/// </summary>
public sealed class AutoStartGeneratorSystem : EntitySystem
{
    [Dependency] private SingularityGeneratorSystem _generator = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoStartGeneratorComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<AutoStartGeneratorComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<SingularityGeneratorComponent>(ent, out var generator))
            return;

        // Goes through the setter so it trips the threshold check, same as a particle hitting it would.
        _generator.SetPower(ent, generator.Threshold, generator);
    }
}
