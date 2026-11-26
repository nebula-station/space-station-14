// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Arcane.Client/Research/UI/DestructiveAnalyzerBoundUserInterface.cs

using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Arcane.Research.UI;

[UsedImplicitly]
public sealed partial class DestructiveAnalyzerBoundUserInterface : BoundUserInterface
{
    private DestructiveAnalyzerMenu? _menu;

    public DestructiveAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<DestructiveAnalyzerMenu>();
        _menu.OnClose += () => _menu = null;
        _menu.OnServerButtonPressed += () => SendMessage(new OpenResearchServerMenuMessage());
        _menu.OnAnalyzePressed += () => SendMessage(new DestructiveAnalyzerRunMessage());
        _menu.OnMethodSelected += method => SendMessage(new DestructiveAnalyzerSelectMethodMessage(method));
        _menu.OnEjectPressed += () => SendMessage(new DestructiveAnalyzerEjectMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is DestructiveAnalyzerBoundInterfaceState cast)
            _menu?.UpdateState(cast);
    }
}
