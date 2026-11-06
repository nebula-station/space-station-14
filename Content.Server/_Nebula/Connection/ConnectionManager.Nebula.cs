// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;
using Content.Server._Nebula.Administration;
using Content.Server._Nebula.Discord.Linking;
using Content.Server.Database;
using Content.Shared._Nebula.CCVar;
using Content.Shared._Nebula.Discord.Linking;
using Robust.Shared.Network;

namespace Content.Server.Connection;

public sealed partial class ConnectionManager
{
    [Dependency] private NebulaServerApi _nebulaApi = default!;
    [Dependency] private DiscordLinkingManager _discordLinking = default!;

    private async Task<(ConnectionDenyReason, string, List<BanDef>? bansHit)?> ShouldDenyDiscordLink(NetConnectingArgs e, bool isAdmin)
    {
        if (!_cfg.GetCVar(NCCVars.DiscordRequireLink) || isAdmin)
            return null;

        var link = await _nebulaApi.GetLinkedUserDataOrNull(e.UserId.UserId);
        if (link == null)
        {
            _sawmill.Warning($"Failed to check Discord link for {e.UserName}");
            return null;
        }

        if (link.Linked)
            return null;

        var (status, code) = await _discordLinking.GetOrCreateCode(e.UserId);
        switch (status)
        {
            case DiscordLinkingCodeStatus.AlreadyLinked:
                return null;
            case DiscordLinkingCodeStatus.Ok:
                break;
            default:
                return (ConnectionDenyReason.DiscordLink, Loc.GetString("nebula-discord-link-required-no-code"), null);
        }

        var url = _cfg.GetCVar(NCCVars.DiscordAccountLinkingMessageLink);
        var msg = Loc.GetString("nebula-discord-link-required", ("code", code.ToString()));
        if (!string.IsNullOrWhiteSpace(url))
            msg += "\n\n" + Loc.GetString("nebula-discord-link-required-url", ("url", url));
        msg += "\n\n" + Loc.GetString("nebula-discord-link-required-expires",
            ("minutes", (int) DiscordLinkingManager.CodeLifetime.TotalMinutes));

        return (ConnectionDenyReason.DiscordLink, msg, null);
    }

    private void AddNebulaDenyProperties(ConnectionDenyReason reason, NetUserId user, Dictionary<string, object> properties)
    {
        if (reason != ConnectionDenyReason.DiscordLink || _discordLinking.GetActiveCode(user) is not { } code)
            return;

        properties[DiscordLinkDenyKeys.Code] = code.ToString();

        var url = _cfg.GetCVar(NCCVars.DiscordAccountLinkingMessageLink);
        if (!string.IsNullOrWhiteSpace(url))
            properties[DiscordLinkDenyKeys.Url] = url;
    }
}
