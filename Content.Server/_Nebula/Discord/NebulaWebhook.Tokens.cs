// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;
using Content.Goobstation.Shared.ServerCurrency;
using Content.Server.Discord;
using Robust.Shared.Network;

namespace Content.Server._Nebula.Discord;

public sealed partial class NebulaWebhooks
{
    public void SendTokensChangedMessage(NetUserId adminId, NetUserId targetId, string value, bool isSet = false)
    {
        if (int.TryParse(value, out var number))
            SendTokensChangedMessage(adminId, targetId, number, isSet);
    }

    public void SendTokensChangedMessage(NetUserId adminId, NetUserId targetId, int value, bool isSet = false)
    {
        if (_webhookIdentifierTokens == null)
            return;

        SendTokenChanged(adminId, targetId, value, isSet);
    }

    public void SendTokenBoughtMessage(NetUserId user, TokenListingPrototype token)
    {
        if (_webhookIdentifierTokens == null)
            return;

        SendTokenBought(user, token);
    }

    /// <summary>
    /// Reports a purchase, grant or refund of an antagonist bought with an antag token.
    /// </summary>
    public void SendAntagTokenMessage(NetUserId user, LocId title, string antagName)
    {
        if (_webhookIdentifierTokens == null)
            return;

        _playerManager.TryGetPlayerData(user, out var data);

        var payload = new WebhookPayload
        {
            Username = Loc.GetString("nebula-webhook-server-name"),
            Embeds =
            [
                new WebhookEmbed
                {
                    Title = Loc.GetString(title),
                    Color = WebhookEmbedColors[WebhookType.TokenBuy],
                    Fields =
                    [
                        new()
                        {
                            Name = Loc.GetString("playtime-webhook-target"),
                            Value = CodeBlockedSmall(data?.UserName ?? Loc.GetString("nebula-webhook-unknown")),
                            Inline = true
                        },
                        EmbedSpacer,
                        new()
                        {
                            Name = Loc.GetString("tokens-webhook-token-value"),
                            Value = CodeBlockedSmall(antagName),
                            Inline = true
                        },
                    ]
                }
            ]
        };

        SendMessage(_webhookIdentifierTokens.Value, payload);
    }

    private async void SendTokenChanged(NetUserId adminId, NetUserId targetId, int value, bool isSet = false)
    {
        var embed = CreateBaseEmbedWithNames(adminId, targetId);

        if (!isSet)
        {
            if (value > 0)
            {
                embed.Title = Loc.GetString("tokens-webhook-title-add");
                embed.Color = WebhookEmbedColors[WebhookType.CoinsAdd];
            }
            else
            {
                embed.Title = Loc.GetString("tokens-webhook-title-rem");
                embed.Color = WebhookEmbedColors[WebhookType.CoinsRem];
            }
        }
        else
        {
            embed.Title = Loc.GetString("tokens-webhook-title-set");
            embed.Color = WebhookEmbedColors[WebhookType.CoinsSet];
        }

        var balance = await GetBalance(targetId);

        embed.Fields.Add(new WebhookEmbedField
        {
            Name = Loc.GetString("tokens-webhook-value-old"),
            Value = CodeBlocked((balance - value).ToString("N0")),
            Inline = true
        });

        embed.Fields.Add(new WebhookEmbedField
        {
            Name = Loc.GetString("tokens-webhook-value"),
            Value = CodeBlocked(value.ToString("N0")),
            Inline = true
        });

        embed.Fields.Add(new WebhookEmbedField
        {
            Name = Loc.GetString("tokens-webhook-value-new"),
            Value = CodeBlocked(balance.ToString("N0")),
            Inline = true
        });

        var payload = new WebhookPayload
        {
            Username = Loc.GetString("nebula-webhook-server-name"),
            Embeds = [embed]
        };

        SendMessage(_webhookIdentifierTokens!.Value, payload);
    }

    private async Task<int> GetBalance(NetUserId? userId = null)
    {
        return userId == null ? 0 : await _serverDbManager.GetServerCurrency(userId.Value);
    }

    private async void SendTokenBought(NetUserId userId, TokenListingPrototype token)
    {
        _playerManager.TryGetPlayerData(userId, out var user);

        var userName = user?.UserName ?? Loc.GetString("nebula-webhook-unknown");

        var payload = new WebhookPayload
        {
            Username = Loc.GetString("nebula-webhook-server-name"),
            Embeds =
            [
                new WebhookEmbed
                {
                    Title = Loc.GetString("tokens-webhook-title-buy"),
                    Color = WebhookEmbedColors[WebhookType.TokenBuy],
                    Fields =
                    [
                        new()
                        {
                            Name = Loc.GetString("playtime-webhook-target"),
                            Value = CodeBlockedSmall(userName),
                            Inline = true
                        },
                        EmbedSpacer,
                        new()
                        {
                            Name = Loc.GetString("tokens-webhook-token-value"),
                            Value = CodeBlockedSmall(Loc.GetString(token.Label)),
                            Inline = true
                        },
                    ]
                }
            ]
        };

        SendMessage(_webhookIdentifierTokens!.Value, payload);
    }
}
