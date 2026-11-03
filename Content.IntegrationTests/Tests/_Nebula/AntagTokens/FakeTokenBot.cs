// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable

using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Content.IntegrationTests.Tests._Nebula.AntagTokens;

/// <summary>
/// Minimal HTTP stand-in for the token API of ss14-discord-bridge. It follows the same paths and status codes,
/// so the real <c>NebulaServerApi</c> client is what gets tested.
/// </summary>
public sealed class FakeTokenBot : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly ConcurrentDictionary<Guid, string> _purchaseOwners = [];

    public readonly string Url;
    public readonly ConcurrentDictionary<string, int> Balances = [];
    public readonly ConcurrentBag<string> Granted = [];
    public readonly ConcurrentBag<string> Refunded = [];
    public int RefundPendingCalls;
    public int StatusRequestsAnswered;

    /// <summary>
    /// The waiting purchase of each user, as the bot sees it.
    /// </summary>
    public readonly ConcurrentDictionary<string, (Guid Id, string Listing, string Server)> ActivePurchases = [];

    /// <summary>
    /// Rank returned by /api/boosty for every user, null means no subscription.
    /// </summary>
    public int? SponsorRank;

    /// <summary>
    /// Discord ID returned by /api/link for every user, null means not linked.
    /// </summary>
    public string? LinkedDiscordId;

    /// <summary>
    /// How long /api/boosty takes to answer.
    /// </summary>
    public TimeSpan SponsorDelay;

    /// <summary>
    /// How long /api/link takes to answer.
    /// </summary>
    public TimeSpan LinkDelay;

    public int LinkRequestsStarted;
    public int LinkRequestsAnswered;

    public int SponsorRequestsStarted;
    public int SponsorRequestsAnswered;

    /// <param name="start">False leaves the port closed until <see cref="Start"/>, as if the bot was down.</param>
    public FakeTokenBot(bool start = true)
    {
        var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        var port = ((IPEndPoint) socket.LocalEndpoint).Port;
        socket.Stop();

        Url = $"http://127.0.0.1:{port}";
        _listener.Prefixes.Add($"{Url}/");

        if (start)
            Start();
    }

    public void Start()
    {
        _listener.Start();
        _ = Task.Run(Loop);
    }

    private async Task Loop()
    {
        while (!_stop.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception) when (_stop.IsCancellationRequested)
            {
                return;
            }

            var (status, body) = await Handle(context.Request);
            context.Response.StatusCode = status;
            if (body != null)
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                context.Response.ContentType = "application/json";
                await context.Response.OutputStream.WriteAsync(bytes);
            }

            context.Response.Close();
        }
    }

    private async Task<(int Status, string? Body)> Handle(HttpListenerRequest request)
    {
        var parts = request.Url!.AbsolutePath.Trim('/').Split('/');

        // Health check of NebulaServerApi.
        if (parts is ["api", "link", _])
        {
            Interlocked.Increment(ref LinkRequestsStarted);
            await Task.Delay(LinkDelay);
            Interlocked.Increment(ref LinkRequestsAnswered);

            return LinkedDiscordId is { } discordId
                ? (200, $"{{\"linked\":true,\"discordId\":\"{discordId}\"}}")
                : (200, "{\"linked\":false,\"discordId\":null}");
        }

        if (parts is ["api", "boosty", _])
        {
            Interlocked.Increment(ref SponsorRequestsStarted);
            await Task.Delay(SponsorDelay);
            Interlocked.Increment(ref SponsorRequestsAnswered);

            return SponsorRank is { } rank
                ? (200, $"{{\"rank\":{rank},\"startedAt\":\"2026-01-01T00:00:00Z\"}}")
                : (404, null);
        }

        if (parts is ["api", "tokens", "servers", _, "refund-pending"])
        {
            Interlocked.Increment(ref RefundPendingCalls);
            return (200, "{\"refunded\":0}");
        }

        if (parts is ["api", "tokens", "purchases", var id, var action])
            return Finish(Guid.Parse(id), action);

        if (parts is ["api", "tokens", var user])
        {
            if (!Balances.TryGetValue(user, out var balance))
                return (404, "{\"error\":\"NOT_LINKED\"}");

            var active = ActivePurchases.TryGetValue(user, out var purchase)
                ? $"{{\"id\":\"{purchase.Id}\",\"listingId\":\"{purchase.Listing}\",\"serverId\":\"{purchase.Server}\"}}"
                : "null";

            Interlocked.Increment(ref StatusRequestsAnswered);
            return (200, $"{{\"balance\":{balance},\"activePurchase\":{active}}}");
        }

        if (parts is ["api", "tokens", var buyer, "purchases"])
            return await Purchase(buyer, request);

        return (500, null);
    }

    private async Task<(int, string?)> Purchase(string user, HttpListenerRequest request)
    {
        if (!Balances.TryGetValue(user, out var balance))
            return (404, "{\"error\":\"NOT_LINKED\"}");

        if (balance < 1)
            return (409, "{\"error\":\"INSUFFICIENT_TOKENS\"}");

        using var reader = new StreamReader(request.InputStream);
        var json = JsonDocument.Parse(await reader.ReadToEndAsync());
        var purchaseId = json.RootElement.GetProperty("purchaseId").GetGuid();

        Balances[user] = balance - 1;
        AddPurchase(user, purchaseId, json.RootElement.GetProperty("listingId").GetString()!,
            json.RootElement.GetProperty("serverId").GetString()!);
        return (204, null);
    }

    /// <summary>
    /// Saves a waiting purchase without asking the game server, like a request it has already given up on.
    /// </summary>
    public void AddPurchase(string user, Guid id, string listing, string server)
    {
        _purchaseOwners[id] = user;
        ActivePurchases[user] = (id, listing, server);
    }

    private (int, string?) Finish(Guid purchaseId, string action)
    {
        if (!_purchaseOwners.TryRemove(purchaseId, out var user))
            return (404, "{\"error\":\"PURCHASE_NOT_FOUND\"}");

        ActivePurchases.TryRemove(user, out _);

        if (action == "refund")
        {
            Balances.AddOrUpdate(user, 1, (_, balance) => balance + 1);
            Refunded.Add(user);
        }
        else
        {
            Granted.Add(user);
        }

        return (204, null);
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Close();
        _stop.Dispose();
    }
}
