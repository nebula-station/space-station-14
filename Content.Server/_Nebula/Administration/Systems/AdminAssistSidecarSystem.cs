// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Content.Shared._Nebula.CCVar;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.Administration.Systems;

public sealed partial class AdminAssistSidecarSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IGameTiming _timing = default!;

    public const string PackagedDir = "admin_assist";
    public const string RoundStatePath = "/admin_assist/round_state";

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StableRun = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(10);

    private static readonly string[] ExtraCVars = ["adminlogs.server_name", "nebula.isolate_records"];

    private ISawmill _sawmill = default!;
    private bool _wanted;
    private Process? _process;
    private Task? _stopping;
    private TimeSpan _startedAt;
    private TimeSpan _nextCheck;
    private TimeSpan _nextStart;
    private int _failures;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("admin_assist.sidecar");
        Subs.CVar(_config, NCCVars.AdminAssistSidecar, OnToggled, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        Stop(wait: true);
        _stopping?.Wait(StopTimeout * 2);
    }

    public void Supervise()
    {
        var now = _timing.RealTime;
        if (now < _nextCheck)
            return;

        _nextCheck = now + CheckInterval;

        if (_process is { } process)
        {
            if (!process.HasExited)
                return;

            OnExited(process, now);
        }

        // Новый сайдкар ждёт, пока старый закончит останавливаться, иначе оба разберут одни и те же тикеты.
        if (_stopping is { IsCompleted: false })
            return;

        if (_wanted && now >= _nextStart)
            Start(now);
    }

    private void OnToggled(bool value)
    {
        _wanted = value;
        _failures = 0;
        _nextStart = TimeSpan.Zero;

        if (!value)
            Stop(wait: false);
    }

    private void Start(TimeSpan now)
    {
        var dir = SidecarDir();
        if (!File.Exists(Path.Combine(dir, "admin_assist", "__main__.py")))
        {
            _sawmill.Error($"Admin Assist sidecar not found in {dir}; set {NCCVars.AdminAssistSidecarDir.Name}");
            Retry(now);
            return;
        }

        var python = _config.GetCVar(NCCVars.AdminAssistSidecarPython);
        var info = new ProcessStartInfo
        {
            FileName = python,
            WorkingDirectory = dir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            ArgumentList = { "-m", "admin_assist", "--config-stdin" },
        };
        info.Environment["PYTHONUNBUFFERED"] = "1";
        info.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
        info.Environment["PYTHONIOENCODING"] = "utf-8";
        info.Environment.Remove("PYTHONSAFEPATH");

        Process process;
        try
        {
            process = Process.Start(info) ?? throw new InvalidOperationException("no process was started");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Cannot start the Admin Assist sidecar with {python} ({NCCVars.AdminAssistSidecarPython.Name}): {e.Message}");
            Retry(now);
            return;
        }

        var outLevel = LogLevel.Info;
        var errLevel = LogLevel.Info;
        process.OutputDataReceived += (_, args) => Forward(args.Data, ref outLevel);
        process.ErrorDataReceived += (_, args) => Forward(args.Data, ref errLevel);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        _process = process;
        _startedAt = now;
        _sawmill.Info($"Admin Assist sidecar started from {dir}, pid {process.Id}");

        try
        {
            var config = Encoding.UTF8.GetBytes(BuildToml(SidecarCVars()));
            var stdin = process.StandardInput.BaseStream;
            stdin.Write(Encoding.ASCII.GetBytes($"{config.Length}\n"));
            stdin.Write(config);
            stdin.Flush();
        }
        catch (IOException e)
        {
            _sawmill.Warning($"Admin Assist sidecar exited before it got its config: {e.Message}");
        }
    }

    private void OnExited(Process process, TimeSpan now)
    {
        _process = null;
        var code = process.ExitCode;
        process.Dispose();

        if (!_wanted)
            return;

        if (now - _startedAt >= StableRun)
            _failures = 0;

        var delay = Retry(now);
        _sawmill.Warning($"Admin Assist sidecar exited with code {code}, restarting in {delay.TotalSeconds:0} s");
    }

    private TimeSpan Retry(TimeSpan now)
    {
        var delay = TimeSpan.FromTicks(Math.Min(MaxBackoff.Ticks, MinBackoff.Ticks << Math.Min(_failures, 16)));
        _failures++;
        _nextStart = now + delay;
        return delay;
    }

    private void Stop(bool wait)
    {
        if (_process is not { } process)
            return;

        _process = null;
        _sawmill.Info("Stopping the Admin Assist sidecar");

        try
        {
            process.StandardInput.Close();
        }
        catch (Exception e) when (e is IOException or InvalidOperationException)
        {
        }

        if (wait)
            WaitOrKill(process);
        else
            _stopping = Task.Run(() => WaitOrKill(process));
    }

    private void WaitOrKill(Process process)
    {
        try
        {
            if (!process.WaitForExit(StopTimeout))
            {
                _sawmill.Warning($"Admin Assist sidecar did not stop in {StopTimeout.TotalSeconds:0} s, killing it");
                process.Kill(entireProcessTree: true);
                process.WaitForExit(StopTimeout);
            }
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            process.Dispose();
        }
    }

    private void Forward(string? line, ref LogLevel level)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        var parts = line.Split(' ', 4);
        if (parts.Length == 4 && ParseLevel(parts[2]) is { } parsed)
        {
            level = parsed;
            line = parts[3];
        }

        _sawmill.Log(level, line);
    }

    private static LogLevel? ParseLevel(string level)
    {
        return level switch
        {
            "DEBUG" => LogLevel.Debug,
            "INFO" => LogLevel.Info,
            "WARNING" => LogLevel.Warning,
            "ERROR" => LogLevel.Error,
            "CRITICAL" => LogLevel.Fatal,
            _ => null,
        };
    }

    private string SidecarDir()
    {
        var dir = _config.GetCVar(NCCVars.AdminAssistSidecarDir);
        return Path.GetFullPath(dir == string.Empty ? Path.Combine(AppContext.BaseDirectory, PackagedDir) : dir);
    }

    private List<(string Name, object Value)> SidecarCVars()
    {
        if (_config.GetCVar(NCCVars.AdminAssistRoundStateToken) == string.Empty)
            _config.SetCVar(NCCVars.AdminAssistRoundStateToken, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant());

        var cvars = _config.GetRegisteredCVars()
            .Where(name => name.StartsWith("admin_assist.", StringComparison.Ordinal) || ExtraCVars.Contains(name))
            .Select(name => (name, _config.GetCVar(name)))
            .ToList();

        if (_config.GetCVar(NCCVars.AdminAssistRoundStateUrl) == string.Empty)
        {
            cvars.RemoveAll(cvar => cvar.Item1 == NCCVars.AdminAssistRoundStateUrl.Name);
            cvars.Add((NCCVars.AdminAssistRoundStateUrl.Name, RoundStateUrl(_config.GetCVar(CVars.StatusBind), _config.GetCVar(CVars.NetPort))));
        }

        return cvars;
    }

    public static string RoundStateUrl(string bind, int netPort)
    {
        var host = "127.0.0.1";
        var port = netPort;
        var colon = bind.LastIndexOf(':');
        if (colon >= 0 && int.TryParse(bind.AsSpan(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var bindPort))
        {
            port = bindPort;
            var bindHost = bind[..colon];
            if (bindHost is not ("" or "*" or "+" or "0.0.0.0" or "[::]"))
                host = bindHost;
        }

        return $"http://{host}:{port}{RoundStatePath}";
    }

    public static string BuildToml(IEnumerable<(string Name, object Value)> cvars)
    {
        var builder = new StringBuilder();
        foreach (var table in cvars
                     .Select(cvar => (Split: cvar.Name.Split('.', 2), cvar.Value))
                     .Where(cvar => cvar.Split.Length == 2)
                     .GroupBy(cvar => cvar.Split[0])
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            builder.Append('[').Append(TomlKey(table.Key)).Append("]\n");
            foreach (var (split, value) in table.OrderBy(cvar => cvar.Split[1], StringComparer.Ordinal))
            {
                builder.Append(TomlKey(split[1])).Append(" = ").Append(TomlValue(value)).Append('\n');
            }
        }

        return builder.ToString();
    }

    private static string TomlKey(string key)
    {
        return key.Length > 0 && key.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-') ? key : TomlString(key);
    }

    private static string TomlValue(object value)
    {
        return value switch
        {
            bool b => b ? "true" : "false",
            int or long or short or byte => Convert.ToString(value, CultureInfo.InvariantCulture)!,
            float f => TomlFloat(f, f.ToString("R", CultureInfo.InvariantCulture)),
            double d => TomlFloat(d, d.ToString("R", CultureInfo.InvariantCulture)),
            _ => TomlString(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty),
        };
    }

    private static string TomlFloat(double value, string text)
    {
        if (double.IsNaN(value))
            return "nan";
        if (double.IsInfinity(value))
            return value > 0 ? "inf" : "-inf";

        return text.Contains('.') || text.Contains('E') ? text : text + ".0";
    }

    private static string TomlString(string value)
    {
        var builder = new StringBuilder(value.Length + 2).Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\b': builder.Append("\\b"); break;
                case '\t': builder.Append("\\t"); break;
                case '\n': builder.Append("\\n"); break;
                case '\f': builder.Append("\\f"); break;
                case '\r': builder.Append("\\r"); break;
                case < ' ' or '\u007f': builder.Append("\\u").Append(((int) c).ToString("X4")); break;
                default: builder.Append(c); break;
            }
        }

        return builder.Append('"').ToString();
    }
}
