// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Nebula.CCVar;
using Content.Shared.GameTicking.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.Lobby;

/// <summary>
/// Lobby backgrounds the player can pick instead of the one the server rolled.
/// </summary>
public static class NebulaLobbyBackgrounds
{
    /// <summary>
    /// Same extensions the server picks random backgrounds from.
    /// </summary>
    private static readonly string[] Extensions = ["png", "jpg", "jpeg", "webp"];

    public static List<LobbyBackgroundPrototype> GetAvailable(IPrototypeManager prototypes)
    {
        return [.. prototypes.EnumeratePrototypes<LobbyBackgroundPrototype>()
            .Where(proto => Extensions.Contains(proto.Background.Extension))
            .OrderBy(proto => proto.ID, StringComparer.Ordinal)];
    }

    /// <summary>
    /// The picked background, or the first available one when the picked id is unknown.
    /// Null while random backgrounds are on, so the server's choice is used.
    /// </summary>
    public static ProtoId<LobbyBackgroundPrototype>? GetPicked(IConfigurationManager cfg, IPrototypeManager prototypes)
    {
        if (cfg.GetCVar(NCCVars.LobbyBackgroundRandom))
            return null;

        var picked = cfg.GetCVar(NCCVars.LobbyBackground);
        var available = GetAvailable(prototypes);

        if (available.Any(proto => proto.ID == picked))
            return picked;

        return available.Count > 0 ? available[0].ID : null;
    }
}
