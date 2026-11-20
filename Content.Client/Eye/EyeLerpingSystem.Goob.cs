// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from https://github.com/Goob-Station/Goob-Station/blob/707e36693d1d9289a4f04921c5e070b1e4195589/Content.Client/Eye/EyeLerpingSystem.Goob.cs

using Content.Shared._ES.Camera;

namespace Content.Client.Eye;

public sealed partial class EyeLerpingSystem
{
    private Angle GetShakeRotation(EntityUid uid)
    {
        var ev = new ESGetEyeRotationEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev.Rotation;
    }
}
