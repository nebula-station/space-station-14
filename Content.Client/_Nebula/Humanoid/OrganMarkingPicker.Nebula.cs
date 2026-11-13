// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;

namespace Content.Client.Humanoid;

public sealed partial class OrganMarkingPicker
{
    private bool _updateQueued;

    private void QueueUpdateMarkings()
    {
        _updateQueued = true;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_updateQueued)
            return;

        _updateQueued = false;
        UpdateMarkings();
    }
}
