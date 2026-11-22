// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Content.Client.Launcher;
using Content.Client.Replay;
using Content.Client.Replay.UI.Loading;
using Content.Shared._Nebula.Bodycam;
using Robust.Client;
using Robust.Client.Replays.Loading;
using Robust.Client.Replays.Playback;
using Robust.Client.State;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Utility;
using static Robust.Shared.Replays.ReplayConstants;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client._Nebula.Bodycam;

public sealed partial class BodycamFootageManager
{
    [Dependency] private ITransferManager _transfer = default!;
    [Dependency] private IResourceManager _resources = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ITaskManager _tasks = default!;
    [Dependency] private IBaseClient _client = default!;
    [Dependency] private IClientNetManager _net = default!;
    [Dependency] private IStateManager _state = default!;
    [Dependency] private IReplayLoadManager _load = default!;
    [Dependency] private IReplayPlaybackManager _playback = default!;
    [Dependency] private ContentReplayPlaybackManager _contentPlayback = default!;

    private const int KeptClips = 10;

    private ISawmill _sawmill = default!;
    private (string Host, ushort Port)? _returnTo;
    private bool _watching;

    public string? Latest { get; private set; }

    public event Action<string>? ClipReceived;

    private ResPath Directory => new ResPath(_cfg.GetCVar(CVars.ReplayDirectory)).ToRootedPath() / "bodycam";

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("bodycam");
        _transfer.RegisterTransferMessage(BodycamFootageTransfer.Key, OnReceived, NetMessageAccept.Client);
        _playback.ReplayPlaybackStopped += OnPlaybackStopped;
        _state.OnStateChanged += OnStateChanged;
    }

    private void OnStateChanged(StateChangedEventArgs args)
    {
        if (!_watching || args.NewState is not ReplayLoadingFailed)
            return;

        _watching = false;
        Timer.Spawn(TimeSpan.FromSeconds(5), Reconnect);
    }

    private async void OnReceived(TransferReceivedEvent ev)
    {
        try
        {
            var lengthBytes = new byte[4];
            await ev.DataStream.ReadExactlyAsync(lengthBytes);
            var length = BitConverter.ToInt32(lengthBytes);
            if (length is <= 0 or > 256)
                return;

            var nameBytes = new byte[length];
            await ev.DataStream.ReadExactlyAsync(nameBytes);
            var name = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Sanitize(Encoding.UTF8.GetString(nameBytes))}";
            var dir = Directory;
            _resources.UserData.CreateDir(dir);
            await using (var file = _resources.UserData.OpenWrite(dir / name))
            {
                await ev.DataStream.CopyToAsync(file);
            }

            _tasks.RunOnMainThread(() => Store(name));
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to receive bodycam footage: {e}");
        }
    }

    private void Store(string name)
    {
        var dir = Directory;
        var old = _resources.UserData.DirectoryEntries(dir)
            .Where(x => x.EndsWith(".zip") && x != name)
            .OrderByDescending(x => x)
            .Skip(KeptClips - 1)
            .ToList();
        foreach (var entry in old)
            _resources.UserData.Delete(dir / entry);

        Latest = name;
        ClipReceived?.Invoke(name);
    }

    private static string Sanitize(string name)
    {
        var builder = new StringBuilder(name.Length);
        foreach (var c in name)
            builder.Append(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_');

        var result = builder.ToString().TrimStart('.');
        return result.EndsWith(".zip") ? result : result + ".zip";
    }

    public void WatchLatest()
    {
        if (Latest == null || _watching)
            return;

        var path = Directory / Latest;
        if (!_resources.UserData.Exists(path))
            return;

        if (_net.ServerChannel?.RemoteEndPoint is { } endpoint)
            _returnTo = (endpoint.Address.ToString(), (ushort) endpoint.Port);

        _watching = true;
        _client.DisconnectFromServer(Loc.GetString("bodycam-footage-disconnect-reason"));
        Timer.Spawn(TimeSpan.FromMilliseconds(500), () => StartReplay(path, 20));
    }

    private void StartReplay(ResPath path, int attempts)
    {
        if (_client.RunLevel != ClientRunLevel.Initialize)
        {
            if (attempts > 0)
                Timer.Spawn(TimeSpan.FromMilliseconds(250), () => StartReplay(path, attempts - 1));
            else
                _watching = false;
            return;
        }

        try
        {
            var reader = new ReplayFileReaderZip(new ZipArchive(_resources.UserData.OpenRead(path)), ReplayZipFolder);
            _contentPlayback.LastLoad = (path, ReplayZipFolder.ToRootedPath());
            _load.LoadAndStartReplay(reader);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to open bodycam footage {path}: {e}");
            _watching = false;
            Reconnect();
        }
    }

    private void OnPlaybackStopped()
    {
        if (!_watching)
            return;

        _watching = false;
        Timer.Spawn(TimeSpan.FromMilliseconds(500), Reconnect);
    }

    private void Reconnect()
    {
        if (_client.RunLevel != ClientRunLevel.Initialize)
            return;

        if (_state.CurrentState is LauncherConnecting launcher)
        {
            launcher.RetryConnect();
            return;
        }

        if (_returnTo is { } target)
            _client.ConnectToServer(target.Host, target.Port);
    }
}
