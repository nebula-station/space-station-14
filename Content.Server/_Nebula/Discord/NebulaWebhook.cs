// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Server.Discord;
using Content.Shared._Nebula.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Discord;

public sealed partial class NebulaWebhooks : IPostInjectInit
{
    [Dependency] private DiscordWebhook _discord = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPlayerLocator _playerLocator = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IServerDbManager _serverDbManager = default!;

    private ISawmill _sawmill = default!;

    private WebhookIdentifier? _webhookIdentifierBan;
    private WebhookIdentifier? _webhookIdentifierPlayTime;
    private WebhookIdentifier? _webhookIdentifierPermissions;

    public void PostInject()
    {
        _sawmill = Logger.GetSawmill("discord");

        // Inject faster, then CCVar. so check is it registered
        // Dont add another if's. 1 should be enough
        if (!_cfg.IsCVarRegistered(NCCVars.DiscordBanWebhook.Name))
            return;

        _cfg.OnValueChanged(NCCVars.DiscordBanWebhook,
            CreateWebhookHandler(wi => _webhookIdentifierBan = wi), true);

        _cfg.OnValueChanged(NCCVars.DiscordPlayTimeWebhook,
            CreateWebhookHandler(wi => _webhookIdentifierPlayTime = wi), true);

        _cfg.OnValueChanged(NCCVars.DiscordPermissionsWebhook,
            CreateWebhookHandler(wi => _webhookIdentifierPermissions = wi), true);
    }

    private Action<string> CreateWebhookHandler(Action<WebhookIdentifier?> setIdentifier)
    {
        return async url =>
        {
            setIdentifier(null);

            if (string.IsNullOrEmpty(url))
                return;

            try
            {
                if (await _discord.GetWebhook(url) is not { } identifier)
                    return;

                setIdentifier(identifier.ToIdentifier());
            }
            catch (Exception e)
            {
                _sawmill.Error($"Error resolving webhook identifier: {e}");
            }
        };
    }

    #region Shared data

    private static WebhookEmbedField EmbedSpacer => new()
    {
        Name = "\u200b",
        Value = "\u200b",
        Inline = true,
    };

    private static string NOT_FOUND = Loc.GetString("ban-webhook-unknown-error");

    private static Dictionary<WebhookType, int> WebhookEmbedColors = new()
    {
        { WebhookType.PlayTimeAdd, ColorToDiscordInt(Color.FromHex("#009455")) },
        { WebhookType.PlayTimeRem, ColorToDiscordInt(Color.FromHex("#007041")) },
        { WebhookType.PlayTimeSet, ColorToDiscordInt(Color.FromHex("#3d9a73")) },

        { WebhookType.CoinsAdd, ColorToDiscordInt(Color.FromHex("#009E98")) },
        { WebhookType.CoinsRem, ColorToDiscordInt(Color.FromHex("#00706C")) },
        { WebhookType.CoinsSet, ColorToDiscordInt(Color.FromHex("#0d3937")) },

        { WebhookType.AdminRoleAdd, ColorToDiscordInt(Color.FromHex("#711300")) },
        { WebhookType.AdminRoleRem, ColorToDiscordInt(Color.FromHex("#a61c00")) },
        { WebhookType.AdminRoleUpdate, ColorToDiscordInt(Color.FromHex("#bb1f00")) },

        { WebhookType.AdminAdd, ColorToDiscordInt(Color.FromHex("#711300")) },
        { WebhookType.AdminRem, ColorToDiscordInt(Color.FromHex("#a61c00")) },
        { WebhookType.AdminUpdate, ColorToDiscordInt(Color.FromHex("#bb1f00")) }
    };

    private enum WebhookType : byte
    {
        PlayTimeAdd,
        PlayTimeRem,
        PlayTimeSet,

        CoinsAdd,
        CoinsRem,
        CoinsSet,

        AdminRoleAdd,
        AdminRoleRem,
        AdminRoleUpdate,

        AdminAdd,
        AdminRem,
        AdminUpdate
    }

    #endregion
    #region Shared functions

    private async Task<string> GetAdminName(NetUserId? id)
    {
        if (id is not { } admin)
            return Loc.GetString("nebula-webhook-unknown");

        if (_playerManager.TryGetPlayerData(admin, out var adminData))
            return adminData.UserName;

        var locatedData = await _playerLocator.LookupIdAsync(admin);
        return locatedData?.Username ?? Loc.GetString("nebula-webhook-unknown");
    }

    private string CodeBlockedSmall(string value)
    {
        return $"``{value}``";
    }

    private string CodeBlocked(string value)
    {
        return $"```{value}```";
    }

    private static int ColorToDiscordInt(Color color)
    {
        return (color.RByte << 16) | (color.GByte << 8) | color.BByte;
    }

    private WebhookEmbed CreateBaseEmbedWithNames(NetUserId adminId, NetUserId targetId)
    {
        _playerManager.TryGetPlayerData(targetId, out var target);
        _playerManager.TryGetPlayerData(adminId, out var admin);

        var targetName = target?.UserName ?? Loc.GetString("nebula-webhook-unknown");
        var adminName = admin?.UserName ?? Loc.GetString("nebula-webhook-unknown");

        return new WebhookEmbed()
        {
            Title = string.Empty,
            Fields = [
                new() { Name = Loc.GetString("playtime-webhook-target"), Value = CodeBlockedSmall(targetName), Inline = true },
                EmbedSpacer,
                new() { Name = Loc.GetString("playtime-webhook-admin"), Value = CodeBlockedSmall(adminName), Inline = true },
            ]
        };
    }

    private async void SendMessage(WebhookIdentifier identifier, WebhookPayload payload)
    {
        try
        {
            await _discord.CreateMessage(identifier, payload);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while sending webhook to Discord: {e}");
        }
    }
    #endregion
}

