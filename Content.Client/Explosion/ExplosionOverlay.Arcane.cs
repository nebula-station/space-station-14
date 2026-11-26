// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Client/Explosion/ExplosionOverlay.cs

using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client.Explosion;

public sealed partial class ExplosionOverlay
{
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    ///     The frame of an animated state at the current time, following the state's own frame delays.
    /// </summary>
    private Texture GetAnimatedFrame(Texture[] frames, float[] delays)
    {
        var total = 0f;
        foreach (var delay in delays)
        {
            total += delay;
        }

        if (frames.Length <= 1 || delays.Length != frames.Length || total <= 0f)
            return frames[0];

        var time = (float) (_timing.RealTime.TotalSeconds % total);
        for (var i = 0; i < frames.Length; i++)
        {
            time -= delays[i];
            if (time < 0f)
                return frames[i];
        }

        return frames[^1];
    }
}
