// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server._Nebula.Bodycam.Replay;
using Content.Shared._Nebula.Bodycam;
using Content.Shared._Nebula.CCVar;
using Content.Shared.Access.Systems;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.Bodycam;

public sealed partial class BodycamFootageConsoleSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IReplayRecordingManager _replay = default!;
    [Dependency] private IResourceManager _resources = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ServerGameTicker _ticker = default!;
    [Dependency] private BodycamSystem _bodycam = default!;
    [Dependency] private BodycamClipSystem _clips = default!;

    private readonly Dictionary<EntityUid, (BodycamFootageStatus Status, string? Text)> _status = new();
    private TimeSpan _nextRefresh;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodycamFootageConsoleComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<BodycamFootageConsoleComponent, BoundUIOpenedEvent>(OnOpened);
        SubscribeLocalEvent<BodycamFootageConsoleComponent, BodycamFootageRequestMessage>(OnRequest);
        SubscribeLocalEvent<BodycamFootageConsoleComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<BodycamFootageConsoleComponent> ent, ref ComponentShutdown args)
    {
        _status.Remove(ent);
    }

    private bool IsAdmin(EntityUid user)
    {
        return _admin.HasAdminFlag(user, AdminFlags.Admin);
    }

    private bool CanUse(EntityUid user, EntityUid console)
    {
        return IsAdmin(user) || _access.IsAllowed(user, console);
    }

    private void OnOpenAttempt(Entity<BodycamFootageConsoleComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (CanUse(args.User, ent))
            return;

        args.Cancel();
        if (args.Silent)
            return;

        _popup.PopupEntity(Loc.GetString("bodycam-footage-access-denied"), ent, args.User);
        _audio.PlayPvs(ent.Comp.DenySound, ent);
    }

    private void OnOpened(Entity<BodycamFootageConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateState(ent, args.Actor);
    }

    public void SetStatus(EntityUid console, BodycamFootageStatus status, string? text = null)
    {
        _status[console] = (status, text);
        UpdateState(console);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextRefresh)
            return;

        _nextRefresh = _timing.CurTime + TimeSpan.FromSeconds(2);
        var query = EntityQueryEnumerator<BodycamFootageConsoleComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_ui.IsUiOpen(uid, BodycamFootageConsoleUiKey.Key))
                UpdateState(uid);
        }
    }

    private TimeSpan TickToRoundTime(GameTick tick)
    {
        var now = _ticker.RoundDuration();
        var ticksAgo = (long) _timing.CurTick.Value - tick.Value;
        var time = now - _timing.TickPeriod * ticksAgo;
        return time < TimeSpan.Zero ? TimeSpan.Zero : time;
    }

    private GameTick RoundTimeToTick(TimeSpan time)
    {
        var now = _ticker.RoundDuration();
        var ticksAgo = (long) Math.Ceiling((now - time).TotalSeconds * _timing.TickRate);
        return new GameTick((uint) Math.Max(0, (long) _timing.CurTick.Value - ticksAgo));
    }

    private void UpdateState(EntityUid console, EntityUid? viewer = null)
    {
        var cameras = new List<BodycamFootageCamera>();
        foreach (var record in _bodycam.Records.Values.OrderBy(r => r.Id))
        {
            var segments = record.Segments
                .Select(s => new BodycamFootageSegment(
                    TickToRoundTime(s.Start),
                    TickToRoundTime(s.End ?? _timing.CurTick),
                    s.End == null,
                    s.Wearer))
                .ToList();
            cameras.Add(new BodycamFootageCamera(record.Id, record.Name, _bodycam.IsActive(record.Camera), segments));
        }

        var (status, text) = _status.GetValueOrDefault(console, (BodycamFootageStatus.Idle, null));
        var available = _replay.IsRecording && _ticker.CurrentReplayPath != null;

        var actors = viewer != null ? new[] { viewer.Value } : _ui.GetActors(console, BodycamFootageConsoleUiKey.Key).ToArray();
        foreach (var actor in actors)
        {
            var admin = IsAdmin(actor);
            var actorMax = admin
                ? _cfg.GetCVar(NCCVars.BodycamAdminMaxClipMinutes)
                : _cfg.GetCVar(NCCVars.BodycamMaxClipMinutes);
            _ui.ServerSendUiMessage(console, BodycamFootageConsoleUiKey.Key, new BodycamFootageStateMessage(
                new BodycamFootageConsoleState(cameras, _ticker.RoundDuration(), actorMax, admin, available, status, text)), actor);
        }
    }

    private void Fail(EntityUid console, string reason)
    {
        SetStatus(console, BodycamFootageStatus.Failed, Loc.GetString(reason));
    }

    private void OnRequest(Entity<BodycamFootageConsoleComponent> ent, ref BodycamFootageRequestMessage args)
    {
        var user = args.Actor;
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        if (!CanUse(user, ent))
        {
            Fail(ent, "bodycam-footage-access-denied");
            return;
        }

        if (_clips.IsBusy(ent))
            return;

        if (!_bodycam.TryGetRecord(args.CameraId, out var record))
        {
            Fail(ent, "bodycam-footage-error-camera");
            return;
        }

        if (!_replay.IsRecording || _ticker.CurrentReplayPath is not { } replayPath || _resources.UserData.RootDir is not { } root)
        {
            Fail(ent, "bodycam-footage-error-unavailable");
            return;
        }

        var admin = IsAdmin(user);
        var max = admin
            ? _cfg.GetCVar(NCCVars.BodycamAdminMaxClipMinutes)
            : _cfg.GetCVar(NCCVars.BodycamMaxClipMinutes);

        var now = _ticker.RoundDuration();
        var from = args.From < TimeSpan.Zero ? TimeSpan.Zero : args.From;
        var to = args.To > now ? now : args.To;
        if (to <= from)
        {
            Fail(ent, "bodycam-footage-error-range");
            return;
        }

        if ((to - from).TotalMinutes > max)
        {
            SetStatus(ent, BodycamFootageStatus.Failed, Loc.GetString("bodycam-footage-error-too-long", ("max", max)));
            return;
        }

        var unfiltered = args.Unfiltered && admin;
        var request = _clips.CreateRequest(
            Path.Combine(root, replayPath.ToRelativeSystemPath()),
            RoundTimeToTick(from),
            RoundTimeToTick(to),
            unfiltered ? null : record.Camera,
            $"{record.Name} {FormatTime(from)}–{FormatTime(to)}");

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} requested {(unfiltered ? "unfiltered " : "")}bodycam footage of {record.Name} from {FormatTime(from)} to {FormatTime(to)}");

        _clips.Enqueue(new BodycamClipJob
        {
            Request = request,
            Console = ent,
            Session = actor.PlayerSession,
            FileName = $"round{_ticker.RoundId}-bodycam{record.Id}-{(int) from.TotalSeconds}-{(int) to.TotalSeconds}{(unfiltered ? "-full" : "")}.zip",
        });
    }

    public static string FormatTime(TimeSpan time)
    {
        return $"{(int) time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
    }
}
