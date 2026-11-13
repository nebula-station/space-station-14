// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;

namespace Content.Client.Humanoid;

public sealed partial class MarkingPicker
{
    private bool _updateQueued;

    private void SetModelNebula(MarkingsViewModel model)
    {
        if (_markingsModel == model)
            return;

        if (IsInsideTree && _markingsModel != null)
        {
            _markingsModel.OrganDataChanged -= QueueUpdateMarkings;
            _markingsModel.EnforcementsChanged -= QueueUpdateMarkings;
        }

        _markingsModel = model;

        if (IsInsideTree)
        {
            _markingsModel.OrganDataChanged += QueueUpdateMarkings;
            _markingsModel.EnforcementsChanged += QueueUpdateMarkings;
        }

        QueueUpdateMarkings();
    }

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
