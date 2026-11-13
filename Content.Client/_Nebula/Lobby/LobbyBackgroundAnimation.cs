// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Lobby;

/// <summary>
/// Plays an animated lobby background, ported from Monolith (LuaM).
/// Frames are loaded one per update so opening the lobby doesn't hitch.
/// </summary>
public sealed class LobbyBackgroundAnimation
{
    private const float MinDelay = 0.02f;

    private readonly IResourceCache _resourceCache;
    private readonly List<Texture> _frames = new();
    private readonly List<float> _delays = new();
    private readonly Queue<(ResPath Path, float Delay)> _pending = new();
    private int _frame;
    private float _timer;

    public LobbyBackgroundAnimation(IResourceCache resourceCache)
    {
        _resourceCache = resourceCache;
    }

    /// <summary>
    /// First frame of the animation, used for the blurred backdrop of the panels.
    /// </summary>
    public Texture? FirstFrame => _frames.Count > 0 ? _frames[0] : null;

    public bool Playing => _frames.Count > 0 || _pending.Count > 0;

    public void Stop()
    {
        _frames.Clear();
        _delays.Clear();
        _pending.Clear();
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

        var folder = proto.Background.Directory;
        var extension = proto.Background.Extension;
        for (var i = 0; i < proto.Frames; i++)
        {
            var delay = MathF.Max(MinDelay, i < proto.Delays.Count ? proto.Delays[i] : proto.FrameTime);
            _pending.Enqueue((folder / $"{i:D3}.{extension}", delay));
        }

        LoadPending();
        return FirstFrame;
    }

    /// <summary>
    /// Advances the animation, returns the texture to show when it changed.
    /// </summary>
    public Texture? Update(float frameTime)
    {
        if (_pending.Count > 0)
        {
            LoadPending();
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

    private void LoadPending()
    {
        if (!_pending.TryDequeue(out var next))
            return;

        // a missing frame is skipped instead of breaking the whole animation
        if (!_resourceCache.TryGetResource<TextureResource>(next.Path, out var texture))
            return;

        _frames.Add(texture.Texture);
        _delays.Add(next.Delay);
    }
}
