// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Bodycam;
using Robust.Client.UserInterface;

namespace Content.Client._Nebula.Bodycam;

public sealed partial class BodycamFootageConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private BodycamFootageManager _footage = default!;

    private BodycamFootageConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BodycamFootageConsoleWindow>();
        _window.OnRequest += (id, from, to, unfiltered) => SendMessage(new BodycamFootageRequestMessage(id, from, to, unfiltered));
        _window.OnWatch += () =>
        {
            Close();
            _footage.WatchLatest();
        };
        _window.SetClip(_footage.Latest);
        _footage.ClipReceived += OnClipReceived;
    }

    private void OnClipReceived(string clip)
    {
        _window?.SetClip(clip);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is BodycamFootageStateMessage state)
            _window?.UpdateState(state.State);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _footage.ClipReceived -= OnClipReceived;
    }
}
