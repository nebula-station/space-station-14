// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Nebula.ReadyManifest;
using Content.Server.GameTicking;
using Content.Shared._Nebula.ReadyManifest;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Nebula.ReadyManifest;

[TestFixture]
[TestOf(typeof(ReadyManifestSystem))]
public sealed class ReadyManifestTest : GameTest
{
    private static readonly ProtoId<JobPrototype> Engineer = "StationEngineer";
    private static readonly ProtoId<JobPrototype> Doctor = "MedicalDoctor";
    private static readonly ProtoId<JobPrototype> Captain = "Captain";

    public override PoolSettings PoolSettings => new()
    {
        DummyTicker = false,
        Connected = true,
        InLobby = true,
    };

    /// <summary>
    /// Pairs return to the pool only with every player not ready.
    /// </summary>
    [TearDown]
    public async Task ResetReady()
    {
        await SetReady(false);
    }

    [Test]
    public async Task HighPriorityJobListed()
    {
        await Pair.SetJobPriorities((Engineer, JobPriority.High), (Doctor, JobPriority.Medium));
        await SetReady(true);

        var jobs = (await GetState()).Entries.Select(entry => entry.Job).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(jobs, Does.Contain(Engineer));
            Assert.That(jobs, Does.Not.Contain(Doctor));
        }
    }

    [Test]
    public async Task HeadJobListedAtAnyPriority()
    {
        await Pair.SetJobPriorities((Captain, JobPriority.Low));
        await SetReady(true);

        var jobs = (await GetState()).Entries.Select(entry => entry.Job).ToList();

        Assert.That(jobs, Does.Contain(Captain));
    }

    [Test]
    public async Task NotReadyPlayerExcluded()
    {
        await Pair.SetJobPriorities((Engineer, JobPriority.High));
        await SetReady(true);

        var ready = await GetState();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ready.ReadyCount, Is.EqualTo(1));
            Assert.That(ready.Entries, Is.Not.Empty);
        }

        await SetReady(false);

        var notReady = await GetState();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(notReady.ReadyCount, Is.Zero);
            Assert.That(notReady.Entries, Is.Empty);
        }
    }

    private async Task SetReady(bool ready)
    {
        var ticker = Pair.Server.System<ServerGameTicker>();
        var session = Pair.Server.PlayerMan.GetSessionById(Pair.Client.User!.Value);

        await Pair.Server.WaitPost(() => ticker.ToggleReady(session, ready));
    }

    private async Task<ReadyManifestEuiState> GetState()
    {
        ReadyManifestEuiState? state = null;
        await Pair.Server.WaitPost(() => state = Pair.Server.System<ReadyManifestSystem>().GetState());
        return state!;
    }
}
