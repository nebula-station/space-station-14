// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Eui;
using Content.Shared._Nebula.ReadyManifest;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Nebula.ReadyManifest;

[UsedImplicitly]
public sealed class ReadyManifestEui : BaseEui
{
    private readonly ReadyManifestWindow _window = new();

    public ReadyManifestEui()
    {
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is ReadyManifestEuiState cast)
            _window.Populate(cast);
    }
}
