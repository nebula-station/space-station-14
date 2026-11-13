// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Nebula.Administration.Systems;
using NUnit.Framework;

namespace Content.Tests.Server._Nebula;

[TestFixture, TestOf(typeof(AdminAssistSidecarSystem))]
[Parallelizable(ParallelScope.All)]
public sealed class AdminAssistSidecarTomlTest
{
    [Test]
    public void ValuesAreWrittenAsTomlTables()
    {
        var toml = AdminAssistSidecarSystem.BuildToml(new (string, object)[]
        {
            ("admin_assist.poll_s", 2f),
            ("admin_assist.price_rub_per_mtok", 4.35f),
            ("admin_assist.budget_rub", 500f),
            ("admin_assist.max_parallel_calls", 3),
            ("admin_assist.sidecar", true),
            ("admin_assist.kidmesh_key", "a\"b\\c\nd\te\u0001ё"),
            ("nebula.isolate_records", false),
            ("adminlogs.server_name", "pegas"),
            ("nodot", "skipped"),
        });

        Assert.That(toml, Is.EqualTo(
            "[admin_assist]\n" +
            "budget_rub = 500.0\n" +
            "kidmesh_key = \"a\\\"b\\\\c\\nd\\te\\u0001ё\"\n" +
            "max_parallel_calls = 3\n" +
            "poll_s = 2.0\n" +
            "price_rub_per_mtok = 4.35\n" +
            "sidecar = true\n" +
            "[adminlogs]\n" +
            "server_name = \"pegas\"\n" +
            "[nebula]\n" +
            "isolate_records = false\n"));
    }

    [Test]
    public void OddKeysAndSpecialFloatsStayValid()
    {
        var toml = AdminAssistSidecarSystem.BuildToml(new (string, object)[]
        {
            ("t.a.b", float.NaN),
            ("t.inf", double.PositiveInfinity),
            ("t.big", 1e20f),
        });

        Assert.That(toml, Is.EqualTo("[t]\n\"a.b\" = nan\nbig = 1E+20\ninf = inf\n"));
    }

    [TestCase("", 1212, "http://127.0.0.1:1212/admin_assist/round_state")]
    [TestCase("*:1212", 1, "http://127.0.0.1:1212/admin_assist/round_state")]
    [TestCase("+:1300", 1, "http://127.0.0.1:1300/admin_assist/round_state")]
    [TestCase("0.0.0.0:1400", 1, "http://127.0.0.1:1400/admin_assist/round_state")]
    [TestCase("[::]:1500", 1, "http://127.0.0.1:1500/admin_assist/round_state")]
    [TestCase("203.0.113.5:1600", 1, "http://203.0.113.5:1600/admin_assist/round_state")]
    [TestCase("localhost:1700", 1, "http://localhost:1700/admin_assist/round_state")]
    [TestCase("garbage", 1984, "http://127.0.0.1:1984/admin_assist/round_state")]
    public void RoundStateUrlFollowsTheStatusBind(string bind, int netPort, string expected)
    {
        Assert.That(AdminAssistSidecarSystem.RoundStateUrl(bind, netPort), Is.EqualTo(expected));
    }
}
