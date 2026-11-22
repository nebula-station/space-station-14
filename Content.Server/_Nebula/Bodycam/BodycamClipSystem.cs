// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Nebula.Bodycam.Replay;
using Content.Shared.Atmos.Components;
using Robust.Shared.Asynchronous;
using Robust.Shared.Audio.Components;
using Robust.Shared.Timing;
using Robust.Shared.Map.Components;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Content.Shared._Nebula.Bodycam;

namespace Content.Server._Nebula.Bodycam;

public sealed partial class BodycamClipJob
{
    public required BodycamClipRequest Request;
    public required EntityUid Console;
    public required ICommonSession Session;
    public required string FileName;
    public BodycamClipResult? Result;
    public string? Error;
}

public sealed partial class BodycamClipSystem : EntitySystem
{
    [Dependency] private IRobustSerializer _serializer = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private ITransferManager _transfer = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ITaskManager _tasks = default!;
    [Dependency] private BodycamFootageConsoleSystem _console = default!;

    private readonly Queue<BodycamClipJob> _queue = new();
    private readonly ConcurrentQueue<BodycamClipJob> _finished = new();
    private readonly ConcurrentQueue<BodycamClipJob> _sent = new();
    private BodycamClipJob? _running;
    private HashSet<ushort>? _hidden;
    private HashSet<ushort>? _gridComponents;

    public override void Initialize()
    {
        base.Initialize();
        _transfer.RegisterTransferMessage(BodycamFootageTransfer.Key, null, NetMessageAccept.Client);
    }

    public HashSet<ushort> HiddenComponents
    {
        get
        {
            if (_hidden != null)
                return _hidden;

            _hidden = new HashSet<ushort>();
            var audio = _factory.GetRegistration<AudioComponent>().NetID;
            foreach (var registration in _factory.GetAllRegistrations())
            {
                if (registration.NetID is not { } netId || netId == audio)
                    continue;

                var component = _factory.GetComponent(registration);
                if (component.SendOnlyToOwner || component.SessionSpecific)
                    _hidden.Add(netId);
            }

            return _hidden;
        }
    }

    public HashSet<ushort> GridComponents
    {
        get
        {
            if (_gridComponents != null)
                return _gridComponents;

            _gridComponents = new HashSet<ushort>();
            if (_factory.GetRegistration<MapGridComponent>().NetID is { } grid)
                _gridComponents.Add(grid);
            if (_factory.GetRegistration<MapComponent>().NetID is { } map)
                _gridComponents.Add(map);
            return _gridComponents;
        }
    }

    public BodycamClipRequest CreateRequest(string sourcePath, GameTick from, GameTick to, NetEntity? camera, string label)
    {
        return new BodycamClipRequest
        {
            SourcePath = sourcePath,
            FromTick = from,
            ToTick = to,
            TickRate = _timing.TickRate,
            HiddenComponents = HiddenComponents,
            GridComponents = GridComponents,
            MapComponent = _factory.GetRegistration<MapComponent>().NetID,
            GasOverlayComponent = _factory.GetRegistration<GasTileOverlayComponent>().NetID,
            UserInterfaceComponent = _factory.GetRegistration<UserInterfaceComponent>().NetID,
            Camera = camera,
            Label = label,
        };
    }

    public bool IsBusy(EntityUid console)
    {
        if (_running?.Console == console)
            return true;

        foreach (var job in _queue)
        {
            if (job.Console == console)
                return true;
        }

        return false;
    }

    public void Enqueue(BodycamClipJob job)
    {
        _queue.Enqueue(job);
        _console.SetStatus(job.Console, BodycamFootageStatus.Queued);
        StartNext();
    }

    public void CutDetached(BodycamClipRequest request, Action<BodycamClipResult?, string?> done)
    {
        var serializer = _serializer;
        StartCutThread(() =>
        {
            BodycamClipResult? result = null;
            string? error = null;
            try
            {
                result = new BodycamClipCutter(serializer).Cut(request);
            }
            catch (BodycamClipException e)
            {
                error = e.Reason;
            }
            catch (Exception e)
            {
                Log.Error($"Bodycam footage cut failed: {e}");
                error = "bodycam-footage-error-internal";
            }

            _tasks.RunOnMainThread(() => done(result, error));
        });
    }

    private static void StartCutThread(Action action)
    {
        var thread = new Thread(() => action())
        {
            Name = "Bodycam footage cutter",
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal,
        };
        thread.Start();
    }

    private void StartNext()
    {
        if (_running != null || !_queue.TryDequeue(out var job))
            return;

        _running = job;
        _console.SetStatus(job.Console, BodycamFootageStatus.Processing);

        var serializer = _serializer;
        StartCutThread(() =>
        {
            try
            {
                job.Result = new BodycamClipCutter(serializer).Cut(job.Request);
            }
            catch (BodycamClipException e)
            {
                job.Error = e.Reason;
            }
            catch (Exception e)
            {
                Log.Error($"Bodycam footage cut failed: {e}");
                job.Error = "bodycam-footage-error-internal";
            }
            finally
            {
                _finished.Enqueue(job);
            }
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        while (_finished.TryDequeue(out var job))
        {
            _running = null;

            if (job.Error != null || job.Result == null)
                _console.SetStatus(job.Console, BodycamFootageStatus.Failed, Loc.GetString(job.Error ?? "bodycam-footage-error-internal"));
            else
                Send(job);

            StartNext();
        }

        while (_sent.TryDequeue(out var job))
        {
            if (job.Error != null)
            {
                _console.SetStatus(job.Console, BodycamFootageStatus.Failed, Loc.GetString(job.Error));
                continue;
            }

            var size = job.Result!.Zip.Count / (1024f * 1024f);
            _console.SetStatus(job.Console, BodycamFootageStatus.Sent, Loc.GetString("bodycam-footage-status-sent-detail", ("size", MathF.Round(size, 1))));
        }
    }

    private void Send(BodycamClipJob job)
    {
        if (job.Session.Status != SessionStatus.InGame || job.Session.Channel is not { IsConnected: true } channel)
        {
            _console.SetStatus(job.Console, BodycamFootageStatus.Failed, Loc.GetString("bodycam-footage-error-disconnected"));
            return;
        }

        _console.SetStatus(job.Console, BodycamFootageStatus.Sending);

        Stream stream;
        try
        {
            stream = _transfer.StartTransfer(channel, BodycamFootageTransfer.Key);
        }
        catch (Exception e)
        {
            Log.Error($"Could not start bodycam footage transfer: {e}");
            _console.SetStatus(job.Console, BodycamFootageStatus.Failed, Loc.GetString("bodycam-footage-error-internal"));
            return;
        }

        var name = Encoding.UTF8.GetBytes(job.FileName);
        var zip = job.Result!.Zip;
        Task.Run(async () =>
        {
            try
            {
                await using (stream)
                {
                    await stream.WriteAsync(BitConverter.GetBytes(name.Length));
                    await stream.WriteAsync(name);
                    await stream.WriteAsync(zip);
                }
            }
            catch (Exception)
            {
                job.Error = "bodycam-footage-error-disconnected";
            }
            finally
            {
                _sent.Enqueue(job);
            }
        });
    }
}
