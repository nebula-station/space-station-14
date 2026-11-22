// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Content.Server._Nebula.Bodycam.Replay;
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
            foreach (var registration in _factory.GetAllRegistrations())
            {
                if (registration.NetID is not { } netId)
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

    public ushort? MapComponentId => _factory.GetRegistration<MapComponent>().NetID;

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

    public BodycamClipResult CutNow(BodycamClipRequest request)
    {
        return new BodycamClipCutter(_serializer).Cut(request);
    }

    private void StartNext()
    {
        if (_running != null || !_queue.TryDequeue(out var job))
            return;

        _running = job;
        _console.SetStatus(job.Console, BodycamFootageStatus.Processing);

        var serializer = _serializer;
        Task.Run(() =>
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

            var size = job.Result!.Zip.Length / (1024f * 1024f);
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
