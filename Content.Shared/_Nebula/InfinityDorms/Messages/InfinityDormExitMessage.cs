// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.InfinityDorm.Messages;

[Serializable, NetSerializable]
public sealed class InfinityDormExitMessage() : BoundUserInterfaceMessage { }
