// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.ERP;
using Robust.Shared.Player;

namespace Content.Server._Nebula.ERP;

public sealed partial class ERPManager : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ERPManagerComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }
}
