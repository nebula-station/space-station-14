// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from https://github.com/Goob-Station/Goob-Station/blob/707e36693d1d9289a4f04921c5e070b1e4195589/Content.Shared/Weapons/Melee/SharedMeleeWeaponSystem.Goob.cs

using Content.Shared._ES.Camera;

namespace Content.Shared.Weapons.Melee;

public abstract partial class SharedMeleeWeaponSystem
{
    [Dependency] private ESScreenshakeSystem _shake = default!;

    private static readonly ESScreenshakeParameters MeleeUserShake = new(0.08f, 1.0f, 0.009f);
    private static readonly ESScreenshakeParameters MeleeTargetShake = new(0.45f, 1.1f, 0.04f);

    private void DoMeleeScreenshake(EntityUid user, HashSet<EntityUid> targets)
    {
        _shake.Screenshake(user, null, MeleeUserShake);
        foreach (var target in targets)
        {
            _shake.Screenshake(target, MeleeTargetShake, null);
        }
    }
}
