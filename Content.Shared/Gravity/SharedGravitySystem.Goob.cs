// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from https://github.com/Goob-Station/Goob-Station/blob/707e36693d1d9289a4f04921c5e070b1e4195589/Content.Shared/Gravity/SharedGravitySystem.Goob.cs

using Content.Shared._ES.Camera;
using Content.Shared.GameTicking;
using Robust.Shared.Player;

namespace Content.Shared.Gravity;

public abstract partial class SharedGravitySystem
{
    [Dependency] private GameTicker _ticker = default!; // Nebula edit
    [Dependency] private ESScreenshakeSystem _shake = default!;

    private static readonly TimeSpan RoundStartShakeGrace = TimeSpan.FromSeconds(30);
    private static readonly ESScreenshakeParameters GridShake = new(0.8f, 0.04f, 0.015f);

    private bool TryScreenshakeGrid(EntityUid uid)
    {
        if (Timing.CurTime - _ticker.RoundStartTimeSpan < RoundStartShakeGrace)
            return true;

        _shake.Screenshake(Filter.BroadcastGrid(uid), GridShake, null);
        return true;
    }
}
