// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/862852d48934ecca32ab2d9b44eeb3ecf73874a0/Content.Arcane.Client/Chat/DirectionalEmote/DirectionalEmoteUIController.cs

using Content.Client._Nebula.Chat;
using Content.Client.Chat.TypingIndicator;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Arcane.Chat.DirectionalEmote;

[UsedImplicitly]
public sealed class DirectionalEmoteUIController : UIController
{
    [UISystemDependency] private readonly DirectionalEmoteSystem _emote = default!;
    [UISystemDependency] private readonly TypingIndicatorSystem _typingIndicator = default!;

    private DirectionalEmoteWindow? _window;

    public void ToggleWindow(NetEntity target)
    {
        if (_window is { Disposed: false, IsOpen: true })
        {
            CloseWindow();
            return;
        }

        _window = UIManager.CreateWindow<DirectionalEmoteWindow>();
        _window.Target = target;
        _window.OnClose += () => _typingIndicator.ClientChangedWindowStatus(false);
        _window.MessageChanged += () => _typingIndicator.ClientChangedChatText();
        _window.AcceptPressed += () =>
        {
            if (_window == null)
                return;

            _typingIndicator.ClientSubmittedChatText();
            _emote.Send(_window.Target, _window.Text);
            CloseWindow();
        };

        _window.OpenCentered();
        _window.MoveToFront();
        _typingIndicator.ClientChangedWindowStatus(true);
    }

    private void CloseWindow()
    {
        _window?.Close();
        _window = null;
    }
}
