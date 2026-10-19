// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.Discord;
using Content.Shared.Database;
using Content.Shared.Roles;
using Robust.Shared.Network;
namespace Content.Server._Nebula.Discord;

public sealed partial class NebulaWebhooks
{
    public static readonly int UnbanColor = 2263842; // Dark green

    public static readonly Dictionary<BanType, int> BanTypeColor = new()
    {
        { BanType.Server, 16646146 }, // Red
        { BanType.Role, 3512539 }, // Blue
    };

    public void SendBan(CreateBanInfo banInfo, BanDef banDef)
    {
        if (_webhookIdentifierBan == null)
            return;

        CreateTypedBanMessage(banDef, banInfo.Users);
    }

    public void SendUnban(UnbanDef unBanInfo, BanDef banDef)
    {
        if (_webhookIdentifierBan == null)
            return;

        SendAssyncUnban(unBanInfo, banDef);
    }

    private async void SendAssyncUnban(UnbanDef unBanInfo, BanDef banDef)
    {
        var userSet = new HashSet<(NetUserId UserId, string UserName)>();
        foreach (var userId in banDef.UserIds)
        {
            var record = await _serverDbManager.GetPlayerRecordByUserId(userId);
            if (record != null)
                userSet.Add((userId, record.LastSeenUserName));
        }

        CreateTypedBanMessage(banDef, userSet, unBanInfo);
    }

    private WebhookPayload CreateBasedBanMessage(BanDef banDef, HashSet<(NetUserId UserId, string UserName)> users, string adminName)
    {
        var time = string.Empty;
        var remaining = banDef.ExpirationTime - DateTimeOffset.UtcNow;

        var userNames = string.Join(", ", users.Select(user => CodeBlockedSmall(user.UserName)));

        if (banDef.ExpirationTime != null && remaining > TimeSpan.Zero)
        {
            var unix = banDef.ExpirationTime.Value.ToUnixTimeSeconds();

            remaining.Value.Add(TimeSpan.FromMinutes(1));

            var days = Loc.GetString("ban-webhook-days", ("days", remaining.Value.Days));
            var hours = Loc.GetString("ban-webhook-hours", ("hours", remaining.Value.Hours));
            var minutes = Loc.GetString("ban-webhook-minutes", ("minutes", remaining.Value.Minutes));

            var relative = $"<t:{unix}:R>";

            time = $"{days} {hours} {minutes} | {relative}";
        }
        else
            time = Loc.GetString("ban-webhook-never");

        return new WebhookPayload
        {
            Username = Loc.GetString("server-ban-webhook-name"),
            Embeds = [
            new()
                {
                    Title = string.Empty,
                    Fields = [
                        new() { Name = "", Value = Loc.GetString("ban-webhook-expaire-at", ("time", time)), Inline = false},
                        new() { Name = Loc.GetString("ban-webhook-target"), Value = userNames, Inline = true },
                        EmbedSpacer,
                        new() { Name = Loc.GetString("ban-webhook-admin"), Value = CodeBlockedSmall(adminName), Inline = true },
                        new() { Name = Loc.GetString("ban-webhook-reason"), Value = CodeBlocked(banDef.Reason), Inline = false },
                    ],
                    Timestamp = banDef.BanTime.ToString("o"),
                    Footer = new WebhookEmbedFooter
                    {
                        Text = Loc.GetString(
                            "ban-webhook-footer",
                            ("round", string.Join(", ", banDef.RoundIds))),
                    },
                },
            ],
        };
    }

    private async void CreateTypedBanMessage(BanDef banDef, HashSet<(NetUserId UserId, string UserName)> users, UnbanDef? unBanInfo = null)
    {
        var payload = CreateBasedBanMessage(banDef, users, await GetAdminName(banDef.BanningAdmin));

        if (payload.Embeds == null)
            return;

        payload.Username = Loc.GetString("nebula-webhook-server-name");

        var embed = payload.Embeds[0];

        embed.Color = BanTypeColor[banDef.Type];

        var type = banDef.Type.ToString().ToLower();
        embed.Title = Loc.GetString($"ban-webhook-role-ban-{type}");

        switch (banDef.Type)
        {
            case BanType.Server:
            {
                break;
            }
            case BanType.Role:
            {
                var rolesText = banDef.Roles is { } bannedRoles
                    ? string.Join(", ", bannedRoles.Select(GetRoleName))
                    : string.Empty;

                // Bad realization, but we needed it only for localization
                var count = rolesText.Count(',');

                payload.Embeds[0].Fields.Insert(payload.Embeds[0].Fields.Count - 1,
                    new()
                    {
                        Name = Loc.GetString("ban-webhook-roles", ("count", count)),
                        Value = CodeBlocked(rolesText), Inline = false
                    }
                );

                break;
            }
        }

        if (unBanInfo != null)
        {
            embed.Color = UnbanColor;
            payload.Embeds = [embed];

            var unix = unBanInfo.UnbanTime.ToUnixTimeSeconds();
            var relative = $"<t:{unix}:R>";

            var adminName = GetAdminName(unBanInfo.UnbanningAdmin);

            payload.Embeds.Insert(payload.Embeds.Count - 1, new()
            {
                Title = Loc.GetString("ban-webhook-role-unban-server"),
                Color = UnbanColor,
                Fields = [
                        new() { Name = Loc.GetString("ban-webhook-unbanned-at-title"), Value = Loc.GetString("ban-webhook-unbanned-at", ("time", relative)), Inline = true},
                        EmbedSpacer,
                        new() { Name = Loc.GetString("ban-webhook-admin"), Value = CodeBlockedSmall(await GetAdminName(unBanInfo.UnbanningAdmin)), Inline = true },
                    ],
                Timestamp = DateTimeOffset.UtcNow.ToString("o"),
            });
        }
        else
            payload.Embeds = [embed];

        SendMessage(_webhookIdentifierBan!.Value, payload);
    }

    private string GetRoleName(BanRoleDef roleDef)
    {
        return roleDef.RoleType switch
        {
            "Job" => _prototypeManager.TryIndex<JobPrototype>(roleDef.RoleId, out var job)
                                ? Loc.GetString(job.Name)
                                : roleDef.RoleId,
            "Department" => _prototypeManager.TryIndex<DepartmentPrototype>(roleDef.RoleId, out var department)
                                ? Loc.GetString(department.Name)
                                : roleDef.RoleId,
            _ => roleDef.ToString(),
        };
    }
}
