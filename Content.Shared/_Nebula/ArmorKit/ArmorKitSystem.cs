// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared._Nebula.Examine;
using Content.Shared.Administration.Logs;
using Content.Shared.Clothing.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.ArmorKit;

public sealed partial class ArmorKitSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IComponentFactory _compFactory = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private NameModifierSystem _nameModifier = default!;

    #region Kit events

    // Runs before InteractUsing, otherwise clothing with pockets just stores the kit.
    [SubscribeLocalEvent]
    private void OnBeforeRangedInteract(EntityUid uid, ArmorKitComponent comp, BeforeRangedInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || args.Target is not { } target
            || !CanApply((uid, comp), target, out _))
            return;

        args.Handled = true;
        var doAfter = new DoAfterArgs(EntityManager, args.User, comp.Delay, new ArmorKitDoAfterEvent(), uid, target, uid)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
            BreakOnDamage = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<ArmorKitComponent> ent, ref ArmorKitDoAfterEvent args)
    {
        if (args.Handled
            || args.Cancelled
            || args.Target is not { } target)
            return;

        args.Handled = true;
        if (!CanApply(ent, target, out var reason))
        {
            _popup.PopupEntity(Loc.GetString(reason), target, args.User);
            return;
        }

        if (_net.IsClient)
            return;

        Apply(ent, target);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.User):user} applied {ToPrettyString(ent.Owner):kit} ({ent.Comp.Source}) to {ToPrettyString(target):target}");
        _popup.PopupEntity(Loc.GetString("armor-kit-popup-success", ("target", target)), target, args.User);
        QueueDel(ent);
    }

    #endregion

    #region Applied events

    [SubscribeLocalEvent]
    private void OnRefreshNameModifiers(Entity<ArmorKitAppliedComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("armor-kit-name", 0, ("prefix", Loc.GetString(ent.Comp.Prefix)));
    }

    [SubscribeLocalEvent]
    private void OnExamineTitle(Entity<ArmorKitAppliedComponent> ent, ref ExamineTitleEvent args)
    {
        var prefix = FormattedMessage.EscapeText(Loc.GetString(ent.Comp.Prefix));
        var baseName = FormattedMessage.EscapeText(_nameModifier.GetBaseName(ent.Owner));
        args.Markup = $"[color={ent.Comp.PrefixColor.ToHex()}]{prefix}[/color] {baseName}";
    }

    #endregion

    #region Helpers

    private bool CanApply(Entity<ArmorKitComponent> ent, EntityUid target, [NotNullWhen(false)] out string? reason)
    {
        reason = null;
        if (!TryComp<ClothingComponent>(target, out var clothing)
            || (clothing.Slots & ent.Comp.Slots) == 0)
            reason = "armor-kit-popup-wrong-slot";
        else if (HasComp<ArmorKitAppliedComponent>(target))
            reason = "armor-kit-popup-already-applied";

        return reason == null;
    }

    private void Apply(Entity<ArmorKitComponent> ent, EntityUid target)
    {
        var source = _proto.Index(ent.Comp.Source);
        var registry = new ComponentRegistry();
        foreach (var name in ent.Comp.Components)
        {
            if (source.Components.TryGetValue(name, out var entry))
                registry.Add(name, entry);
            else
                RemComp(target, _compFactory.GetRegistration(name).Type);
        }

        EntityManager.AddComponents(target, registry);

        var applied = EnsureComp<ArmorKitAppliedComponent>(target);
        applied.Prefix = ent.Comp.Prefix;
        applied.PrefixColor = ent.Comp.PrefixColor;
        Dirty(target, applied);

        _nameModifier.RefreshNameModifiers(target);
    }

    #endregion
}
