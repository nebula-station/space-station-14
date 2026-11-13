// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Administration;
using Content.Shared.Administration.Notes;

namespace Content.Client.Administration.UI.Notes;

public sealed partial class AdminNotesEui
{
    private void AdminAssistPrefill(AdminNotesEuiState state)
    {
        if (!state.CanCreate)
            return;

        var briefings = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AdminAssistBriefingSystem>();
        if (briefings.TakeNoteDraft(state.NotedPlayerName) is { } draft)
            NoteControl.OpenAdminAssistNote(draft);
    }
}
