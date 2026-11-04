// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable

using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Server._Nebula.AntagTokens;
using Content.Server.GameTicking;
using Content.Shared._Nebula.AntagTokens;
using Content.Shared._Nebula.CCVar;
using Content.Shared.Antag.Components;
using Content.Shared.GameTicking.Prototypes;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using ClientAntagTokenSystem = Content.Client._Nebula.AntagTokens.AntagTokenSystem;

namespace Content.IntegrationTests.Tests._Nebula.AntagTokens;

[TestFixture]
[TestOf(typeof(AntagTokenSystem))]
public sealed class AntagTokenTest
{
    private const string Preset = "Greenshift";
    private static readonly ProtoId<JobPrototype> Passenger = "Passenger";
    private static readonly ProtoId<SpeciesPrototype> Human = "Human";

    private static readonly string[] Listings =
    [
        "Changeling", "Devil", "Heretic", "InitialInfected", "Spy", "Thief", "Traitor", "Vampire",
    ];

    /// <summary>
    /// Thresholds of the original rules: the single player versions must not change them.
    /// </summary>
    private static readonly (string Wrapper, string Parent)[] WrappedRules =
    [
        ("AntagTokenThiefRule", "Thief"),
        ("AntagTokenTraitorRule", "Traitor"),
        ("AntagTokenInitialInfectedRule", "Zombie"),
    ];

    private static PoolSettings RoundSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        InLobby = true,
    };

    [Test]
    public async Task ListingsGrantASinglePlayer()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var factory = server.ResolveDependency<IComponentFactory>();
        var protoMan = server.ProtoMan;

        await server.WaitAssertion(() =>
        {
            var ids = protoMan.EnumeratePrototypes<AntagTokenListingPrototype>().Select(l => l.ID).OrderBy(i => i);
            Assert.That(ids, Is.EqualTo(Listings));

            foreach (var listing in protoMan.EnumeratePrototypes<AntagTokenListingPrototype>())
            {
                var rule = protoMan.Index(listing.Rule);

                Assert.That(protoMan.HasIndex(listing.Antag), listing.ID);
                Assert.That(rule.TryComp<AntagSelectionComponent>(out var selection, factory), listing.ID);
                Assert.That(selection!.LateJoinAdditional, Is.False, $"{listing.ID} would pick late joiners");
                Assert.That(selection.Antags, Is.Not.Empty, listing.ID);
                Assert.That(rule.Components.ContainsKey(listing.RuleComp), listing.ID);

            }

            foreach (var (wrapper, parent) in WrappedRules)
            {
                protoMan.Index(wrapper).TryComp<GameRuleComponent>(out var wrapped, factory);
                protoMan.Index(parent).TryComp<GameRuleComponent>(out var original, factory);
                Assert.That(wrapped?.MinPlayers, Is.EqualTo(original?.MinPlayers), wrapper);
            }

            var presets = server.ResolveDependency<IConfigurationManager>().GetCVar(NCCVars.AntagTokensPresets);
            foreach (var preset in presets.Split(','))
            {
                Assert.That(protoMan.HasIndex<GamePresetPrototype>(preset), preset);
            }
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task OldCurrencyCommandsAreGone()
    {
        await using var pair = await PoolManager.GetServerClient();
        var host = pair.Server.ResolveDependency<IConsoleHost>();

        Assert.That(host.AvailableCommands.Keys, Does.Not.Contain("balanceui"));
        Assert.That(host.AvailableCommands.Keys, Does.Not.Contain("wipecurrency"));
        Assert.That(host.AvailableCommands.Values.Where(c => c.GetType().Namespace!.Contains("ServerCurrency")),
            Is.Empty);

        await pair.CleanReturnAsync();
    }

    private static async Task<(TestPair Pair, AntagTokenSystem Tokens, ICommonSession Player)> Start(FakeTokenBot bot,
        float windowMax = 0f)
    {
        var pair = await PoolManager.GetServerClient(RoundSettings);
        var server = pair.Server;
        var cfg = server.ResolveDependency<IConfigurationManager>();

        await server.WaitPost(() =>
        {
            cfg.SetCVar(NCCVars.ApiToken, "test-key");
            cfg.SetCVar(NCCVars.ApiUrl, bot.Url);
            cfg.SetCVar(NCCVars.AntagTokensServerId, "test");
            cfg.SetCVar(NCCVars.AntagTokensPresets, Preset);
            cfg.SetCVar(NCCVars.AntagTokensWindowMin, 0f);
            cfg.SetCVar(NCCVars.AntagTokensWindowMax, windowMax);
            cfg.SetCVar(NCCVars.AntagTokensRetryInterval, 0.1f);
        });

        // The startup refund runs once per server and pooled servers are reused between tests.
        var tokens = server.System<AntagTokenSystem>();
        await WaitFor(pair, () => tokens.StartupRefundDone, "startup refund");
        return (pair, tokens, pair.Player!);
    }

    private static async Task Finish(TestPair pair)
    {
        var cfg = pair.Server.ResolveDependency<IConfigurationManager>();
        await pair.Server.WaitPost(() =>
        {
            cfg.SetCVar(NCCVars.ApiToken, string.Empty);
            cfg.SetCVar(NCCVars.ApiUrl, string.Empty);
            cfg.SetCVar(NCCVars.AntagTokensServerId, string.Empty);
        });
        await pair.CleanReturnAsync();
        await pair.DisposeAsync();
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

    private static async Task Buy(TestPair pair, AntagTokenSystem tokens, ICommonSession player, string listing)
    {
        Task? task = null;
        await pair.Server.WaitPost(() => task = tokens.Buy(player, listing));
        await WaitFor(pair, () => task is { IsCompleted: true }, $"purchase of {listing}");
    }

    private static async Task StartRound(TestPair pair)
    {
        var ticker = pair.Server.System<ServerGameTicker>();
        await pair.Server.WaitPost(() =>
        {
            ticker.SetGamePreset(Preset);
            ticker.ToggleReadyAll(true);
            ticker.StartRound();
        });
        await pair.RunTicksSync(10);
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
    }

    private static int RequiredPlayers(TestPair pair, string listing)
    {
        var tokens = pair.Server.System<AntagTokenSystem>();
        return tokens.GetMinPlayers(pair.Server.ProtoMan.Index<AntagTokenListingPrototype>(listing));
    }

    [TestCaseSource(nameof(Listings))]
    public async Task GrantsExactlyOnePlayer(string listing)
    {
        using var bot = new FakeTokenBot();
        var (pair, tokens, player) = await Start(bot);
        var user = player.UserId.ToString();
        bot.Balances[user] = 1;

        // Some antagonists forbid certain jobs and species, and that is a refund by design. Neither must be random here.
        await pair.SetJobPriority(Passenger, JobPriority.High);
        await pair.SetSpecies(Human);

        var dummies = await pair.Server.AddDummySessions(Math.Max(RequiredPlayers(pair, listing), 2));
        await pair.RunTicksSync(5);

        await Buy(pair, tokens, player, listing);
        Assert.That(tokens.HasPurchase(player.UserId), $"{listing} was not bought");
        Assert.That(bot.Balances[user], Is.Zero);

        await StartRound(pair);
        await WaitFor(pair, () => !tokens.HasPurchase(player.UserId), "the grant");
        await WaitFor(pair, () => bot.Granted.Contains(user) || bot.Refunded.Contains(user), "the bot call");

        var role = pair.Server.System<SharedRoleSystem>();
        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(bot.Granted, Does.Contain(user), $"{listing} was refunded instead of granted");
            Assert.That(role.PlayerIsAntagonist(player), listing);
            Assert.That(dummies.Where(role.PlayerIsAntagonist), Is.Empty, $"{listing} also picked other players");
        });

        await Finish(pair);
    }

    [Test]
    public async Task OtherPresetRefunds()
    {
        using var bot = new FakeTokenBot();
        var (pair, tokens, player) = await Start(bot);
        var user = player.UserId.ToString();
        bot.Balances[user] = 1;

        await pair.Server.WaitPost(() => pair.Server.ResolveDependency<IConfigurationManager>()
            .SetCVar(NCCVars.AntagTokensPresets, "Dynamic"));

        await Buy(pair, tokens, player, "Thief");
        await StartRound(pair);
        await WaitFor(pair, () => bot.Refunded.Contains(user), "the refund");

        Assert.That(bot.Balances[user], Is.EqualTo(1));
        Assert.That(tokens.HasPurchase(player.UserId), Is.False);
        Assert.That(pair.Server.System<SharedRoleSystem>().PlayerIsAntagonist(player), Is.False);

        await Finish(pair);
    }

    [Test]
    public async Task NotEnoughPlayersClosesThePurchase()
    {
        using var bot = new FakeTokenBot();
        var (pair, tokens, player) = await Start(bot);
        var user = player.UserId.ToString();
        bot.Balances[user] = 1;

        Assert.That(RequiredPlayers(pair, "Traitor"), Is.GreaterThan(1));

        await Buy(pair, tokens, player, "Traitor");

        Assert.That(tokens.HasPurchase(player.UserId), Is.False);
        Assert.That(bot.Balances[user], Is.EqualTo(1));

        await Finish(pair);
    }

    [Test]
    public async Task UnlinkedPlayerCannotBuy()
    {
        using var bot = new FakeTokenBot();
        var (pair, tokens, player) = await Start(bot);

        await Buy(pair, tokens, player, "Thief");

        Assert.That(tokens.HasPurchase(player.UserId), Is.False);

        await Finish(pair);
    }

    [Test]
    public async Task EmptyBalanceCannotBuy()
    {
        using var bot = new FakeTokenBot();
        var (pair, tokens, player) = await Start(bot);
        bot.Balances[player.UserId.ToString()] = 0;

        await Buy(pair, tokens, player, "Thief");

        Assert.That(tokens.HasPurchase(player.UserId), Is.False);

        await Finish(pair);
    }

    [Test]
    public async Task CancelReturnsToken()
    {
        using var bot = new FakeTokenBot();
        var (pair, tokens, player) = await Start(bot);
        var user = player.UserId.ToString();
        bot.Balances[user] = 1;

        await Buy(pair, tokens, player, "Thief");
        await pair.Server.WaitPost(() => tokens.Cancel(player));
        await WaitFor(pair, () => bot.Refunded.Contains(user), "the refund");

        Assert.That(tokens.HasPurchase(player.UserId), Is.False);
        Assert.That(bot.Balances[user], Is.EqualTo(1));

        await Finish(pair);
    }

    [Test]
    public async Task LostPurchaseOfThisServerIsRefunded()
    {
        using var bot = new FakeTokenBot();
        var (pair, tokens, player) = await Start(bot);
        var user = player.UserId.ToString();
        bot.Balances[user] = 0;
        bot.AddPurchase(user, Guid.NewGuid(), "Thief", "test");

        await pair.Server.WaitPost(() => tokens.Cancel(player));
        await WaitFor(pair, () => bot.Refunded.Contains(user), "the refund");

        Assert.That(bot.Balances[user], Is.EqualTo(1));
        Assert.That(bot.ActivePurchases, Is.Empty);

        await Finish(pair);
    }

    [Test]
    public async Task PurchaseOfOtherServerIsNotRefunded()
    {
        using var bot = new FakeTokenBot();
        var (pair, tokens, player) = await Start(bot);
        var user = player.UserId.ToString();
        bot.Balances[user] = 0;
        bot.AddPurchase(user, Guid.NewGuid(), "Thief", "other");

        await pair.Server.WaitPost(() => tokens.Cancel(player));
        await WaitFor(pair, () => bot.StatusRequestsAnswered > 0, "the status request");
        await pair.RunTicksSync(10);

        Assert.That(bot.Refunded, Is.Empty);
        Assert.That(bot.ActivePurchases, Is.Not.Empty);

        await Finish(pair);
    }

    [Test]
    public async Task HeldPurchaseIsNotRefundedByStateUpdate()
    {
        using var bot = new FakeTokenBot();
        var (pair, tokens, player) = await Start(bot);
        var user = player.UserId.ToString();
        bot.Balances[user] = 1;

        await Buy(pair, tokens, player, "Thief");

        Assert.That(bot.StatusRequestsAnswered, Is.GreaterThan(0));
        Assert.That(bot.Refunded, Is.Empty);
        Assert.That(tokens.HasPurchase(player.UserId));

        await Finish(pair);
    }

    [Test]
    public async Task StateRequestsAreThrottled()
    {
        using var bot = new FakeTokenBot();
        var (pair, _, player) = await Start(bot);
        var client = pair.Client.System<ClientAntagTokenSystem>();
        bot.Balances[player.UserId.ToString()] = 1;

        await pair.Client.WaitPost(() =>
        {
            for (var i = 0; i < 5; i++)
            {
                client.RequestState();
            }
        });

        await WaitFor(pair, () => bot.StatusRequestsAnswered > 0, "the status request");

        await Task.Delay(200);
        await pair.RunTicksSync(10);

        Assert.That(bot.StatusRequestsAnswered, Is.EqualTo(1));

        await Finish(pair);
    }

    [Test]
    public async Task ActionDuringCooldownIsRejectedWithoutBot()
    {
        using var bot = new FakeTokenBot();
        var (pair, tokens, player) = await Start(bot);
        var client = pair.Client.System<ClientAntagTokenSystem>();
        bot.Balances[player.UserId.ToString()] = 1;

        await pair.Client.WaitPost(() => client.Buy("Thief"));
        await WaitFor(pair, () => client.State?.ActiveListing != null, "the purchase");

        var requests = bot.StatusRequestsAnswered;

        await pair.Client.WaitPost(() => client.Cancel());
        await WaitFor(pair, () => client.State?.Error == AntagTokenError.Busy, "the rejection");

        Assert.That(tokens.HasPurchase(player.UserId));
        Assert.That(bot.Refunded, Is.Empty);
        Assert.That(bot.StatusRequestsAnswered, Is.EqualTo(requests));

        await Finish(pair);
    }

    [Test]
    public async Task ClientLearnsAboutRunLevelChange()
    {
        using var bot = new FakeTokenBot();
        var (pair, _, player) = await Start(bot);
        var client = pair.Client.System<ClientAntagTokenSystem>();
        bot.Balances[player.UserId.ToString()] = 1;

        await pair.Client.WaitPost(() => client.RequestState());
        await WaitFor(pair, () => client.State != null, "the state");
        Assert.That(client.State!.InLobby);

        await StartRound(pair);
        await WaitFor(pair, () => client.State is { InLobby: false }, "the run level message");

        await Finish(pair);
    }

    [Test]
    public async Task CannotBuyDuringRound()
    {
        using var bot = new FakeTokenBot();
        var (pair, tokens, player) = await Start(bot);
        var user = player.UserId.ToString();
        bot.Balances[user] = 1;

        await StartRound(pair);
        await Buy(pair, tokens, player, "Thief");

        Assert.That(tokens.HasPurchase(player.UserId), Is.False);
        Assert.That(bot.Balances[user], Is.EqualTo(1));

        await Finish(pair);
    }

    [Test]
    public async Task GrantTimeCountsFromThisRound()
    {
        using var bot = new FakeTokenBot();
        var (pair, tokens, player) = await Start(bot, windowMax: 10f);
        var user = player.UserId.ToString();
        bot.Balances[user] = 1;

        await pair.Server.WaitPost(() => pair.Server.ResolveDependency<IConfigurationManager>()
            .SetCVar(NCCVars.AntagTokensWindowMin, 10f));

        await Buy(pair, tokens, player, "Thief");
        await StartRound(pair);

        var ticker = pair.Server.System<ServerGameTicker>();
        Assert.That(tokens.HasPurchase(player.UserId), "the antagonist must not be granted at the round start");
        Assert.That(tokens.GetGrantTime(player.UserId),
            Is.EqualTo(ticker.RoundStartTimeSpan + TimeSpan.FromMinutes(10)));

        await pair.Server.WaitPost(() => ticker.EndRound());
        await WaitFor(pair, () => bot.Refunded.Contains(user), "the refund");

        await Finish(pair);
    }

    [Test]
    public async Task RoundEndRefundsWaitingPurchase()
    {
        using var bot = new FakeTokenBot();
        // The grant moment is far away, so the purchase is still waiting when the round ends.
        var (pair, tokens, player) = await Start(bot, windowMax: 1000f);
        var user = player.UserId.ToString();
        bot.Balances[user] = 1;

        await pair.Server.WaitPost(() => pair.Server.ResolveDependency<IConfigurationManager>()
            .SetCVar(NCCVars.AntagTokensWindowMin, 1000f));

        await Buy(pair, tokens, player, "Thief");
        await StartRound(pair);
        Assert.That(tokens.HasPurchase(player.UserId));

        await pair.Server.WaitPost(() => pair.Server.System<ServerGameTicker>().EndRound());
        await WaitFor(pair, () => bot.Refunded.Contains(user), "the refund");

        Assert.That(bot.Balances[user], Is.EqualTo(1));

        await Finish(pair);
    }
}
