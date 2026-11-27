// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.IntegrationTests/Tests/_Arcane/JoinQueueNetworkTest.cs

#nullable enable
using System.Diagnostics;
using System.Linq;
using Content.Client._Arcane.JoinQueue;
using Content.Goobstation.Common.CCVar;
using Content.IntegrationTests.Fixtures;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Players;
using Content.Shared.Station.Components;
using Content.Shared._Arcane.JoinQueue;
using Robust.Client.Console;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Player;
using ServerQueue = Content.Server._Arcane.JoinQueue.JoinQueueSystem;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class JoinQueueNetworkTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, DummyTicker = false, Dirty = true };

    public override async Task DoTeardown()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != global::NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            // A Connected queue client receives no game states. The base cleanup's PVS sync cannot complete there.
            await Client.WaitPost(() =>
            {
                var net = Client.ResolveDependency<IClientNetManager>();
                if (net.ServerChannel is { IsConnected: true })
                    net.ClientDisconnect("Queue test cleanup");
            });
            await Pair.ReallyBeIdle(5);
            await Client.WaitAssertion(() => Assert.That(Client.Session, Is.Null));
        }
        await base.DoTeardown();
    }

    [TestCase(false)]
    public async Task RoundRestartAndReadyCommandsCannotAdmitAWaitingConnection(bool lobby)
    {
        var (_, session) = await ConnectQueued();
        await Server.WaitPost(() =>
        {
            Server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars.GameLobbyEnabled, lobby);
            SEntMan.System<ServerGameTicker>().RestartRound();
        });
        await Pair.ReallyBeIdle(20);
        await Server.WaitAssertion(() =>
        {
            var ticker = SEntMan.System<ServerGameTicker>();
            ticker.ToggleReady(session, true);
            Assert.That(ticker.PlayerGameStatuses.ContainsKey(session.UserId), Is.False);
            Assert.That(ticker.ReadyPlayerCount(), Is.Zero);
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Connected));
            Assert.That(session.AttachedEntity, Is.Null);
            Assert.That(SEntMan.System<ServerQueue>().PlayerInQueueCount, Is.EqualTo(1));
        });
        await Client.WaitAssertion(() => Assert.That(Client.ResolveDependency<IStateManager>().CurrentState, Is.TypeOf<QueueState>()));
    }

    [TestCase(true)]
    public async Task WaitingLobbyReconnectCannotJoinThroughConsole(bool observe)
    {
        await Server.WaitPost(() =>
        {
            Server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars.GameLobbyEnabled, true);
            var ticker = SEntMan.System<ServerGameTicker>();
            ticker.RestartRound();
            ticker.PauseStart();
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        });
        var (name, initial) = await ConnectQueued();
        await Server.WaitPost(() => Server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars.SoftMaxPlayers, 1));
        await WaitServer(() => initial.Status == SessionStatus.InGame);

        var command = "observe";
        await Server.WaitAssertion(() =>
        {
            var ticker = SEntMan.System<ServerGameTicker>();
            Assert.That(ticker.PlayerGameStatuses[initial.UserId], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
            ticker.SetGamePreset(ticker.FindGamePreset("sandbox")!);
            ticker.StartRound(force: true);
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            Assert.That(initial.GetMind(), Is.Null);
            if (!observe)
            {
                var jobs = SEntMan.System<ServerStationJobsSystem>();
                var station = SEntMan.System<ServerStationSystem>().GetStations().Single(s =>
                    SEntMan.HasComponent<StationJobsComponent>(s) && jobs.TryGetJobSlot(s, "Passenger", out var slots) && slots != 0);
                command = $"joingame Passenger {SEntMan.GetNetEntity(station.Owner).Id}";
            }
            Server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars.SoftMaxPlayers, 0);
        });

        var waiting = await Reconnect(name, initial, SessionStatus.Connected);
        await Server.WaitAssertion(() => Assert.That(SEntMan.System<ServerGameTicker>().PlayerGameStatuses[waiting.UserId],
            Is.EqualTo(PlayerGameStatus.NotReadyToPlay), "Reconnect must retain the genuine lobby status for this regression."));
        await Client.WaitPost(() => Client.ResolveDependency<IClientConsoleHost>().RemoteExecuteCommand(null, command));
        await Pair.ReallyBeIdle(5);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<ServerGameTicker>().UserHasJoinedGame(waiting.UserId), Is.False,
                "A command from an unadmitted connection must not grant returning-player priority.");
            Assert.That(waiting.AttachedEntity, Is.Null);
            Assert.That(waiting.GetMind(), Is.Null);
            Assert.That(SEntMan.System<ServerQueue>().PlayerInQueueCount, Is.EqualTo(1));
            Assert.That(SEntMan.System<ServerQueue>().ActualPlayersCount, Is.Zero);
        });

        // Reconnect again at the full cap: the rejected command must not have created a bypass.
        waiting = await Reconnect(name, waiting, SessionStatus.Connected);
        await Server.WaitPost(() => Server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars.SoftMaxPlayers, 1));
        await WaitServer(() => waiting.Status == SessionStatus.InGame);
        await Client.WaitPost(() => Client.ResolveDependency<IClientConsoleHost>().RemoteExecuteCommand(null, command));
        await WaitServer(() => SEntMan.System<ServerGameTicker>().UserHasJoinedGame(waiting.UserId)
            && waiting.AttachedEntity != null);

        // A player who actually joined the round still keeps the existing returning-player bypass.
        await Server.WaitPost(() => Server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars.SoftMaxPlayers, 0));
        var returned = await Reconnect(name, waiting, SessionStatus.InGame);
        await Server.WaitAssertion(() =>
        {
            Assert.That(returned.AttachedEntity, Is.Not.Null);
            Assert.That(SEntMan.System<ServerQueue>().PlayerInQueueCount, Is.Zero);
        });
    }

    [TestCase(true)]
    public async Task ReconnectWithoutBypassKeepsFifoAcrossRealSessions(bool interruptAdmission)
    {
        var (name, initial) = await ConnectQueued();
        double previousAverage = -1;
        await Server.WaitPost(() => previousAverage = SEntMan.System<ServerQueue>().AverageQueueWait);
        await using var other = await PoolManager.GetServerClient(new PoolSettings { Connected = false, Dirty = true });
        var otherName = "queue_second_" + Guid.NewGuid().ToString("N")[..10];
        other.Client.SetConnectTarget(Server);
        await other.Client.WaitPost(() => other.Client.ResolveDependency<IClientNetManager>().ClientConnect(null!, 0, otherName));
        ICommonSession? second = null;
        await PumpUntil(() =>
        {
            second = Server.ResolveDependency<IPlayerManager>().Sessions.FirstOrDefault(s => s.Name == otherName);
            return second != null && second.Status == SessionStatus.Connected
                && Server.ResolveDependency<UserDbDataManager>().IsLoadComplete(second)
                && SEntMan.System<ServerQueue>().PlayerInQueueCount == 2;
        });

        // Execute between reservation and the next timer tick through public configuration/network entry points.
        await Server.WaitAssertion(() =>
        {
            var cfg = Server.ResolveDependency<IConfigurationManager>();
            var queue = SEntMan.System<ServerQueue>();
            if (interruptAdmission)
            {
                cfg.SetCVar(CCVars.SoftMaxPlayers, 1);
                Assert.That(initial.Status, Is.EqualTo(SessionStatus.Connected));
                Assert.That(queue.ActualPlayersCount, Is.EqualTo(1), "The pending admission reserves one slot.");
                Assert.That(SEntMan.System<ServerGameTicker>().AdmittedPlayerCount, Is.EqualTo(1), "Admission snapshots must not wait for the UI broadcast.");
                cfg.SetCVar(CCVars.SoftMaxPlayers, 0);
            }
            initial.Channel.Disconnect("FIFO reconnect regression");
            Server.ResolveDependency<IServerNetManager>().ProcessPackets();
            Assert.That(initial.Status, Is.EqualTo(SessionStatus.Disconnected));
            Assert.That(queue.ActualPlayersCount, Is.Zero);
            Assert.That(SEntMan.System<ServerGameTicker>().AdmittedPlayerCount, Is.Zero);
            Assert.That(queue.AverageQueueWait, Is.EqualTo(previousAverage), "A cancelled admission is not a completed wait.");
        });
        await PumpUntil(() => initial.Status == SessionStatus.Disconnected);
        await Client.WaitAssertion(() => Assert.That(Client.Session, Is.Null));
        await Client.WaitPost(() => Client.ResolveDependency<IClientNetManager>().ClientConnect(null!, 0, name));
        ICommonSession? replacement = null;
        await PumpUntil(() =>
        {
            replacement = Server.ResolveDependency<IPlayerManager>().Sessions.FirstOrDefault(s => s.UserId == initial.UserId);
            return replacement != null && replacement != initial && replacement.Status == SessionStatus.Connected
                && Server.ResolveDependency<UserDbDataManager>().IsLoadComplete(replacement)
                && SEntMan.System<ServerQueue>().PlayerInQueueCount == 2;
        });
        await Server.WaitPost(() => Server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars.SoftMaxPlayers, 1));
        await PumpUntil(() => replacement!.Status == SessionStatus.InGame || second!.Status == SessionStatus.InGame);
        await Server.WaitAssertion(() =>
        {
            Assert.That(replacement!.Status, Is.EqualTo(SessionStatus.InGame), "Reconnect must preserve the first FIFO place.");
            Assert.That(second!.Status, Is.EqualTo(SessionStatus.Connected));
            Assert.That(SEntMan.System<ServerQueue>().ActualPlayersCount, Is.EqualTo(1));
            Server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars.SoftMaxPlayers, 2);
        });
        await PumpUntil(() => second!.Status == SessionStatus.InGame);
        await other.Client.WaitPost(() => other.Client.ResolveDependency<IClientNetManager>().ClientDisconnect("FIFO cleanup"));
        await PumpUntil(() => second!.Status == SessionStatus.Disconnected);
        await other.Client.WaitAssertion(() => Assert.That(other.Client.Session, Is.Null));
        await other.Server.WaitAssertion(() => Assert.That(other.Server.ResolveDependency<IPlayerManager>().Sessions, Is.Empty));
        other.Client.SetConnectTarget(other.Server);
        await other.CleanReturnAsync();

        async Task PumpUntil(Func<bool> predicate)
        {
            for (var i = 0; i < 200; i++)
            {
                await Pair.ReallyBeIdle(2);
                await other.Client.WaitRunTicks(2);
                await other.Client.WaitIdleAsync();
                var done = false;
                await Server.WaitPost(() => done = predicate());
                if (done)
                    return;
            }
            Assert.Fail("The two-client queue transition did not complete.");
        }
    }

    private async Task<ICommonSession> Reconnect(string name, ICommonSession previous, SessionStatus expected)
    {
        await Client.WaitPost(() => Client.ResolveDependency<IClientNetManager>().ClientDisconnect("Queue admission regression"));
        await WaitServer(() => previous.Status == SessionStatus.Disconnected);
        await Client.WaitPost(() => Client.ResolveDependency<IClientNetManager>().ClientConnect(null!, 0, name));
        ICommonSession? current = null;
        await WaitServer(() =>
        {
            current = Server.ResolveDependency<IPlayerManager>().Sessions.FirstOrDefault(s =>
                s.UserId == previous.UserId && s != previous && s.Status == expected);
            return current != null && Server.ResolveDependency<UserDbDataManager>().IsLoadComplete(current);
        });
        await Pair.ReallyBeIdle(5);
        return current!;
    }

    private async Task<(string Name, ICommonSession Session)> ConnectQueued()
    {
        await Server.WaitPost(() =>
        {
            var cfg = Server.ResolveDependency<IConfigurationManager>();
            cfg.SetCVar(CCVars.ConsoleLoginLocal, false);
            cfg.SetCVar(CCVars.AdminBypassMaxPlayers, false);
            cfg.SetCVar(CCVars.SoftMaxPlayers, 0);
            cfg.SetCVar(GoobCVars.QueueEnabled, true);
        });
        var name = "queued_" + Guid.NewGuid().ToString("N")[..12];
        Client.SetConnectTarget(Server);
        await Client.WaitPost(() => Client.ResolveDependency<IClientNetManager>().ClientConnect(null!, 0, name));
        ICommonSession? session = null;
        await WaitServer(() =>
        {
            session = Server.ResolveDependency<IPlayerManager>().Sessions.FirstOrDefault();
            return session != null && session.Status == SessionStatus.Connected
                && Server.ResolveDependency<UserDbDataManager>().IsLoadComplete(session)
                && SEntMan.System<ServerQueue>().PlayerInQueueCount == 1;
        });
        // UI coalescing uses real uptime. Advancing simulation ticks does not advance the production stopwatch.
        await WaitClientRealTime(() => Client.ResolveDependency<IStateManager>().CurrentState is QueueState,
            "The initial queue update did not reach the client.");
        return (name, session!);
    }

    private async Task WaitClientRealTime(Func<bool> predicate, string failure)
    {
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < TimeSpan.FromSeconds(5))
        {
            await Pair.ReallyBeIdle(2);
            var done = false;
            await Client.WaitPost(() => done = predicate());
            if (done)
                return;
            // Poll the real clock boundary without blocking either simulation thread.
            await Task.Delay(25);
        }
        Assert.Fail(failure);
    }

    private async Task WaitServer(Func<bool> predicate)
    {
        for (var i = 0; i < 200; i++)
        {
            var done = false;
            await Pair.ReallyBeIdle(2);
            await Server.WaitPost(() => done = predicate());
            if (done)
                return;
        }
        Assert.Fail("The queue lifecycle transition did not complete.");
    }
}
