// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Content.Shared._Nebula.Discord.Linking;

namespace Content.Server._Nebula.Administration;

public sealed partial class NebulaServerApi
{
    public async Task<DiscordLinkingCodeStatus> RegisterLinkingCode(Guid user, Guid code)
    {
        if (!_isActive)
            await RefreshState();

        if (!_isActive)
            return DiscordLinkingCodeStatus.Unavailable;

        try
        {
            using var req = CreateRequest(HttpMethod.Post, "/api/link/codes");
            req.Content = JsonContent.Create(new { code = code.ToString(), userId = user });

            using var resp = await _http.SendAsync(req);

            if (resp.IsSuccessStatusCode)
                return DiscordLinkingCodeStatus.Ok;

            if (resp.StatusCode == HttpStatusCode.Conflict
                && (await resp.Content.ReadAsStringAsync()).Contains("ALREADY_LINKED"))
            {
                return DiscordLinkingCodeStatus.AlreadyLinked;
            }

            _sawmill.Error($"Link code register failed: {(int) resp.StatusCode}");
            return DiscordLinkingCodeStatus.Error;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Link code register failed: {e.Message}");
            return DiscordLinkingCodeStatus.Unavailable;
        }
    }

    public async Task<LinkStatus?> GetLinkedUserDataOrNull(Guid user)
    {
        if (!_isActive)
            return null;

        try
        {
            using var req = CreateRequest(HttpMethod.Get, $"/api/link/{user}");
            using var resp = await _http.SendAsync(req);

            if (!resp.IsSuccessStatusCode)
                return null;

            return await resp.Content.ReadFromJsonAsync<LinkStatus>();
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Get link status failed: {e.Message}");
            return null;
        }
    }
}
