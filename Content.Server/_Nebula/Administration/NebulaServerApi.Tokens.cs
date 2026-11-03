// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Content.Server._Nebula.Administration;

public enum TokenPurchaseResult : byte
{
    Created,
    NotLinked,
    InsufficientTokens,
    ActivePurchase,

    /// <summary>
    /// The bot may or may not have saved the purchase, the caller must refund it.
    /// </summary>
    Unknown,
}

public sealed class TokenActivePurchase
{
    public Guid Id { get; set; }
    public string ListingId { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
}

public sealed class TokenStatus
{
    public int Balance { get; set; }
    public TokenActivePurchase? ActivePurchase { get; set; }
    public int WeeklyTokens { get; set; }
    public int WeeklyTokenCap { get; set; }
    public DateTimeOffset? NextWeeklyGrantAt { get; set; }
}

public sealed partial class NebulaServerApi
{
    /// <returns>Whether the bot answered. <paramref name="Status"/> is null when the account is not linked.</returns>
    public async Task<(bool Answered, TokenStatus? Status)> GetTokenStatus(Guid user)
    {
        if (!await EnsureActive())
            return (false, null);

        try
        {
            using var req = CreateRequest(HttpMethod.Get, $"/api/tokens/{user}");
            using var resp = await _http.SendAsync(req);

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return (true, null);

            if (!resp.IsSuccessStatusCode)
            {
                _sawmill.Error($"Get token status failed: {(int) resp.StatusCode}");
                return (false, null);
            }

            return (true, await resp.Content.ReadFromJsonAsync<TokenStatus>());
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Get token status failed: {e.Message}");
            return (false, null);
        }
    }

    public async Task<TokenPurchaseResult> PurchaseToken(Guid user, Guid purchaseId, string listingId, string serverId)
    {
        if (!await EnsureActive())
            return TokenPurchaseResult.Unknown;

        try
        {
            using var req = CreateRequest(HttpMethod.Post, $"/api/tokens/{user}/purchases");
            req.Content = JsonContent.Create(new { purchaseId, listingId, serverId });

            using var resp = await _http.SendAsync(req);

            if (resp.IsSuccessStatusCode)
                return TokenPurchaseResult.Created;

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return TokenPurchaseResult.NotLinked;

            if (resp.StatusCode == HttpStatusCode.Conflict)
            {
                return (await resp.Content.ReadAsStringAsync()).Contains("INSUFFICIENT_TOKENS")
                    ? TokenPurchaseResult.InsufficientTokens
                    : TokenPurchaseResult.ActivePurchase;
            }

            _sawmill.Error($"Token purchase failed: {(int) resp.StatusCode}");
            return TokenPurchaseResult.Unknown;
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Token purchase failed: {e.Message}");
            return TokenPurchaseResult.Unknown;
        }
    }

    /// <returns>False if the call has to be repeated.</returns>
    public Task<bool> MarkTokenPurchaseGranted(Guid purchaseId)
    {
        return FinishTokenPurchase(purchaseId, "grant");
    }

    /// <returns>False if the call has to be repeated.</returns>
    public Task<bool> RefundTokenPurchase(Guid purchaseId)
    {
        return FinishTokenPurchase(purchaseId, "refund");
    }

    /// <returns>False if the call has to be repeated.</returns>
    public async Task<bool> RefundPendingTokenPurchases(string serverId)
    {
        if (!await EnsureActive())
            return false;

        return await SendFinish($"/api/tokens/servers/{Uri.EscapeDataString(serverId)}/refund-pending");
    }

    private async Task<bool> FinishTokenPurchase(Guid purchaseId, string action)
    {
        if (!await EnsureActive())
            return false;

        return await SendFinish($"/api/tokens/purchases/{purchaseId}/{action}");
    }

    private async Task<bool> SendFinish(string path)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Post, path);
            using var resp = await _http.SendAsync(req);

            // 404 and 409 are final: there is nothing left to finish and repeating will not change it.
            if (resp.IsSuccessStatusCode
                || resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
            {
                return true;
            }

            _sawmill.Error($"{path} failed: {(int) resp.StatusCode}");
            return false;
        }
        catch (Exception e)
        {
            _sawmill.Warning($"{path} failed: {e.Message}");
            return false;
        }
    }
}
