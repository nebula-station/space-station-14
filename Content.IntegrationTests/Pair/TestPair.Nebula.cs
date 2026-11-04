// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using Content.Server.Preferences.Managers;
using Content.Shared._Nebula.Roles;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Pair;

public sealed partial class TestPair
{
    /// <summary>
    /// Set a user's alternate title for a job and optionally the gender. Modified preferences are automatically reset at the end of the test.
    /// </summary>
    public async Task SetJobAltTitle(
        ProtoId<JobPrototype> job,
        ProtoId<JobAlternateTitlePrototype>? title,
        Gender? gender = null,
        NetUserId? user = null)
    {
        var userId = user ?? Client.User!.Value;

        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var prefs = prefMan.GetPreferences(userId);

        Assert.That(prefs.SelectedCharacterIndex, Is.EqualTo(0));

        var profile = ((HumanoidCharacterProfile) prefs.Characters[0]).WithJobAltTitle(job, title);
        if (gender is { } value)
            profile = profile.WithGender(value);

        _modifiedProfiles.Add(userId);
        await Server.WaitPost(() => prefMan.SetProfile(userId, 0, profile).Wait());
    }

    /// <summary>
    /// Set the species of a user's first character. Modified preferences are automatically reset at the end of the test.
    /// </summary>
    public async Task SetSpecies(ProtoId<SpeciesPrototype> species, NetUserId? user = null)
    {
        var userId = user ?? Client.User!.Value;

        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var prefs = prefMan.GetPreferences(userId);

        Assert.That(prefs.SelectedCharacterIndex, Is.EqualTo(0));

        var profile = ((HumanoidCharacterProfile) prefs.Characters[0]).WithSpecies(species);

        _modifiedProfiles.Add(userId);
        await Server.WaitPost(() => prefMan.SetProfile(userId, 0, profile).Wait());
    }

    /// <summary>
    /// Set the wardrobe of a user's first character. Modified preferences are automatically reset at the end of the test.
    /// </summary>
    public async Task SetWardrobe(List<EntProtoId> wardrobe, NetUserId? user = null)
    {
        var userId = user ?? Client.User!.Value;

        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var prefs = prefMan.GetPreferences(userId);

        Assert.That(prefs.SelectedCharacterIndex, Is.EqualTo(0));

        var profile = new HumanoidCharacterProfile((HumanoidCharacterProfile) prefs.Characters[0]) { Wardrobe = wardrobe };

        _modifiedProfiles.Add(userId);
        await Server.WaitPost(() => prefMan.SetProfile(userId, 0, profile).Wait());
    }
}
