// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Network;

namespace Content.Server.Preferences.Managers;

public sealed partial class ServerPreferencesManager
{
    /// <summary>
    /// Raised when a player selects another character or saves a profile.
    /// </summary>
    public event Action<NetUserId>? PreferencesChanged;
}
