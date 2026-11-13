// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using System.Linq;
using Content.Client._Nebula.Administration;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Database;
using Content.Shared._Nebula.Administration;
using Content.Shared.Database;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Nebula.Administration;

[TestFixture]
public sealed class AdminAssistBriefingTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Connected = false,
        Dirty = true,
    };

    [Test]
    public async Task OneVotePerAdminPerBriefing()
    {
        var db = Server.ResolveDependency<IServerDbManager>();
        var admin = Guid.NewGuid();
        var other = Guid.NewGuid();

        AHelpBriefingVote Vote(Guid who, long summary, short vote) => new()
        {
            SummaryId = summary,
            AdminUserId = who,
            Vote = vote,
            VotedAt = DateTime.UtcNow,
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await db.AddAHelpBriefingVote(Vote(admin, 7, 1)), Is.True);
            Assert.That(await db.AddAHelpBriefingVote(Vote(admin, 7, -1)), Is.False);
            Assert.That(await db.AddAHelpBriefingVote(Vote(other, 7, -1)), Is.True);
            Assert.That(await db.AddAHelpBriefingVote(Vote(admin, 8, -1)), Is.True);

            Assert.That(await db.GetAHelpBriefingVotes(admin, [7, 8, 9]), Is.EquivalentTo(new long[] { 7, 8 }));
            Assert.That(await db.GetAHelpBriefingVotes(other, [8]), Is.Empty);
        }
    }

    [Test]
    public async Task TicketRequestsAreStored()
    {
        var db = Server.ResolveDependency<IServerDbManager>();
        var server = await db.AddOrGetServer("request-test");
        var player = Guid.NewGuid();
        await db.UpdatePlayerRecordAsync(new NetUserId(player), "requester", System.Net.IPAddress.Loopback, null);
        var (roundId, _) = await db.AddNewRound(server, player);

        foreach (var (kind, round) in new[] { (AHelpTicketRequestKind.Close, (int?) roundId), (AHelpTicketRequestKind.Briefing, null) })
        {
            Assert.DoesNotThrowAsync(() => db.AddAHelpTicketRequest(new AHelpTicketRequest
            {
                RoundId = round,
                ServerId = server.Id,
                TicketOwnerUserId = player,
                AdminUserId = Guid.NewGuid(),
                Kind = kind,
                RequestedAt = DateTime.UtcNow,
            }));
        }
    }

    [Test]
    public async Task NoBriefingsWithoutTheSidecarSchema()
    {
        var db = Server.ResolveDependency<IServerDbManager>();

        Assert.That(await db.GetAdminAssistBriefings(1, 0, 50), Is.Empty);
    }

    [Test]
    public async Task DraftsComeFromTheBriefingFields()
    {
        const string question = "Приоритет: НИЗКИЙ\nТип: вопрос\nСуть: можно ли казнить без суда\nПравила: п. 4.2: \"Казнь только по приговору.\"";
        const string noRule = "Приоритет: НИЗКИЙ\nТип: вопрос\nСуть: где взять карту\nПравила: явно применимого правила нет";
        const string bug = "Приоритет: НИЗКИЙ\nТип: баг\nСуть: застрял в стене";
        const string complaint = "Приоритет: ОБЫЧНЫЙ\nТип: жалоба\nСуть: Bob утверждает, что его убил Alice\nЛоги: [L3]";

        await Client.WaitAssertion(() =>
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(AdminAssistBriefingControl.ReplyDraft(question), Does.Contain("п. 4.2: \"Казнь только по приговору.\""));
                Assert.That(AdminAssistBriefingControl.ReplyDraft(noRule),
                    Is.EqualTo(loc.GetString("admin-assist-reply-draft-default")));
                Assert.That(AdminAssistBriefingControl.ReplyDraft(bug),
                    Is.EqualTo(loc.GetString("admin-assist-reply-draft-bug")));
                Assert.That(AdminAssistBriefingControl.ReplyDraft(complaint),
                    Is.EqualTo(loc.GetString("admin-assist-reply-draft-default")));

                var briefing = new AdminAssistBriefingEvent(new NetUserId(Guid.NewGuid()), 1, 42, DateTime.UtcNow,
                    complaint, "ОБЫЧНЫЙ", false, new(), [], [], false);
                var note = AdminAssistBriefingControl.NoteDraft(briefing);
                Assert.That(note, Does.Contain("42"));
                Assert.That(note, Does.Contain("Bob утверждает, что его убил Alice"));
            }
        });
    }

    [Test]
    public void ChannelListRanksTicketsByPriority()
    {
        var ranked = new[] { "НИЗКИЙ", null, "ВЫСОКИЙ", "СРОЧНО!!!", "ОБЫЧНЫЙ" }
            .OrderBy(AdminAssistPriority.Rank)
            .ToList();
        Assert.That(ranked, Is.EqualTo(new[] { "ВЫСОКИЙ", "ОБЫЧНЫЙ", "НИЗКИЙ", null, "СРОЧНО!!!" }));
        Assert.That(AdminAssistPriority.Rank("СРОЧНО!!!"), Is.LessThan(AdminAssistPriority.NoTicket));

        var colors = new[] { AdminAssistPriority.High, AdminAssistPriority.Normal, AdminAssistPriority.Low };
        Assert.That(colors.Distinct().Count(), Is.EqualTo(3));
        Assert.That(AdminAssistPriority.StripeColor("ВЫСОКИЙ"), Is.EqualTo(AdminAssistPriority.High));
        Assert.That(AdminAssistPriority.StripeColor("СРОЧНО!!!"), Is.EqualTo(AdminAssistPriority.None));
    }

    [Test]
    public void MarksComeFromCheckedCitations()
    {
        var bob = new NetUserId(Guid.NewGuid());
        const string text = "Суть: Bob утверждает, что его убил Alice\n"
                            + "Логи: Alice ударила Bob [L3]\n"
                            + "Расхождения: Bob говорит, что не нападал, но [L2] показывает удар; Bob говорит, что был в баре\n"
                            + "Оценка: неясно [L9]";
        var cited = new Dictionary<string, string> { ["L2"] = "Bob hit Alice", ["L3"] = "Alice hit Bob" };
        var names = new Dictionary<string, NetUserId> { ["Bob"] = bob };

        var withNames = AdminAssistBriefingControl.Format(1, text, cited, ["L2", "L3"], names);
        var noLogs = AdminAssistBriefingControl.Format(1, text, new Dictionary<string, string>(), ["L2", "L3"], names);
        var nothingChecked = AdminAssistBriefingControl.Format(1, text, cited, [], names);

        string Marks(Robust.Shared.Utility.FormattedMessage msg) => string.Concat(msg.Nodes
            .Where(n => n.Name == null)
            .Select(n => n.Value.StringValue ?? "")
            .Where(t => t is "✔ " or "✖ " or "? "));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Marks(withNames), Is.EqualTo("✔ ✖ ? "));
            Assert.That(Marks(noLogs), Is.EqualTo("✔ ✖ ? "));
            Assert.That(Marks(nothingChecked), Is.EqualTo("? ? ? "));

            var players = withNames.Nodes.Where(n => n.Name == AdminAssistPlayerTag.TagName && !n.Closing).ToList();
            Assert.That(players, Has.Count.EqualTo(4));
            Assert.That(players.All(n => n.Attributes["user"].StringValue == bob.UserId.ToString()));
            Assert.That(withNames.Nodes.Count(n => n.Name == AdminAssistCiteTag.TagName && !n.Closing), Is.EqualTo(2));
        }
    }

    [Test]
    public async Task TimelineKeepsTheImportantLinesOfTheParticipants()
    {
        var db = Server.ResolveDependency<IServerDbManager>();
        var author = Guid.NewGuid();
        var other = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        foreach (var user in new[] { author, other, stranger })
        {
            await db.UpdatePlayerRecordAsync(new NetUserId(user), $"p{user:N}"[..12], System.Net.IPAddress.Loopback, null);
        }

        var server = await db.AddOrGetServer("timeline-test");
        var (roundId, _) = await db.AddNewRound(server, author, other, stranger);
        var t0 = new DateTime(2026, 11, 12, 20, 0, 0, DateTimeKind.Utc);

        AdminLog Log(int id, LogImpact impact, LogType type, int minute, params Guid[] players) => new()
        {
            RoundId = roundId,
            Id = id,
            Type = type,
            Impact = impact,
            Date = t0.AddMinutes(minute),
            Message = $"line {id}",
            Json = System.Text.Json.JsonDocument.Parse("{}"),
            Players = players.Select(p => new AdminLogPlayer { RoundId = roundId, LogId = id, PlayerUserId = p }).ToList(),
        };

        await db.AddAdminLogs([
            Log(1, LogImpact.High, LogType.Damaged, 1, author, other),
            Log(2, LogImpact.Medium, LogType.Damaged, 2, other),
            Log(3, LogImpact.Low, LogType.Damaged, 3, author),
            Log(4, LogImpact.High, LogType.Chat, 4, author),
            Log(5, LogImpact.High, LogType.Erp, 5, author),
            Log(6, LogImpact.Extreme, LogType.Damaged, 6, stranger),
            Log(7, LogImpact.Extreme, LogType.Damaged, 30, author),
            Log(8, LogImpact.Extreme, LogType.Damaged, 7, author),
            Log(9, LogImpact.High, LogType.AdminCommands, 1, author),
            Log(10, LogImpact.Medium, LogType.LateJoin, 1, author, other),
            Log(11, LogImpact.Medium, LogType.Identity, 1, other),
            Log(12, LogImpact.Medium, LogType.Action, 2, author),
        ]);

        var lines = await db.GetAdminAssistTimeline(roundId, [author, other], t0, t0.AddMinutes(10), 12);
        Assert.That(lines.Select(l => l.Id), Is.EqualTo(new[] { 1, 8, 2 }));

        var capped = await db.GetAdminAssistTimeline(roundId, [author, other], t0, t0.AddMinutes(10), 2);
        Assert.That(capped.Select(l => l.Id), Is.EqualTo(new[] { 1, 8 }));
    }
}
