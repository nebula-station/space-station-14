// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.EUI;
using Content.Shared._Nebula.DetailExaminable;
using Content.Shared.Eui;

namespace Content.Server._Nebula.DetailExaminable;

public sealed class DetailExaminableEui(DetailExaminableEuiState state) : BaseEui
{
    private readonly DetailExaminableEuiState _state = state;

    public override EuiStateBase GetNewState()
    {
        return _state;
    }
}
