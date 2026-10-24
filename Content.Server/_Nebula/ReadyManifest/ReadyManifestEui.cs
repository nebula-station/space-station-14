// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.EUI;
using Content.Shared.Eui;

namespace Content.Server._Nebula.ReadyManifest;

public sealed class ReadyManifestEui(ReadyManifestSystem system) : BaseEui
{
    public override EuiStateBase GetNewState()
    {
        return system.GetState();
    }

    public override void Closed()
    {
        base.Closed();
        system.OnEuiClosed(Player);
    }
}
