// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Bodycam;

[Serializable, NetSerializable]
public sealed class BodycamReplayStatusEvent : EntityEventArgs
{
    public NetEntity Camera;
    public bool Enabled;

    public BodycamReplayStatusEvent(NetEntity camera, bool enabled)
    {
        Camera = camera;
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class BodycamReplayViewEvent : EntityEventArgs
{
    public NetEntity Camera;
    public NetCoordinates Position;
    public float Range;
    public bool Reset;
    public NetEntity[] Entered;
    public NetEntity[] Left;

    public BodycamReplayViewEvent(NetEntity camera, NetCoordinates position, float range, bool reset, NetEntity[] entered, NetEntity[] left)
    {
        Camera = camera;
        Position = position;
        Range = range;
        Reset = reset;
        Entered = entered;
        Left = left;
    }
}

[Serializable, NetSerializable]
public sealed class BodycamReplayChatEvent : EntityEventArgs
{
    public NetEntity Camera;
    public ChatMessage Message;

    public BodycamReplayChatEvent(NetEntity camera, ChatMessage message)
    {
        Camera = camera;
        Message = message;
    }
}
