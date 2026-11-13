// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Nebula.CCVar;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Tests._Nebula.Administration;

[TestFixture]
public sealed class AdminAssistSidecarTest : GameTest
{
    private const string FakeSidecar = """
        import json, os, pathlib, sys, tomllib

        size = int(sys.stdin.buffer.readline())
        cvars = tomllib.loads(sys.stdin.buffer.read(size).decode("utf-8"))
        cvars["pid"] = os.getpid()
        out = pathlib.Path("config.json")
        tmp = pathlib.Path("config.json.tmp")
        tmp.write_text(json.dumps(cvars), encoding="utf-8")
        tmp.replace(out)
        print("fake sidecar up", flush=True)
        sys.stdin.buffer.read()
        pathlib.Path(f"stopped-{os.getpid()}").write_text("1")
        """;

    public override PoolSettings PoolSettings => new()
    {
        Connected = false,
        Dirty = true,
    };

    [Test]
    public async Task SidecarGetsItsConfigRestartsAndStops()
    {
        if (!PythonAvailable())
            Assert.Ignore("python3 is not on PATH");

        var dir = Directory.CreateTempSubdirectory("admin_assist_test_");
        try
        {
            Directory.CreateDirectory(Path.Combine(dir.FullName, "admin_assist"));
            await File.WriteAllTextAsync(Path.Combine(dir.FullName, "admin_assist", "__init__.py"), "");
            await File.WriteAllTextAsync(Path.Combine(dir.FullName, "admin_assist", "__main__.py"), FakeSidecar);

            const string key = "a\"b\\c\nd ё";
            var config = Server.ResolveDependency<IConfigurationManager>();
            await OverrideCVar(Side.Server, NCCVars.AdminAssistSidecarDir, dir.FullName);
            await OverrideCVar(Side.Server, NCCVars.AdminAssistKidMeshKey, key);
            await OverrideCVar(Side.Server, NCCVars.AdminAssistRoundStateToken, "");
            await OverrideCVar(Side.Server, NCCVars.AdminAssistSidecar, true);

            var configFile = Path.Combine(dir.FullName, "config.json");
            var first = await WaitForConfig(configFile, previousPid: null);
            var token = config.GetCVar(NCCVars.AdminAssistRoundStateToken);
            var sidecar = first.GetProperty("admin_assist");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sidecar.GetProperty("kidmesh_key").GetString(), Is.EqualTo(key));
                Assert.That(token, Has.Length.EqualTo(64));
                Assert.That(sidecar.GetProperty("round_state_token").GetString(), Is.EqualTo(token));
                Assert.That(sidecar.GetProperty("round_state_url").GetString(), Does.StartWith("http://").And.EndWith("/admin_assist/round_state"));
                Assert.That(sidecar.GetProperty("rules_file").GetString(), Is.EqualTo("rules/nebula.ru.txt"));
                Assert.That(sidecar.GetProperty("sidecar").GetBoolean(), Is.True);
                Assert.That(sidecar.GetProperty("price_rub_per_mtok").GetDouble(), Is.EqualTo(4.35));
                Assert.That(first.GetProperty("adminlogs").TryGetProperty("server_name", out _), Is.True);
                Assert.That(first.GetProperty("nebula").TryGetProperty("isolate_records", out _), Is.True);
            }

            var firstPid = first.GetProperty("pid").GetInt32();
            Process.GetProcessById(firstPid).Kill();

            var second = await WaitForConfig(configFile, firstPid);
            var secondPid = second.GetProperty("pid").GetInt32();
            Assert.That(second.GetProperty("admin_assist").GetProperty("round_state_token").GetString(), Is.EqualTo(token),
                "a restart must keep the token the round-state endpoint checks");

            await OverrideCVar(Side.Server, NCCVars.AdminAssistSidecar, false);
            await OverrideCVar(Side.Server, NCCVars.AdminAssistSidecar, true);
            var third = await WaitForConfig(configFile, secondPid);
            var thirdPid = third.GetProperty("pid").GetInt32();
            Assert.That(File.Exists(Path.Combine(dir.FullName, $"stopped-{secondPid}")), Is.True,
                "the old sidecar has to be gone before a new one starts");

            await OverrideCVar(Side.Server, NCCVars.AdminAssistSidecar, false);
            await WaitFor(() => File.Exists(Path.Combine(dir.FullName, $"stopped-{thirdPid}")), "the sidecar to stop once stdin closed");
        }
        finally
        {
            await OverrideCVar(Side.Server, NCCVars.AdminAssistSidecar, false);
            await Server.WaitRunTicks(5);
            try
            {
                dir.Delete(recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    private async Task<JsonElement> WaitForConfig(string file, int? previousPid)
    {
        JsonElement result = default;
        await WaitFor(() =>
        {
            if (!File.Exists(file))
                return false;

            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            if (previousPid != null && doc.RootElement.GetProperty("pid").GetInt32() == previousPid)
                return false;

            result = doc.RootElement.Clone();
            return true;
        }, previousPid == null ? "the sidecar to start" : "the sidecar to restart");
        return result;
    }

    private async Task WaitFor(Func<bool> condition, string what)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                Assert.Fail($"Timed out waiting for {what}");

            await Server.WaitRunTicks(1);
            await Task.Delay(50);
        }
    }

    private static bool PythonAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("python3", "-c \"import tomllib\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (process == null)
                return false;

            process.WaitForExit(10_000);
            return process.HasExited && process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
