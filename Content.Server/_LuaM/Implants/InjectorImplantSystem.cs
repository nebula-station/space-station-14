// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Server/_LuaM/Implants/InjectorImplantSystem.cs

using Content.Shared._LuaM.Implants;
using Content.Shared.Body.Systems;
using Content.Shared.Charges.Systems;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._LuaM.Implants;

public sealed partial class InjectorImplantSystem : EntitySystem
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnUse(Entity<InjectorImplantComponent> ent, ref UseInjectorImplantEvent args)
    {
        if (args.Handled
            || !TryComp<SubdermalImplantComponent>(ent, out var implant)
            || implant.ImplantedEntity is not { } user)
            return;

        // The action system takes the charge itself.
        args.Handled = Inject(ent, user);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<InjectorImplantComponent> ent, ref ImplantRelayEvent<MobStateChangedEvent> args)
    {
        if (!ent.Comp.InjectOnCritical
            || args.Args.NewMobState is not (MobState.SoftCrit or MobState.Critical)
            || _timing.CurTime < ent.Comp.NextAutoInject
            || !TryComp<SubdermalImplantComponent>(ent, out var implant)
            || implant.Action is not { } action
            || _charges.IsEmpty(action))
            return;

        if (!Inject(ent, args.ImplantedEntity))
            return;

        ent.Comp.NextAutoInject = _timing.CurTime + ent.Comp.AutoInjectCooldown;
        _charges.TryUseCharge(action);
    }

    private bool Inject(Entity<InjectorImplantComponent> ent, EntityUid user)
    {
        if (!_bloodstream.TryAddToBloodstream(user, ent.Comp.Solution.Clone()))
            return false;

        _audio.PlayPvs(ent.Comp.InjectSound, user);
        _popup.PopupEntity(Loc.GetString(ent.Comp.UsedPopup), user, user);
        return true;
    }
}
