// SPDX-FileCopyrightText: 2026 Lytheriia
// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Lobby.UI;
using Content.Client.Lobby.UI.Roles;
using Content.Shared._Nebula.ERP;
using Content.Shared._Nebula.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Robust.Shared.Enums;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private JobAlternateTitleSystem? _jobAltTitles;

    private void ResetMarkings()
    {
        if (Profile == null)
            return;

        var cleared = Profile.Appearance.WithMarkings([]);
        Profile = Profile.WithCharacterAppearance(HumanoidCharacterAppearance.EnsureValid(cleared, Profile.Species, Profile.Sex));
        UpdateMarkings();
        ReloadPreview();
    }

    private void CopySkinColorToMarkings()
    {
        if (Profile == null)
            return;

        _markingsModel.SetAllMarkingColors(Profile.Appearance.SkinColor);
    }

    /// <summary>
    /// Replaces the job name of the selector with a picker of its alternate titles, if the job has any.
    /// </summary>
    private void SetupAltTitles(RequirementsSelector selector, JobPrototype job)
    {
        _jobAltTitles ??= _entManager.System<JobAlternateTitleSystem>();
        if (!_jobAltTitles.Enabled)
            return;

        var titles = _jobAltTitles.GetTitles(job.ID);
        if (titles.Count == 0)
            return;

        var picker = new NebulaAltTitlePicker(job.LocalizedName, titles);
        picker.SetGender(Profile?.Gender ?? Gender.Epicene);
        if (Profile != null && Profile.JobAlternateTitles.TryGetValue(job.ID, out var selected))
            picker.Select(selected);

        picker.OnSelected += title =>
        {
            Profile = Profile?.WithJobAltTitle(job.ID, title);

            foreach (var (jobId, other) in _jobPriorities)
            {
                if (jobId == job.ID && other.AltTitlePicker != picker)
                    other.AltTitlePicker?.Select(title);
            }

            SetDirty();
        };

        selector.SetAltTitlePicker(picker);
    }

    /// <summary>
    /// Updates alternate titles to the gendered forms of the current profile.
    /// </summary>
    private void UpdateAltTitleGender()
    {
        var gender = Profile?.Gender ?? Gender.Epicene;
        foreach (var (_, selector) in _jobPriorities)
        {
            selector.AltTitlePicker?.SetGender(gender);
        }
    }

    private void OnConsentChanged(string tag, ErpConsent consent)
    {
        if (Profile == null)
            return;

        var data = Profile.DetailExaminableData.CopyWithConsent(tag, consent);
        data.ValidateConsentTags(_prototypeManager);
        Profile = Profile.WithDetailExaminableData(data);
        _flavorText?.SetConsentData(data);
        SetDirty();
    }
}
