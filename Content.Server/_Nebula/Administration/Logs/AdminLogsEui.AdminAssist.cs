// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Administration;

namespace Content.Server.Administration.Logs;

public sealed partial class AdminLogsEui
{
    public void FocusAdminAssistCitation(AdminAssistLogFocusMessage focus)
    {
        SendMessage(focus);
    }
}
