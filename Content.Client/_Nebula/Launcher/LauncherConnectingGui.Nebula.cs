// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Discord.Linking;
using Robust.Client.UserInterface;
using Robust.Shared.Network;

namespace Content.Client.Launcher;

public sealed partial class LauncherConnectingGui
{
    private string? _discordCode;
    private string? _discordUrl;
    private float _copiedTime;

    private void NebulaInitialize()
    {
        NebulaDiscordCopyButton.OnPressed += _ =>
        {
            if (_discordCode == null)
                return;

            _clipboard.SetText(_discordCode);
            NebulaDiscordCopyButton.Text = Loc.GetString("nebula-discord-link-copied");
            _copiedTime = 3f;
        };

        NebulaDiscordOpenButton.OnPressed += _ =>
        {
            if (_discordUrl != null)
                IoCManager.Resolve<IUriOpener>().OpenUri(_discordUrl);
        };
    }

    private void NebulaHandleDisconnectReason(INetStructuredReason? reason)
    {
        _discordCode = reason?.Message.StringOf(DiscordLinkDenyKeys.Code);
        _discordUrl = reason?.Message.StringOf(DiscordLinkDenyKeys.Url);

        NebulaDiscordLink.Visible = _discordCode != null;
        NebulaDiscordOpenButton.Visible = _discordUrl != null;
    }

    private void NebulaFrameUpdate(float frameTime)
    {
        if (_copiedTime <= 0)
            return;

        _copiedTime -= frameTime;
        if (_copiedTime <= 0)
            NebulaDiscordCopyButton.Text = Loc.GetString("nebula-discord-link-copy-key");
    }
}
