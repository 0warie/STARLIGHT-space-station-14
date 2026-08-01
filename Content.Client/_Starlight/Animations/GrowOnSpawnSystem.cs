using System.Numerics;
using Content.Shared._Starlight.Animations;
using Robust.Client.GameObjects;

namespace Content.Client._Starlight.Animations;

/// <summary>
/// Grows entities with a <see cref="GrowOnSpawnComponent"/> from their start scale to their end scale.
/// </summary>
public sealed partial class GrowOnSpawnSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedPointLightSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrowOnSpawnComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<GrowOnSpawnComponent> ent, ref ComponentStartup args)
    {
        // Make sure the very first frame is already tiny instead of flashing at full size.
        Apply(ent, ent.Comp.StartScale, ent.Comp.StartLightRadius, ent.Comp.StartLightEnergy);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<GrowOnSpawnComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Elapsed += frameTime;

            var fraction = comp.Duration <= 0f ? 1f : Math.Clamp(comp.Elapsed / comp.Duration, 0f, 1f);
            var eased = Easings.InCubic(fraction);

            Apply((uid, comp),
                Vector2.Lerp(comp.StartScale, comp.EndScale, eased),
                float.Lerp(comp.StartLightRadius, comp.EndLightRadius, eased),
                float.Lerp(comp.StartLightEnergy, comp.EndLightEnergy, eased));
        }
    }

    private void Apply(Entity<GrowOnSpawnComponent> ent, Vector2 scale, float radius, float energy)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.SetScale((ent.Owner, sprite), scale);

        if (!_light.TryGetLight(ent, out var light))
            return;

        _light.SetRadius(ent, radius, light);
        _light.SetEnergy(ent, energy, light);
    }
}
