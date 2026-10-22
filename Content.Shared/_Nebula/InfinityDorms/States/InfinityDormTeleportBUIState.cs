// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.InfinityDorms.States;

[Serializable, NetSerializable]
public sealed class InfinityDormTeleportBUIState(List<List<InfinityDormVisitorData>> players) : BoundUserInterfaceState
{
    public readonly List<List<InfinityDormVisitorData>> Players = players;
}
