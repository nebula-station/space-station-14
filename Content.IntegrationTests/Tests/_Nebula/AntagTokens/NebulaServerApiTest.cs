// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable

using Content.IntegrationTests.Pair;
using Content.Server._Nebula.Administration;
using Content.Server._Nebula.Discord.Linking;
using Content.Server._Nebula.Sponsor;
using Content.Shared._Nebula.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests._Nebula.AntagTokens;

[TestFixture]
[TestOf(typeof(NebulaServerApi))]
public sealed class NebulaServerApiTest
{
    private static PoolSettings Settings => new() { Dirty = true };

    private static async Task Configure(TestPair pair, FakeTokenBot bot, float healthCheckInterval = 0f)
    {
        var cfg = pair.Server.ResolveDependency<IConfigurationManager>();
        await pair.Server.WaitPost(() =>
        {
            cfg.SetCVar(NCCVars.ApiHealthCheckInterval, healthCheckInterval);
            cfg.SetCVar(NCCVars.ApiToken, "test-key");
            cfg.SetCVar(NCCVars.ApiUrl, bot.Url);
        });
    }

    private static async Task Finish(TestPair pair)
    {
        var cfg = pair.Server.ResolveDependency<IConfigurationManager>();
        await pair.Server.WaitPost(() =>
        {
            cfg.SetCVar(NCCVars.ApiToken, string.Empty);
            cfg.SetCVar(NCCVars.ApiUrl, string.Empty);
            cfg.SetCVar(NCCVars.ApiHealthCheckInterval, NCCVars.ApiHealthCheckInterval.DefaultValue);
        });
        await pair.CleanReturnAsync();
    }

    private static async Task WaitFor(TestPair pair, Func<bool> condition, string what)
    {
        for (var i = 0; i < 400; i++)
        {
            if (condition())
                return;

            await pair.RunTicksSync(3);
            await Task.Delay(10);
        }

        Assert.Fail($"Timed out waiting for {what}");
    }

    private static async Task<T> Call<T>(TestPair pair, Func<Task<T>> call)
    {
        Task<T>? task = null;
        await pair.Server.WaitPost(() => task = call());
        await WaitFor(pair, () => task is { IsCompleted: true }, "the API call");
        return await task!;
    }

    [Test]
    public async Task RecoversWhenBotWasDownAtStart()
    {
        using var bot = new FakeTokenBot(start: false);
        await using var pair = await PoolManager.GetServerClient(Settings);
        var api = pair.Server.ResolveDependency<NebulaServerApi>();
        var user = Guid.NewGuid();

        bot.SponsorRank = 3;
        bot.LinkedDiscordId = "500";

        await Configure(pair, bot);

        Assert.That(await Call(pair, () => api.GetSponsorUserDataOrNull(user)), Is.Null);
        Assert.That(await Call(pair, () => api.GetLinkedUserDataOrNull(user)), Is.Null);

        bot.Start();

        var sponsor = await Call(pair, () => api.GetSponsorUserDataOrNull(user));
        Assert.That(sponsor?.Rank, Is.EqualTo(3), "sponsor status stayed lost after the bot came back");

        var link = await Call(pair, () => api.GetLinkedUserDataOrNull(user));
        Assert.That(link?.DiscordId, Is.EqualTo("500"), "link status stayed lost after the bot came back");

        await Finish(pair);
    }

    [Test]
    public async Task BotIsNotCheckedAgainDuringInterval()
    {
        using var bot = new FakeTokenBot(start: false);
        await using var pair = await PoolManager.GetServerClient(Settings);
        var api = pair.Server.ResolveDependency<NebulaServerApi>();
        var user = Guid.NewGuid();

        bot.SponsorRank = 3;

        await Configure(pair, bot, healthCheckInterval: 3600f);

        Assert.That(await Call(pair, () => api.GetSponsorUserDataOrNull(user)), Is.Null);

        bot.Start();

        Assert.That(await Call(pair, () => api.GetSponsorUserDataOrNull(user)), Is.Null);
        Assert.That(bot.LinkRequestsStarted, Is.Zero, "the health check ran again before the interval ended");
        Assert.That(bot.SponsorRequestsStarted, Is.Zero);

        await Finish(pair);
    }

    [Test]
    public async Task DisconnectDuringRequestDoesNotCacheSponsor()
    {
        using var bot = new FakeTokenBot();
        await using var pair = await PoolManager.GetServerClient(Settings);
        var server = pair.Server;
        var api = server.ResolveDependency<NebulaServerApi>();
        var sponsors = server.System<SponsorManagerSystem>();

        bot.SponsorRank = 3;
        await Configure(pair, bot);

        // Makes the API active, so the request below really reaches the bot.
        await Call(pair, () => api.GetLinkedUserDataOrNull(Guid.NewGuid()));

        bot.SponsorDelay = TimeSpan.FromSeconds(2);
        var started = bot.SponsorRequestsStarted;
        var answered = bot.SponsorRequestsAnswered;

        var dummy = await server.AddDummySession();
        await WaitFor(pair, () => bot.SponsorRequestsStarted > started, "the sponsor request");

        // Lets the rest of the server finish connecting the dummy, as it happens for a real player.
        await pair.RunTicksSync(30);

        Assert.That(bot.SponsorRequestsAnswered, Is.EqualTo(answered), "the request must still be running");
        await server.RemoveDummySession(dummy);
        await WaitFor(pair, () => bot.SponsorRequestsAnswered > answered, "the sponsor answer");
        await pair.RunTicksSync(30);
        await Task.Delay(100);
        await pair.RunTicksSync(30);

        Assert.That(sponsors.IsSponsor(dummy.UserId), Is.False, "the cache kept a player that already left");

        await Finish(pair);
    }

    [Test]
    public async Task DisconnectDuringRequestDoesNotCacheLink()
    {
        using var bot = new FakeTokenBot();
        await using var pair = await PoolManager.GetServerClient(Settings);
        var server = pair.Server;
        var api = server.ResolveDependency<NebulaServerApi>();
        var links = server.System<DiscordLinkingSystem>();

        bot.LinkedDiscordId = "500";
        await Configure(pair, bot);

        // Makes the API active, so the request below really reaches the bot.
        await Call(pair, () => api.GetLinkedUserDataOrNull(Guid.NewGuid()));

        bot.LinkDelay = TimeSpan.FromSeconds(2);
        var started = bot.LinkRequestsStarted;
        var answered = bot.LinkRequestsAnswered;

        var dummy = await server.AddDummySession();
        await WaitFor(pair, () => bot.LinkRequestsStarted > started, "the link request");

        // Lets the rest of the server finish connecting the dummy, as it happens for a real player.
        await pair.RunTicksSync(30);

        Assert.That(bot.LinkRequestsAnswered, Is.EqualTo(answered), "the request must still be running");
        await server.RemoveDummySession(dummy);

        await WaitFor(pair, () => bot.LinkRequestsAnswered > answered, "the link answer");
        await pair.RunTicksSync(30);
        await Task.Delay(100);
        await pair.RunTicksSync(30);

        Assert.That(links.IsUserLinked(dummy.UserId), Is.False, "the cache kept a player that already left");

        bot.LinkDelay = TimeSpan.Zero;
        await Finish(pair);
    }

    [Test]
    public async Task ConnectedPlayerIsCachedAsLinked()
    {
        using var bot = new FakeTokenBot();
        await using var pair = await PoolManager.GetServerClient(Settings);
        var server = pair.Server;
        var api = server.ResolveDependency<NebulaServerApi>();
        var links = server.System<DiscordLinkingSystem>();

        bot.LinkedDiscordId = "500";
        await Configure(pair, bot);
        await Call(pair, () => api.GetLinkedUserDataOrNull(Guid.NewGuid()));

        ICommonSession dummy = await server.AddDummySession();
        await WaitFor(pair, () => links.IsUserLinked(dummy.UserId), "the link status");

        await Finish(pair);
    }

    [Test]
    public async Task ConnectedPlayerIsCachedAsSponsor()
    {
        using var bot = new FakeTokenBot();
        await using var pair = await PoolManager.GetServerClient(Settings);
        var server = pair.Server;
        var api = server.ResolveDependency<NebulaServerApi>();
        var sponsors = server.System<SponsorManagerSystem>();

        bot.SponsorRank = 3;
        await Configure(pair, bot);
        await Call(pair, () => api.GetLinkedUserDataOrNull(Guid.NewGuid()));

        ICommonSession dummy = await server.AddDummySession();
        await WaitFor(pair, () => sponsors.IsSponsor(dummy.UserId), "the sponsor status");

        await Finish(pair);
    }
}
