// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.InfinityDorms.States;

[Serializable, NetSerializable]
public sealed class InfinityDormExitBUIState(bool isClosed, bool isOwner, int playersCount) : BoundUserInterfaceState
{
    public readonly bool IsClosed = isClosed;
    public readonly bool IsOwner = isOwner;
    public readonly int PlayersCount = playersCount;
}
