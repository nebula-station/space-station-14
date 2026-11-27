// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Body;
using Content.Shared.Popups;

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

public sealed partial class QuantumRegulatorSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private ImplantCooldownSystem _cooldown = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnAction(Entity<QuantumRegulatorComponent> ent, ref QuantumRegulatorActionEvent args)
    {
        if (args.Handled || _body.GetBody(ent) is not { } body)
            return;

        args.Handled = true;
        if (!_cooldown.GetCoolingImplants(body).Any())
        {
            _popup.PopupClient(Loc.GetString("quantum-regulator-popup-nothing"), body, body);
            return;
        }

        _ui.OpenUi(ent.Owner, QuantumRegulatorUiKey.Key, body);
    }

    [SubscribeLocalEvent]
    private void HandleSelectMessage(Entity<QuantumRegulatorComponent> ent, ref QuantumRegulatorSelectMessage args)
    {
        var implant = GetEntity(args.Implant);
        if (_body.GetBody(ent) is not { } body
            || args.Actor != body
            || _cooldown.IsOnCooldown(ent)
            || !_cooldown.GetCoolingImplants(body).Contains(implant))
            return;

        _ui.CloseUi(ent.Owner, QuantumRegulatorUiKey.Key, body);
        _cooldown.ResetCooldown(implant);
        _cooldown.StartCooldown(ent, ent.Comp.Cooldown);

        _popup.PopupClient(Loc.GetString("quantum-regulator-popup-reset", ("implant", implant)), body, body);
    }
}
