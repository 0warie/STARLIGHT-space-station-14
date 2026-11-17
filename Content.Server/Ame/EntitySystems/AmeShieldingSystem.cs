using Content.Server.Ame.Components;
using Content.Shared.Ame.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Ame.EntitySystems;

public sealed partial class AmeShieldingSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearanceSystem = default!;
    [Dependency] private PointLightSystem _pointLightSystem = default!;

    public void SetCore(EntityUid uid, bool value, AmeShieldComponent? shield = null)
    {
        if (!Resolve(uid, ref shield))
            return;
        if (value == shield.IsCore)
            return;

        shield.IsCore = value;
        _appearanceSystem.SetData(uid, AmeShieldVisuals.Core, value);
        if (!value)
            UpdateCoreVisuals(uid, 0, false, false, shield); // Starlight-edit
    }

    public void UpdateCoreVisuals(EntityUid uid, int injectionStrength, bool injecting, bool overloading, AmeShieldComponent? shield = null) // Starlight-edit: overloading decides Strong state
    {
        if (!Resolve(uid, ref shield))
            return;

        if (!injecting)
        {
            _appearanceSystem.SetData(uid, AmeShieldVisuals.CoreState, AmeCoreState.Off);
            _pointLightSystem.SetEnabled(uid, false);
            return;
        }

        _pointLightSystem.SetRadius(uid, Math.Clamp(injectionStrength, 1, 12));
        _pointLightSystem.SetEnabled(uid, true);
        _appearanceSystem.SetData(uid, AmeShieldVisuals.CoreState, overloading ? AmeCoreState.Strong : AmeCoreState.Weak); // Starlight-edit: was injectionStrength > 2, out of sync with SafeFuelLimit
    }
}
