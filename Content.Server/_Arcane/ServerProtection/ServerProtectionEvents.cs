// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/b52ef462f193de8563488db184be5393b9881342/Content.Arcane.Common/ServerProtection/ServerProtectionEvents.cs

using Robust.Shared.Network;

namespace Content.Server._Arcane.ServerProtection;

/// <summary>
///     Raised broadcast when an entity sends an emote to chat.
/// </summary>
[ByRefEvent]
public readonly record struct EntityEmoteSentEvent(EntityUid Source);

/// <summary>
///     Raised broadcast when an admin places a server ban.
/// </summary>
[ByRefEvent]
public readonly record struct AdminBanPlacedEvent(NetUserId Admin, string AdminName, string TargetName);

/// <summary>
///     Raised broadcast when an admin removes another admin or reduces their permissions.
/// </summary>
[ByRefEvent]
public readonly record struct AdminPermissionsReducedEvent(NetUserId Admin, string AdminName, string TargetName, string Action);
