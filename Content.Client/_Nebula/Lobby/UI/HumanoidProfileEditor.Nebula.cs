// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void ResetMarkings()
    {
        if (Profile == null)
            return;

        var markings = HumanoidCharacterAppearance.DefaultWithSpecies(Profile.Species, Profile.Sex).Markings;
        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithMarkings(markings));
        UpdateMarkings();
        ReloadPreview();
    }
}
