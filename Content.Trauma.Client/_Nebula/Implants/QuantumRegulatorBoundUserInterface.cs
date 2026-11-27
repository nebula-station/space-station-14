// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Shared.Body;
using Content.Trauma.Shared._Nebula.Implants.Cyberpunk;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client._Nebula.Implants;

/// <summary>
/// Radial menu of the recharging implants the quantum regulator can reset.
/// </summary>
public sealed partial class QuantumRegulatorBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(GetOptions());
        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOptionBase> GetOptions()
    {
        if (!EntMan.TryGetComponent<OrganComponent>(Owner, out var organ) || organ.Body is not { } body)
            yield break;

        var cooldown = EntMan.System<ImplantCooldownSystem>();
        foreach (var implant in cooldown.GetCoolingImplants(body))
        {
            yield return new RadialMenuActionOption<EntityUid>(Select, implant)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(implant),
                ToolTip = EntMan.GetComponent<MetaDataComponent>(implant).EntityName,
            };
        }
    }

    private void Select(EntityUid implant)
    {
        SendPredictedMessage(new QuantumRegulatorSelectMessage(EntMan.GetNetEntity(implant)));
    }
}
