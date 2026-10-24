// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Nebula.GameTicking;

/// <summary>
/// Raised after the lobby info is sent again: a player joined, left, toggled ready or the preset changed.
/// </summary>
public sealed class LobbyInfoUpdatedEvent : EntityEventArgs;
