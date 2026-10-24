// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Lobby.UI;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Shared.Ghost.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Ghost.Roles;

/// <summary>
/// Request and follow buttons of one ghost role, the request text counts down an active raffle.
/// Replaces <see cref="Content.Client.UserInterface.Systems.Ghost.Controls.Roles.GhostRoleEntryButtons"/>.
/// </summary>
public sealed partial class NebulaGhostRoleButtons : BoxContainer
{
    [Dependency] private IGameTiming _timing = default!;

    private const float DisabledAlpha = 0.5f;

    private readonly GhostRoleInfo _role;
    private int _shownSeconds = -1;

    public ShapedButton RequestButton { get; }
    public ShapedButton FollowButton { get; }

    public NebulaGhostRoleButtons(GhostRoleInfo role, bool hasAccess, FormattedMessage? reason, string? accentClass)
    {
        IoCManager.InjectDependencies(this);

        _role = role;
        Orientation = LayoutOrientation.Horizontal;
        SeparationOverride = 6;

        RequestButton = new ShapedButton
        {
            HorizontalExpand = true,
            SizeFlagsStretchRatio = 3,
            MinHeight = 30,
            Icon = NebulaIcons.Get(hasAccess ? "person_add" : "lock"),
        };
        RequestButton.AddStyleClass(NebulaStyleClass.ButtonFilled);

        if (accentClass != null)
            RequestButton.AddStyleClass(accentClass);

        FollowButton = new ShapedButton
        {
            Text = Loc.GetString("ghost-roles-window-follow-role-button"),
            HorizontalExpand = true,
            MinHeight = 30,
            Icon = NebulaIcons.Get("visibility"),
        };
        FollowButton.AddStyleClass(NebulaStyleClass.ButtonTonal);

        if (!hasAccess)
        {
            RequestButton.Disabled = true;
            RequestButton.Content.Modulate = Color.White.WithAlpha(DisabledAlpha);

            if (reason != null && !reason.IsEmpty)
            {
                NebulaBarPopup.Attach(RequestButton, () =>
                {
                    var popup = new NebulaBarPopup();
                    popup.AddMessage(reason);
                    return popup;
                });
            }
        }

        AddChild(RequestButton);
        AddChild(FollowButton);

        UpdateRequestText();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (IsActiveRaffle(_role.Kind) && (int) GetTimeLeft().TotalSeconds != _shownSeconds)
            UpdateRequestText();
    }

    private void UpdateRequestText()
    {
        var messageId = _role.Kind switch
        {
            GhostRoleKind.FirstComeFirstServe => "ghost-roles-window-request-role-button",
            GhostRoleKind.RaffleReady => "ghost-roles-window-join-raffle-button",
            GhostRoleKind.RaffleInProgress => "ghost-roles-window-raffle-in-progress-button",
            GhostRoleKind.RaffleJoined => "ghost-roles-window-leave-raffle-button",
            _ => throw new ArgumentOutOfRangeException(nameof(_role.Kind),
                $"Unknown {nameof(GhostRoleKind)} '{_role.Kind}'"),
        };

        if (!IsActiveRaffle(_role.Kind))
        {
            RequestButton.Text = Loc.GetString(messageId);
            return;
        }

        var timeLeft = GetTimeLeft();
        _shownSeconds = (int) timeLeft.TotalSeconds;

        var time = $"{timeLeft.Minutes:0}:{timeLeft.Seconds:00}";
        RequestButton.Text = Loc.GetString(messageId, ("time", time), ("players", _role.RafflePlayerCount));
    }

    private TimeSpan GetTimeLeft()
    {
        return _timing.CurTime <= _role.RaffleEndTime
            ? _role.RaffleEndTime - _timing.CurTime
            : TimeSpan.Zero;
    }

    private static bool IsActiveRaffle(GhostRoleKind kind)
    {
        return kind is GhostRoleKind.RaffleInProgress or GhostRoleKind.RaffleJoined;
    }
}
