// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Administration.UI.CustomControls;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.Logs;

public sealed partial class AdminLogsControl
{
    private static readonly Color CitedColor = Color.Gold;

    private readonly HashSet<int> _adminAssistCited = new();
    private Button? _windowButton;
    private Button? _onlyCitedButton;

    public bool AdminAssistWindowOn => _windowButton?.Pressed ?? false;

    public event Action? AdminAssistWindowToggled;

    public void SetAdminAssistCited(HashSet<int> logIds)
    {
        _adminAssistCited.Clear();
        _adminAssistCited.UnionWith(logIds);

        if (_windowButton == null)
        {
            _windowButton = new Button
            {
                Text = Loc.GetString("admin-assist-logs-window"),
                ToolTip = Loc.GetString("admin-assist-logs-window-tooltip"),
                ToggleMode = true,
                Pressed = true,
                StyleClasses = { "OpenRight" },
            };
            _windowButton.OnToggled += _ => AdminAssistWindowToggled?.Invoke();

            _onlyCitedButton = new Button
            {
                Text = Loc.GetString("admin-assist-logs-only-cited"),
                ToggleMode = true,
                StyleClasses = { "OpenLeft" },
            };
            _onlyCitedButton.OnToggled += _ => UpdateLogs();

            TopRightContainer.AddChild(_windowButton);
            TopRightContainer.AddChild(_onlyCitedButton);
            _windowButton.SetPositionInParent(0);
            _onlyCitedButton.SetPositionInParent(1);
        }

        var ticket = logIds.Count == 0;
        _windowButton.Text = Loc.GetString(ticket ? "admin-assist-logs-window-ticket" : "admin-assist-logs-window");
        _windowButton.ToolTip = Loc.GetString(ticket
            ? "admin-assist-logs-window-ticket-tooltip"
            : "admin-assist-logs-window-tooltip");
        _windowButton.StyleClasses.Remove(ticket ? "OpenRight" : "OpenBoth");
        _windowButton.StyleClasses.Add(ticket ? "OpenBoth" : "OpenRight");
        _onlyCitedButton!.Visible = !ticket;
        if (ticket)
            _onlyCitedButton.Pressed = false;

        foreach (var child in LogsContainer.Children)
        {
            if (child is AdminLogLabel label)
                HighlightAdminAssistCited(label);
        }

        UpdateLogs();
    }

    private bool HiddenByAdminAssist(AdminLogLabel label)
    {
        return _onlyCitedButton is { Pressed: true } && !_adminAssistCited.Contains(label.Log.Id);
    }

    private void HighlightAdminAssistCited(AdminLogLabel label)
    {
        if (!_adminAssistCited.Contains(label.Log.Id))
            return;

        label.SetMessage($"▶ {label.Log.Date.ToLocalTime():HH:mm:ss}: {label.Log.Message}", CitedColor);
    }
}
