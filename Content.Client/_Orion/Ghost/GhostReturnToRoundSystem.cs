// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/045e1f70c57558b76595316d8f399599f85bc753/Content.Client/_Orion/Ghost/GhostReturnToRoundSystem.cs

using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Shared._Orion.Ghost;
using Content.Shared.Ghost.Components;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.Ghost;

public sealed partial class GhostReturnToRoundSystem : SharedGhostReturnToRoundSystem
{
    [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.25);

    private TimeSpan _nextUpdate;
    private string? _lastText;

    public void RequestReturnToRound()
    {
        RaiseNetworkEvent(new GhostReturnToRoundRequest());
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var now = GameTiming.RealTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + UpdateInterval;

        if (!TryComp<GhostComponent>(_playerManager.LocalEntity, out var ghostComponent))
            return;

        var ui = _userInterfaceManager.GetActiveUIWidgetOrNull<GhostGui>();
        if (ui == null)
            return;

        var timeLeft = GhostRespawnTime - (GameTiming.CurTime - ghostComponent.TimeOfDeath);
        var canReturn = timeLeft <= TimeSpan.Zero;
        var text = canReturn
            ? Loc.GetString("ghost-gui-return-to-round-ready-button")
            : Loc.GetString("ghost-gui-return-to-round-button", ("time", FormatTimeLeft(timeLeft)));

        if (text == _lastText && ui.ReturnToRound.Disabled == !canReturn)
            return;

        ui.ReturnToRound.Disabled = !canReturn;
        ui.ReturnToRound.Text = text;
        _lastText = text;
    }

    private static string FormatTimeLeft(TimeSpan timeLeft)
    {
        return $"{(int) timeLeft.TotalMinutes:00}:{timeLeft.Seconds:00}";
    }
}
