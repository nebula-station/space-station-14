// SPDX-License-Identifier: AGPL-3.0-only
// Ported from https://github.com/AdventureTimeSS14/space_station_ADT/blob/921015157ff5a0e4d3156c1fccc0a1f3c34c0a96/Content.Client/ADT/Casino/SlotMachineCasinoBoundUserInterface.cs

using Content.Client.UserInterface.Controls;
using Content.Shared._ADT.Casino;

namespace Content.Client._ADT.Casino;

public sealed class SlotMachineCasinoBoundUserInterface : BoundUserInterface
{
    private SlotMachineCasinoMenu? _menu;

    public SlotMachineCasinoBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new SlotMachineCasinoMenu(this);
        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SlotMachineCasinoBoundUserInterfaceState casinoState)
            _menu?.UpdateState(casinoState);
    }

    public void SendAction(SlotMachineCasinoAction action, int amount = 0, string choice = "")
    {
        SendMessage(new SlotMachineCasinoActionMessage(action, amount, choice));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Orphan(); // Nebula edit
        _menu = null;
    }
}
