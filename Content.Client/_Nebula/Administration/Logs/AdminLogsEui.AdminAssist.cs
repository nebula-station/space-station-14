// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Administration;

namespace Content.Client.Administration.UI.Logs;

public sealed partial class AdminLogsEui
{
    private AdminAssistLogFocusMessage? _adminAssistFocus;

    private DateTime? FocusBefore => _adminAssistFocus != null && LogsControl.AdminAssistWindowOn
        ? _adminAssistFocus.Before
        : null;

    private DateTime? FocusAfter => _adminAssistFocus != null && LogsControl.AdminAssistWindowOn
        ? _adminAssistFocus.After
        : null;

    private void FocusAdminAssistCitation(AdminAssistLogFocusMessage focus)
    {
        if (_adminAssistFocus == null)
            LogsControl.AdminAssistWindowToggled += RequestLogs;

        _adminAssistFocus = focus;
        LogsControl.SetAdminAssistCited(focus.CitedLogIds);
        LogsControl.SelectPlayers(focus.Players);

        if (!FirstState)
        {
            LogsControl.SetRoundSpinBox(focus.RoundNumber);
            RequestLogs();
        }
    }
}
