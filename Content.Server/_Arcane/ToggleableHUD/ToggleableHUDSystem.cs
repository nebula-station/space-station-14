// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/2e7ace0bdb6965d914d8417c8b16ca25b6760918/Content.Arcane.Server/ToggleableHUD/ToggleableHUDSystem.cs

using Content.Goobstation.Shared.Disease.Components;
using Content.Shared._Arcane.ToggleableHUD;
using Content.Shared.Actions;
using Content.Shared.Overlays;
using Content.Shared.Popups;
using Content.Shared.Toggleable;

namespace Content.Server._Arcane.ToggleableHUD;

public sealed partial class ToggleableHUDSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ToggleableHUDComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ToggleableHUDComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnToggle(Entity<ToggleableHUDComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryToggle((ent, ent.Comp));
    }

    public bool TryToggle(Entity<ToggleableHUDComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var uid = ent.Owner;
        var toggled = !ent.Comp.IsToggled;

        if (toggled)
        {
            EnsureComp<ShowJobIconsComponent>(uid);
            EnsureComp<ShowMindShieldIconsComponent>(uid);
            EnsureComp<ShowCriminalRecordIconsComponent>(uid);

            var healthBars = EnsureComp<ShowHealthBarsComponent>(uid);
            healthBars.DamageContainers = [.. ent.Comp.DamageContainers];
            Dirty(uid, healthBars);
            EnsureComp<ShowHealthIconsComponent>(uid);
            EnsureComp<ShowDiseaseIconsComponent>(uid);

            EnsureComp<ShowSyndicateIconsComponent>(uid);
        }
        else
        {
            RemComp<ShowJobIconsComponent>(uid);
            RemComp<ShowMindShieldIconsComponent>(uid);
            RemComp<ShowCriminalRecordIconsComponent>(uid);

            RemComp<ShowHealthBarsComponent>(uid);
            RemComp<ShowHealthIconsComponent>(uid);
            RemComp<ShowDiseaseIconsComponent>(uid);

            RemComp<ShowSyndicateIconsComponent>(uid);
        }

        _popup.PopupEntity(Loc.GetString(toggled ? ent.Comp.PopupToggleOn : ent.Comp.PopupToggleOff), uid, uid);

        ent.Comp.IsToggled = toggled;
        Dirty(uid, ent.Comp);
        return true;
    }
}
