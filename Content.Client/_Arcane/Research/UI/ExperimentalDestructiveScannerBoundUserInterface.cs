// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Arcane.Client/Research/UI/ExperimentalDestructiveScannerBoundUserInterface.cs

using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Arcane.Research.UI;

[UsedImplicitly]
public sealed partial class ExperimentalDestructiveScannerBoundUserInterface : BoundUserInterface
{
    private ExperimentalDestructiveScannerMenu? _menu;

    public ExperimentalDestructiveScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ExperimentalDestructiveScannerMenu>();
        _menu.OnServerButtonPressed += () => SendMessage(new OpenResearchServerMenuMessage());
        _menu.OnPerformPressed += () => SendMessage(new ExperimentalDestructiveScannerPerformMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ExperimentalDestructiveScannerBoundInterfaceState cast)
            _menu?.UpdateState(cast);
    }
}
