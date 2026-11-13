// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Content.Server._Nebula.Discord.Linking;
using Robust.Server.ServerStatus;
using Robust.Shared.Network;

namespace Content.Server.Administration;

public sealed partial class ServerApi
{
    private void InitializeNebulaPart()
    {
        RegisterHandler(HttpMethod.Post, "/linking/update", RequestUpdateDiscordLinkingData);
        InitializeRoundState();
    }

    private async Task RequestUpdateDiscordLinkingData(IStatusHandlerContext context)
    {
        var body = await ReadJson<UpdateLinkingDataBody>(context);
        if (body == null)
            return;

        await RunOnMainThread(async () =>
        {
            if (!_playerManager.TryGetSessionById(new NetUserId(body.Guid), out var player))
            {
                await RespondError(
                    context,
                    ErrorCode.PlayerNotFound,
                    HttpStatusCode.UnprocessableContent,
                    "Player not found");
                return;
            }

            var discordLinking = _entitySystemManager.GetEntitySystem<DiscordLinkingSystem>();
            discordLinking.ForceUpdateUserData(player.UserId);

            await RespondOk(context);
            _sawmill.Info($"Forced discord linking update for {player.Name}");
        });
    }

    private sealed class UpdateLinkingDataBody
    {
        public required Guid Guid { get; init; }
    }
}
