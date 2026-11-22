// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Nebula.Bodycam;

[AdminCommand(AdminFlags.Host)]
public sealed partial class BodycamCutCommand : LocalizedEntityCommands
{
    [Dependency] private IResourceManager _resources = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private BodycamClipSystem _clips = default!;

    public override string Command => "bodycam_cut";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 3 or > 4
            || !uint.TryParse(args[1], out var from)
            || !uint.TryParse(args[2], out var to))
        {
            shell.WriteError(Loc.GetString("cmd-bodycam_cut-help"));
            return;
        }

        NetEntity? camera = null;
        if (args.Length == 4)
        {
            if (!NetEntity.TryParse(args[3], out var parsed))
            {
                shell.WriteError(Loc.GetString("cmd-bodycam_cut-help"));
                return;
            }

            camera = parsed;
        }

        var replays = new ResPath(_cfg.GetCVar(CVars.ReplayDirectory)).ToRootedPath();
        var source = replays / args[0];
        if (_resources.UserData.RootDir is not { } root || !_resources.UserData.Exists(source))
        {
            shell.WriteError(Loc.GetString("cmd-bodycam_cut-missing", ("path", source)));
            return;
        }

        var output = replays / "bodycam" / $"{Path.GetFileNameWithoutExtension(args[0])}-{from}-{to}{(camera == null ? "-full" : $"-{camera}")}.zip";
        var request = _clips.CreateRequest(
            Path.Combine(root, source.ToRelativeSystemPath()),
            new GameTick(from),
            new GameTick(to),
            camera,
            args[0]);

        var watch = new Stopwatch();
        watch.Start();
        _clips.CutDetached(request, (result, error) =>
        {
            if (result == null)
            {
                shell.WriteError(Loc.GetString(error ?? "bodycam-footage-error-internal"));
                return;
            }

            _resources.UserData.CreateDir(output.Directory);
            using (var file = _resources.UserData.OpenWrite(output))
            {
                file.Write(result.Zip);
            }

            shell.WriteLine(Loc.GetString("cmd-bodycam_cut-done",
                ("path", output),
                ("size", result.Zip.Count / 1024),
                ("first", result.FirstTick.Value),
                ("last", result.LastTick.Value),
                ("time", watch.Elapsed.TotalSeconds.ToString("0.0"))));
        });
    }
}
