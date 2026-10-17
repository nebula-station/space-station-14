// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Eui;
using Content.Shared._Nebula.DetailExaminable;
using Content.Shared.Eui;

namespace Content.Client._Nebula.DetailExaminable;

public sealed partial class DetailExaminableEui : BaseEui
{
    [Dependency] private IEntityManager _entManager = default!;

    private readonly DetailExaminableWindow _window;

    public DetailExaminableEui()
    {
        _window = new DetailExaminableWindow();
    }

    public override void Opened()
    {
        _window.OpenCenteredAt(new Vector2(0f, 0.75f));
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is DetailExaminableEuiState examinableState)
            _window.UpdateState(examinableState, _entManager);
    }
}
