// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel;

/// <summary>
/// Plays the lean-in animation of <see cref="User"/> towards <see cref="LocalPos"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class ErpLungeEvent(NetEntity user, Vector2 localPos) : EntityEventArgs
{
    public readonly NetEntity User = user;

    public readonly Vector2 LocalPos = localPos;
}
