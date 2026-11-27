// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Arcane.Client/Economy/BankCardBoundUserInterface.cs

using Content.Shared._Arcane.Economy;
using Robust.Client.UserInterface;

namespace Content.Client._Arcane.Economy;

public sealed partial class BankCardBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BankCardWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BankCardWindow>();
        _window.OnWithdraw += amount => SendMessage(new BankCardWithdrawMessage(amount));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is BankCardBoundUiState cast)
            _window?.UpdateState(cast);
    }
}
