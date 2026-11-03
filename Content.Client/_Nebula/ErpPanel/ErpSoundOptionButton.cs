// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Nebula.ErpPanel;

/// <summary>
/// Sound picker with a play button on every sound, so it can be heard before saving.
/// </summary>
public sealed class ErpSoundOptionButton : OptionButton
{
    private const string PlayIcon = "/Textures/Interface/AdminActions/play.png";

    /// <summary>
    /// Raised with the item id of the sound to play.
    /// </summary>
    public event Action<int>? OnPreview;

    public override void ButtonOverride(Button button)
    {
        // called right after the item is added, negative ids are "no sound"
        var id = GetItemId(ItemCount - 1);
        if (id < 0)
            return;

        var play = new ShapedButton
        {
            Icon = IoCManager.Resolve<IResourceCache>().GetTexture(PlayIcon),
            IconSize = 10,
            MinWidth = 22,
            MinHeight = 22,
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Center,
            ToolTip = Loc.GetString("erp-custom-emote-sound-preview"),
        };
        play.AddStyleClass(NebulaStyleClass.ButtonGhost);
        play.OnPressed += _ => OnPreview?.Invoke(id);

        button.Label.Margin = new Thickness(0, 0, 26, 0);
        button.AddChild(play);
    }
}
