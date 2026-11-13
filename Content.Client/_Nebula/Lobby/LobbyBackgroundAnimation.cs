// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;
using Content.Shared.GameTicking.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._Nebula.Lobby;

/// <summary>
/// Plays an animated lobby background, ported from Monolith (LuaM).
/// </summary>
public sealed partial class LobbyBackgroundAnimation
{
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IResourceManager _resources = default!;

    private const float MinDelay = 0.02f;
    private const int MaxDecoding = 2;

    private readonly List<Texture> _frames = [];
    private readonly List<float> _delays = [];
    private readonly List<OwnedTexture> _owned = [];
    private readonly Queue<(ResPath Path, float Delay)> _pending = [];
    private readonly Queue<(Task<Image<Rgba32>?> Decode, float Delay)> _decoding = [];
    private CancellationTokenSource? _cancel;
    private int _frame;
    private float _timer;

    public LobbyBackgroundAnimation()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// First frame of the animation, used for the blurred backdrop of the panels.
    /// </summary>
    public Texture? FirstFrame => _frames.Count > 0 ? _frames[0] : null;

    public bool Playing => _frames.Count > 0 || _pending.Count > 0 || _decoding.Count > 0;

    public void Stop()
    {
        _cancel?.Cancel();
        _cancel?.Dispose();
        _cancel = null;

        while (_decoding.TryDequeue(out var decoding))
        {
            DisposeWhenDone(decoding.Decode);
        }

        _pending.Clear();

        foreach (var texture in _owned)
        {
            texture.Dispose();
        }

        _owned.Clear();
        _frames.Clear();
        _delays.Clear();
        _frame = 0;
        _timer = 0f;
    }

    /// <summary>
    /// Starts the animation and returns its first frame, or null when the prototype is static or has no frames.
    /// </summary>
    public Texture? Start(LobbyBackgroundPrototype proto)
    {
        Stop();

        if (!proto.Animated)
            return null;

        if (!_resourceCache.TryGetResource<TextureResource>(proto.Background, out var first))
            return null;

        _frames.Add(first.Texture);
        _delays.Add(GetDelay(proto, 0));

        _cancel = new CancellationTokenSource();
        var folder = proto.Background.Directory;
        var extension = proto.Background.Extension;

        for (var i = 1; i < proto.Frames; i++)
        {
            _pending.Enqueue((folder / $"{i:D3}.{extension}", GetDelay(proto, i)));
        }

        StartDecodes();

        return FirstFrame;
    }

    /// <summary>
    /// Advances the animation, returns the texture to show when it changed.
    /// </summary>
    public Texture? Update(float frameTime)
    {
        if (_decoding.Count > 0)
        {
            UploadNext();
            return null;
        }

        if (_frames.Count < 2)
            return null;

        _timer += frameTime;
        var changed = false;
        while (_timer >= _delays[_frame])
        {
            _timer -= _delays[_frame];
            _frame = (_frame + 1) % _frames.Count;
            changed = true;
        }

        return changed ? _frames[_frame] : null;
    }

    private void UploadNext()
    {
        if (!_decoding.TryPeek(out var next) || !next.Decode.IsCompleted)
            return;

        _decoding.Dequeue();
        StartDecodes();
        if (!next.Decode.IsCompletedSuccessfully || next.Decode.GetAwaiter().GetResult() is not { } image)
            return;

        using (image)
        {
            var texture = _clyde.LoadTextureFromImage(image);
            _owned.Add(texture);
            _frames.Add(texture);
            _delays.Add(next.Delay);
        }
    }

    private void StartDecodes()
    {
        if (_cancel == null)
            return;

        var token = _cancel.Token;
        while (_decoding.Count < MaxDecoding && _pending.TryDequeue(out var next))
        {
            var path = next.Path;
            _decoding.Enqueue((Task.Run(() => Decode(path, token), token), next.Delay));
        }
    }

    private Image<Rgba32>? Decode(ResPath path, CancellationToken token)
    {
        if (token.IsCancellationRequested || !_resources.TryContentFileRead(path, out var stream))
            return null;

        using (stream)
        {
            return Image.Load<Rgba32>(stream);
        }
    }

    private static float GetDelay(LobbyBackgroundPrototype proto, int frame)
    {
        return MathF.Max(MinDelay, frame < proto.Delays.Count ? proto.Delays[frame] : proto.FrameTime);
    }

    private static void DisposeWhenDone(Task<Image<Rgba32>?> decode)
    {
        decode.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
                task.GetAwaiter().GetResult()?.Dispose();
        });
    }
}
