// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Bodycam;

[Serializable, NetSerializable]
public enum BodycamFootageConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum BodycamFootageStatus : byte
{
    Idle,
    Queued,
    Processing,
    Sending,
    Sent,
    Failed,
}

[Serializable, NetSerializable]
public sealed class BodycamFootageSegment
{
    public TimeSpan Start;
    public TimeSpan End;
    public bool Open;
    public string Wearer;

    public BodycamFootageSegment(TimeSpan start, TimeSpan end, bool open, string wearer)
    {
        Start = start;
        End = end;
        Open = open;
        Wearer = wearer;
    }
}

[Serializable, NetSerializable]
public sealed class BodycamFootageCamera
{
    public int Id;
    public string Name;
    public bool Enabled;
    public List<BodycamFootageSegment> Segments;

    public BodycamFootageCamera(int id, string name, bool enabled, List<BodycamFootageSegment> segments)
    {
        Id = id;
        Name = name;
        Enabled = enabled;
        Segments = segments;
    }
}

[Serializable, NetSerializable]
public sealed class BodycamFootageConsoleState : BoundUserInterfaceState
{
    public List<BodycamFootageCamera> Cameras;
    public TimeSpan RoundTime;
    public int MaxMinutes;
    public bool CanUnfiltered;
    public bool Available;
    public BodycamFootageStatus Status;
    public string? StatusText;

    public BodycamFootageConsoleState(
        List<BodycamFootageCamera> cameras,
        TimeSpan roundTime,
        int maxMinutes,
        bool canUnfiltered,
        bool available,
        BodycamFootageStatus status,
        string? statusText)
    {
        Cameras = cameras;
        RoundTime = roundTime;
        MaxMinutes = maxMinutes;
        CanUnfiltered = canUnfiltered;
        Available = available;
        Status = status;
        StatusText = statusText;
    }
}

[Serializable, NetSerializable]
public sealed class BodycamFootageRequestMessage : BoundUserInterfaceMessage
{
    public int CameraId;
    public TimeSpan From;
    public TimeSpan To;
    public bool Unfiltered;

    public BodycamFootageRequestMessage(int cameraId, TimeSpan from, TimeSpan to, bool unfiltered)
    {
        CameraId = cameraId;
        From = from;
        To = to;
        Unfiltered = unfiltered;
    }
}

public static class BodycamFootageTransfer
{
    public const string Key = "NebulaBodycamFootage";
}

[Serializable, NetSerializable]
public sealed class BodycamFootageStateMessage : BoundUserInterfaceMessage
{
    public BodycamFootageConsoleState State;

    public BodycamFootageStateMessage(BodycamFootageConsoleState state)
    {
        State = state;
    }
}
