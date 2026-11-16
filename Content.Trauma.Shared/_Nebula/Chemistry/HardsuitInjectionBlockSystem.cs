// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Events;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Content.Trauma.Shared.Medical.Hypoport;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared._Nebula.Chemistry;

/// <summary>
/// Stops others from injecting someone who wears a hardsuit; the wearer can still inject themselves.
/// Handles instant injectors here, projectile and melee injections are handled on the server.
/// </summary>
public sealed partial class HardsuitInjectionBlockSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> HardsuitTag = "Hardsuit";
    private const string SuitSlot = "outerClothing";
    private const string BlockedMessage = "hardsuit-injection-blocked";

    [SubscribeLocalEvent(before: [typeof(HypoportSystem)])]
    private void OnBeforeInject(Entity<InventoryComponent> ent, ref TargetBeforeInjectEvent args)
    {
        if (args.Cancelled
            || !IsInstant(args.UsedInjector)
            || !IsBlocked(ent, args.EntityUsingInjector))
            return;

        args.OverrideMessage = Loc.GetString(BlockedMessage);
        args.Cancel();
    }

    /// <summary>
    /// Whether <paramref name="user"/> can't inject <paramref name="target"/> because of its hardsuit.
    /// </summary>
    public bool IsBlocked(EntityUid target, EntityUid? user)
    {
        return user != target
            && _inventory.TryGetSlotEntity(target, SuitSlot, out var suit)
            && _tag.HasTag(suit.Value, HardsuitTag);
    }

    private bool IsInstant(EntityUid injector)
    {
        return TryComp<InjectorComponent>(injector, out var comp)
            && ProtoMan.Resolve(comp.ActiveModeProtoId, out var mode)
            && mode.DelayPerVolume == TimeSpan.Zero
            && mode.MobTime == TimeSpan.Zero;
    }
}
