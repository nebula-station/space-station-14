// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from https://github.com/Goob-Station/Goob-Station/blob/707e36693d1d9289a4f04921c5e070b1e4195589/Content.Shared/_ES/Camera/Trigger/ESScreenshakeUserOnTriggerSystem.cs

using Content.Shared.Trigger;

namespace Content.Shared._ES.Camera.Trigger;

public sealed partial class ESScreenshakeUserOnTriggerSystem : XOnTriggerSystem<ESScreenshakeUserOnTriggerComponent>
{
    [Dependency] private ESScreenshakeSystem _screenShake = default!;

    protected override void OnTrigger(Entity<ESScreenshakeUserOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (args.User == null)
            return;

        _screenShake.Screenshake(args.User.Value, ent.Comp.Translation, ent.Comp.Rotation);
        args.Handled = true;
    }
}
