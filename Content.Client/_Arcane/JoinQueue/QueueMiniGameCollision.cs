// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.Arcane.Client/JoinQueue/QueueMiniGameCollision.cs

namespace Content.Client._Arcane.JoinQueue;

internal static class QueueMiniGameCollision
{
    // Sweep the projectile's top-left corner through the target expanded by the projectile size.
    public static bool Intersects(float fromX, float fromY, float toX, float toY, float width, float height,
        float targetX, float targetY, float targetWidth, float targetHeight)
        => Intersects(fromX, fromY, toX, toY, width, height, targetX, targetY, targetWidth, targetHeight, out _);

    public static bool Intersects(float fromX, float fromY, float toX, float toY, float width, float height,
        float targetX, float targetY, float targetWidth, float targetHeight, out float enter)
    {
        enter = 0f;
        var exit = 1f;
        return Clip(fromX, toX - fromX, targetX - width, targetX + targetWidth, ref enter, ref exit) &&
               Clip(fromY, toY - fromY, targetY - height, targetY + targetHeight, ref enter, ref exit);
    }

    public static bool ClipPath(ref float fromX, ref float fromY, ref float toX, ref float toY,
        float minX, float minY, float maxX, float maxY)
    {
        var enter = 0f;
        var exit = 1f;
        var dx = toX - fromX;
        var dy = toY - fromY;
        if (!Clip(fromX, dx, minX, maxX, ref enter, ref exit, true) ||
            !Clip(fromY, dy, minY, maxY, ref enter, ref exit, true))
            return false;
        toX = fromX + dx * exit;
        toY = fromY + dy * exit;
        fromX += dx * enter;
        fromY += dy * enter;
        return true;
    }

    private static bool Clip(float start, float delta, float min, float max, ref float enter, ref float exit,
        bool inclusive = false)
    {
        if (delta == 0f)
            return inclusive ? start >= min && start <= max : start > min && start < max;

        var first = (min - start) / delta;
        var last = (max - start) / delta;
        if (first > last)
            (first, last) = (last, first);
        enter = Math.Max(enter, first);
        exit = Math.Min(exit, last);
        return inclusive ? enter <= exit : enter < exit;
    }
}
