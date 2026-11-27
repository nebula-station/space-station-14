// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/862852d48934ecca32ab2d9b44eeb3ecf73874a0/Content.Arcane.Client/Lobby/SpeciesWindowUIController.cs

using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Arcane.Lobby;

/// <summary>
/// Adds a species window button next to the species dropdown of every profile editor (Orion #45).
/// </summary>
[UsedImplicitly]
public sealed class SpeciesWindowUIController : UIController
{
    private SpeciesWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        LobbyUIController.OnProfileEditorCreated += OnEditorCreated;
    }

    private void OnEditorCreated(HumanoidProfileEditor editor)
    {
        var dropdown = editor.FindControl<OptionButton>("SpeciesButton");
        if (dropdown.Parent is not { } row)
            return;

        var button = new Button
        {
            Text = Loc.GetString("species-window-open-button"),
            HorizontalAlignment = Control.HAlignment.Right,
            Margin = new Thickness(0, 0, 4, 0),
        };
        button.OnPressed += _ => OpenWindow(editor);
        row.AddChild(button);
        button.SetPositionInParent(dropdown.GetPositionInParent());
    }

    private void OpenWindow(HumanoidProfileEditor editor)
    {
        _window?.Close();
        if (editor.Profile is not { } profile)
            return;

        _window = new SpeciesWindow(profile);
        _window.OnSpeciesChosen += species =>
        {
            editor.SelectSpecies(species);
            _window?.Close();
        };
        _window.OnClose += () => _window = null;
        _window.OpenCenteredLeft();
    }
}
