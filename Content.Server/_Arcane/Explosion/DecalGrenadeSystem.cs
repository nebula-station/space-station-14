// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/Explosion/DecalGrenadeSystem.cs

using Content.Shared._Arcane.Explosion;
using Content.Server.Decals;
using Content.Shared.Trigger;
using Robust.Shared.Random;

namespace Content.Server._Arcane.Explosion;

public sealed partial class DecalGrenadeSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DecalSystem _decal = default!;

    [SubscribeLocalEvent]
    private void OnTrigger(Entity<DecalGrenadeComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;
        if (target == null || ent.Comp.DecalPrototypes.Count == 0)
            return;

        var coordinates = Transform(target.Value).Coordinates;
        for (var i = 0; i < ent.Comp.DecalCount; i++)
        {
            var radius = ent.Comp.DecalRadius * (_random.NextFloat() + 0.1f);
            var angle = _random.NextFloat() * MathF.Tau;
            var offset = new Vector2(radius * MathF.Cos(angle), radius * MathF.Sin(angle));
            var decal = _random.Pick(ent.Comp.DecalPrototypes);

            _decal.TryAddDecal(decal, coordinates.Offset(offset), out _, cleanable: true);
        }

        args.Handled = true;
    }
}
