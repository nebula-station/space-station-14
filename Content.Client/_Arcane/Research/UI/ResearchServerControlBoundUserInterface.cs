// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Arcane.Client/Research/UI/ResearchServerControlBoundUserInterface.cs

using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Arcane.Research.UI;

[UsedImplicitly]
public sealed partial class ResearchServerControlBoundUserInterface : BoundUserInterface
{
    private ResearchServerControlMenu? _menu;

    public ResearchServerControlBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ResearchServerControlMenu>();
        _menu.OnToggleRequested += id => SendMessage(new ToggleServerGenerationMessage(id));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ResearchServerControlBoundInterfaceState cast)
            _menu?.Populate(cast.Servers);
    }
}
