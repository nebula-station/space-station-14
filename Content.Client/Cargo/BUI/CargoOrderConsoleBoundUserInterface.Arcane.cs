// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Client/Cargo/BUI/CargoOrderConsoleBoundUserInterface.Arcane.cs

using Content.Client.Cargo.UI;
using Content.Shared.Cargo.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Cargo.BUI;

/// <summary>
/// Arcane - requester from ID, delivery destination and secured delivery in the order window (Orion #377).
/// </summary>
public sealed partial class CargoOrderConsoleBoundUserInterface
{
    [Dependency] private IPrototypeManager _proto = default!;

    private Action<BaseButton.ButtonToggledEventArgs>? _secureToggled;

    private void SetupArcaneOrderMenu(CargoConsoleOrderMenu menu, EntityUid? localPlayer)
    {
        if (_product == null || !EntMan.TryGetComponent<CargoOrderConsoleComponent>(Owner, out var console))
            return;

        var product = _product;
        menu.Requester.Editable = console.EditableRequesterName;
        if (localPlayer != null)
            menu.Requester.Text = _cargoSystem.GenerateRequesterName(localPlayer.Value);

        menu.DeliveryDestination.Text = string.Empty;
        menu.DeliveryDestination.PlaceHolder = _proto.Resolve(console.Account, out var account) && account.DepartmentDestinationName is { } dest
            ? Loc.GetString(dest)
            : Loc.GetString("cargo-console-paper-delivery-destination-default");

        menu.SecureDelivery.Text = Loc.GetString("cargo-console-secure-order-checkbox", ("cost", console.SecureOrderCost));
        menu.SecureDelivery.Pressed = false;
        menu.SecureDelivery.Disabled = !_proto.Resolve(product.Product, out var productProto)
            || !_cargoSystem.CanBeSecuredDelivery((Owner, console), productProto);

        if (_secureToggled != null)
            menu.SecureDelivery.OnToggled -= _secureToggled;

        _secureToggled = args =>
        {
            var cost = product.Cost + (args.Pressed ? console.SecureOrderCost : 0);
            menu.PointCost.Text = Loc.GetString("cargo-console-menu-points-amount", ("amount", cost));
        };
        menu.SecureDelivery.OnToggled += _secureToggled;

        menu.PayPrivately.Text = Loc.GetString("cargo-console-order-private-checkbox");
        menu.PayPrivately.ToolTip = Loc.GetString("cargo-console-order-private-tooltip");
        menu.PayPrivately.Pressed = false;
        menu.PayPrivately.Visible = console.Mode == CargoOrderConsoleMode.DirectOrder;
    }
}
