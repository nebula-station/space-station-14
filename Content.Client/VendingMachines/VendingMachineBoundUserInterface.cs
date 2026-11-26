using Content.Client.UserInterface.Controls;
using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using System.Linq;
using Content.Shared.VendingMachines.Components;
using Content.Shared._Arcane.Economy;

namespace Content.Client.VendingMachines;

public sealed class VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private VendingMachineMenu? _menu;

    [ViewVariables]
    private List<VendingMachineInventoryEntry> _cachedInventory = new();

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindowCenteredLeft<VendingMachineMenu>();
        _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _menu.OnItemSelected += OnItemSelected;
        // Nebula start
        _menu.GetPrice = (id, type) =>
        {
            var price = new VendingItemPriceEvent(PlayerManager.LocalEntity, id, (int) type);
            EntMan.EventBus.RaiseLocalEvent(Owner, ref price);
            return price.Price;
        };
        // Nebula end
        Refresh();
    }

    public void Refresh()
    {
        var enabled = EntMan.TryGetComponent(Owner, out VendingMachineEjectComponent? eject) && !eject.Ejecting;

        var system = EntMan.System<VendingMachineSystem>();
        _cachedInventory = system.GetAllInventory(Owner);

        _menu?.Populate(_cachedInventory, enabled);
        UpdateBalance(); // Nebula edit
    }

    public void UpdateAmounts()
    {
        var enabled = EntMan.TryGetComponent(Owner, out VendingMachineEjectComponent? eject) && !eject.Ejecting;

        var system = EntMan.System<VendingMachineSystem>();
        _cachedInventory = system.GetAllInventory(Owner);
        _menu?.UpdateAmounts(_cachedInventory, enabled);
        UpdateBalance(); // Nebula edit
    }

    // Nebula start
    private void UpdateBalance()
    {
        if (_menu == null || PlayerManager.LocalEntity is not { } user)
            return;

        var name = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        var balance = new BankBalanceQueryEvent(user);
        EntMan.EventBus.RaiseEvent(EventSource.Local, ref balance);
        _menu.Title = balance.Balance is { } credits
            ? Loc.GetString("vending-machine-title-with-balance", ("name", name), ("balance", credits))
            : name;
    }
    // Nebula end

    private void OnItemSelected(GUIBoundKeyEventArgs args, ListData data)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (data is not VendorItemsListData { ItemIndex: var itemIndex })
            return;

        if (_cachedInventory.Count == 0)
            return;

        var selectedItem = _cachedInventory.ElementAtOrDefault(itemIndex);

        if (selectedItem == null)
            return;

        SendPredictedMessage(new VendingMachineEjectMessage(selectedItem.Type, selectedItem.ID));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_menu == null)
            return;

        _menu.OnItemSelected -= OnItemSelected;
        _menu.OnClose -= Close;
        _menu.Dispose();
    }
}
