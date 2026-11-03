// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Content.Shared._Nebula.Sponsor;

namespace Content.Server._Nebula.Administration;

public sealed partial class NebulaServerApi
{
    public async Task<SponsorStatus?> GetSponsorUserDataOrNull(Guid user)
    {
        if (!await EnsureActive())
            return null;

        try
        {
            using var req = CreateRequest(HttpMethod.Get, $"/api/boosty/{user}");
            using var resp = await _http.SendAsync(req);

            if (!resp.IsSuccessStatusCode)
                return null;

            return await resp.Content.ReadFromJsonAsync<SponsorStatus>();
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Get boosty status failed: {e.Message}");
            return null;
        }
    }
}
