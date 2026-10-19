// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Database;
using Content.Server.Discord;
using Robust.Shared.Network;
using AdminRank = Content.Server.Database.AdminRank;

namespace Content.Server._Nebula.Discord;

public sealed partial class NebulaWebhooks
{
    #region Ranks
    public void SendRankDeletedMessage(NetUserId adminId, AdminRank rank)
    {
        if (_webhookIdentifierPermissions == null)
            return;

        SendRankRA(adminId, rank, true);
    }

    public void SendRankCreatedMessage(NetUserId adminId, AdminRank rank)
    {
        if (_webhookIdentifierPermissions == null)
            return;

        SendRankRA(adminId, rank, false);
    }

    public void SendRankChangedMessage(NetUserId adminId, AdminRank? oldRank, AdminRank? newRank)
    {
        if (_webhookIdentifierPermissions == null)
            return;

        SendChangelogRank(adminId, oldRank, newRank);
    }

    private async void SendChangelogRank(NetUserId adminId, AdminRank? oldRank, AdminRank? newRank)
    {
        if (oldRank == null || newRank == null)
            return;

        var adminName = await GetAdminName(adminId);

        var fields = new List<WebhookEmbedField>
        {
            new() { Name = Loc.GetString("permissions-webhook-title-changed"),
                Value = CodeBlockedSmall(oldRank.Name), Inline = true },
            new() { Name = Loc.GetString("ban-webhook-admin"),
                Value = CodeBlockedSmall(adminName), Inline = true }
        };

        if (oldRank.Name != newRank.Name)
            fields.Add(new WebhookEmbedField
            {
                Name = Loc.GetString("permissions-webhook-field-title-changed"),
                Value = CodeBlocked($"{oldRank.Name} -> {newRank.Name}"),
                Inline = false
            });

        var oldFlags = oldRank.Flags.Select(f => f.Flag).ToHashSet();
        var newFlags = newRank.Flags.Select(f => f.Flag).ToHashSet();

        var addedFlags = newFlags.Except(oldFlags).ToList();
        var removedFlags = oldFlags.Except(newFlags).ToList();

        if (addedFlags.Count > 0)
            fields.Add(new WebhookEmbedField
            {
                Name = Loc.GetString("permissions-webhook-field-flags-added"),
                Value = CodeBlocked(string.Join(", ", addedFlags)),
                Inline = false
            });

        if (removedFlags.Count > 0)
            fields.Add(new WebhookEmbedField
            {
                Name = Loc.GetString("permissions-webhook-field-flags-removed"),
                Value = CodeBlocked(string.Join(", ", removedFlags)),
                Inline = false
            });

        if (addedFlags.Count == 0 && removedFlags.Count == 0 && oldRank.Name == newRank.Name)
            return;

        var payload = new WebhookPayload()
        {
            Username = Loc.GetString("nebula-webhook-server-name"),
            Embeds = [
                new WebhookEmbed()
            {
                Title = Loc.GetString("permissions-webhook-title-changed"),
                Fields = fields,
                Color = WebhookEmbedColors[WebhookType.AdminRoleAdd]
            }
            ]
        };

        SendMessage(_webhookIdentifierPermissions!.Value, payload);
    }

    private async void SendRankRA(NetUserId adminId, AdminRank rank, bool isRemoved)
    {
        var adminName = await GetAdminName(adminId);

        var fields = new List<WebhookEmbedField>
        {
            new() { Name = Loc.GetString("permissions-webhook-field-rank-name"),
                Value = CodeBlockedSmall(rank.Name), Inline = true },
            new() { Name = Loc.GetString("ban-webhook-admin"),
                Value = CodeBlockedSmall(adminName), Inline = true }
        };

        if (!isRemoved)
            fields.Add(new WebhookEmbedField
            {
                Name = Loc.GetString("permissions-webhook-field-flags"),
                Value = CodeBlocked(string.Join(", ", rank.Flags.Select(f => f.Flag))),
                Inline = false
            });
        else
            if (rank.Admins != null)
                fields.Add(new WebhookEmbedField
                {
                    Name = Loc.GetString("permissions-webhook-field-touched-admins"),
                    Value = CodeBlocked(string.Join(", ",
                        rank.Admins.Select(a => GetAdminName(new NetUserId(a.UserId))))),
                    Inline = false
                });


        var payload = new WebhookPayload()
        {
            Username = Loc.GetString("nebula-webhook-server-name"),
            Embeds = [
                new WebhookEmbed()
                {
                    Title = isRemoved ? Loc.GetString("permissions-webhook-title-deleted") : Loc.GetString("permissions-webhook-title-created"),
                    Fields = fields,
                    Color =  isRemoved ? WebhookEmbedColors[WebhookType.AdminRoleRem] : WebhookEmbedColors[WebhookType.AdminRoleAdd]
                }
            ]
        };

        SendMessage(_webhookIdentifierPermissions!.Value, payload);
    }

    #endregion
    #region Admins

    public void SendAdminDeletedMessage(NetUserId adminId, Admin admin)
    {
        if (_webhookIdentifierPermissions == null)
            return;

        SendAdminRA(adminId, admin, true);
    }

    public void SendAdminAddMessage(NetUserId adminId, Admin admin)
    {
        if (_webhookIdentifierPermissions == null)
            return;

        SendAdminRA(adminId, admin, false);
    }

    public void SendAdminChangedMessage(NetUserId adminId, Admin? oldAdmin, Admin? newAdmin)
    {
        if (_webhookIdentifierPermissions == null)
            return;

        SendChangelogAdmin(adminId, oldAdmin, newAdmin);
    }

    private async void SendAdminRA(NetUserId adminId, Admin admin, bool isRemoved)
    {
        var adminName = await GetAdminName(adminId);
        var targetName = await GetAdminName(new NetUserId(admin.UserId));

        var fields = new List<WebhookEmbedField>
        {
            new() { Name = Loc.GetString("permissions-webhook-field-target-admin"),
                Value = CodeBlockedSmall(targetName), Inline = true },
            new() { Name = Loc.GetString("ban-webhook-admin"),
                Value = CodeBlockedSmall(adminName), Inline = true }
        };

        if (!isRemoved)
            fields.Add(new WebhookEmbedField
            {
                Name = Loc.GetString("permissions-webhook-field-flags"),
                Value = CodeBlocked(string.Join(", ", admin.Flags.Select(f => f.Flag))),
                Inline = false
            });

        if (admin.AdminRank != null)
            fields.Add(new WebhookEmbedField
            {
                Name = Loc.GetString("permissions-webhook-field-rank-name"),
                Value = CodeBlocked(admin.AdminRank.Name),
                Inline = false
            });

        if (admin.Title != null)
            fields.Add(new WebhookEmbedField
            {
                Name = Loc.GetString("permissions-webhook-field-role-title"),
                Value = CodeBlocked(admin.Title),
                Inline = false
            });

        var payload = new WebhookPayload()
        {
            Username = Loc.GetString("nebula-webhook-server-name"),
            Embeds = [
                new WebhookEmbed()
            {
                Title = isRemoved ? Loc.GetString("permissions-webhook-title-admin-deleted") : Loc.GetString("permissions-webhook-title-admin-created"),
                Fields = fields,
                Color = isRemoved ? WebhookEmbedColors[WebhookType.AdminRoleRem] : WebhookEmbedColors[WebhookType.AdminRoleAdd]
            }
            ]
        };

        SendMessage(_webhookIdentifierPermissions!.Value, payload);
    }

    private async void SendChangelogAdmin(NetUserId adminId, Admin? oldAdmin, Admin? newAdmin)
    {
        if (oldAdmin == null || newAdmin == null)
            return;

        var adminName = await GetAdminName(adminId);
        var targetName = await GetAdminName(new NetUserId(newAdmin.UserId));

        var fields = new List<WebhookEmbedField>
        {
            new() { Name = Loc.GetString("permissions-webhook-field-target-admin"),
                Value = CodeBlockedSmall(targetName), Inline = true },
            new() { Name = Loc.GetString("ban-webhook-admin"),
                Value = CodeBlockedSmall(adminName), Inline = true }
        };

        if (oldAdmin.Title != newAdmin.Title)
            fields.Add(new WebhookEmbedField
            {
                Name = Loc.GetString("permissions-webhook-field-title-changed"),
                Value = CodeBlocked($"{oldAdmin.Title ?? "-"} -> {newAdmin.Title ?? "-"}"),
                Inline = false
            });

        if (oldAdmin.AdminRankId != newAdmin.AdminRankId)
            fields.Add(new WebhookEmbedField
            {
                Name = Loc.GetString("permissions-webhook-field-rank-changed"),
                Value = CodeBlocked($"{oldAdmin.AdminRank?.Name ?? "-"} -> {newAdmin.AdminRank?.Name ?? "-"}"),
                Inline = false
            });

        if (oldAdmin.Suspended != newAdmin.Suspended)
            fields.Add(new WebhookEmbedField
            {
                Name = Loc.GetString("permissions-webhook-field-suspended-changed"),
                Value = CodeBlocked(newAdmin.Suspended.ToString()),
                Inline = false
            });

        var oldFlags = oldAdmin.Flags.Select(f => f.Flag).ToHashSet();
        var newFlags = newAdmin.Flags.Select(f => f.Flag).ToHashSet();

        var addedFlags = newFlags.Except(oldFlags).ToList();
        var removedFlags = oldFlags.Except(newFlags).ToList();

        if (addedFlags.Count > 0)
            fields.Add(new WebhookEmbedField
            {
                Name = Loc.GetString("permissions-webhook-field-flags-added"),
                Value = CodeBlocked(string.Join(", ", addedFlags)),
                Inline = false
            });

        if (removedFlags.Count > 0)
            fields.Add(new WebhookEmbedField
            {
                Name = Loc.GetString("permissions-webhook-field-flags-removed"),
                Value = CodeBlocked(string.Join(", ", removedFlags)),
                Inline = false
            });

        if (fields.Count == 2)
            return;

        var payload = new WebhookPayload()
        {
            Username = Loc.GetString("nebula-webhook-server-name"),
            Embeds = [
                new WebhookEmbed()
            {
                Title = Loc.GetString("permissions-webhook-title-admin-changed"),
                Fields = fields,
                Color = WebhookEmbedColors[WebhookType.AdminRoleAdd]
            }
            ]
        };

        SendMessage(_webhookIdentifierPermissions!.Value, payload);
    }

    #endregion
}
