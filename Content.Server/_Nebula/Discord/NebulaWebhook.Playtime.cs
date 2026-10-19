// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Discord;
using Content.Shared.Roles;
using Robust.Shared.Network;

namespace Content.Server._Nebula.Discord;

public sealed partial class NebulaWebhooks
{
    private static readonly Dictionary<string, string> JobNames = [];

    /// <summary>
    /// That function cant be called at post inject, because it not loaded prototypes yet
    /// </summary>
    private void OnPlayTimeWebhookInit()
    {
        foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>())
            JobNames[job.PlayTimeTracker] = job.LocalizedName;
    }

    public void SendTimeChangedMessage(NetUserId adminId, NetUserId targetId, Dictionary<string, TimeSpan> timeData, bool isSet = false)
    {
        if (_webhookIdentifierPlayTime == null || timeData.Count == 0)
            return;

        if (JobNames.Count == 0)
            OnPlayTimeWebhookInit();

        var firstTime = timeData.First().Value;
        if (timeData.Any(t => t.Value != firstTime))
        {
            SendDifferentTimeChanges(adminId, targetId, timeData, isSet);
            return;
        }

        SendEqualTimeChanges(adminId, targetId, timeData, isSet);
    }

    private void SendDifferentTimeChanges(NetUserId adminId, NetUserId targetId, Dictionary<string, TimeSpan> timeData, bool isSet = false)
    {
        var embed = CreateBaseEmbedWithNames(adminId, targetId);

        if (isSet)
        {
            embed.Title = Loc.GetString("playtime-webhook-title-set");
            embed.Color = WebhookEmbedColors[WebhookType.PlayTimeSet];
        }
        else
        {
            embed.Title = Loc.GetString("playtime-webhook-title-change");
            embed.Color = WebhookEmbedColors[WebhookType.PlayTimeAdd];
        }

        AddRoleColumns(embed, timeData);

        var payload = new WebhookPayload()
        {
            Username = Loc.GetString("nebula-webhook-server-name"),
            Embeds = [
                embed
            ]
        };

        SendMessage(_webhookIdentifierPlayTime!.Value, payload);
    }

    private void AddRoleColumns(WebhookEmbed embed, Dictionary<string, TimeSpan> timeData)
    {
        var lines = timeData.Select(kv =>
        {
            var sign = kv.Value < TimeSpan.Zero ? "-" : "+";

            var timeSpan = kv.Value.Duration();

            var parts = new List<string>();
            if (timeSpan.Days != 0) parts.Add(Loc.GetString("playtime-webhook-days-short", ("days", timeSpan.Days)));
            if (timeSpan.Hours != 0) parts.Add(Loc.GetString("playtime-webhook-hours-short", ("hours", timeSpan.Hours)));
            if (timeSpan.Minutes != 0) parts.Add(Loc.GetString("playtime-webhook-minutes-short", ("minutes", timeSpan.Minutes)));

            var time = parts.Count > 0 ? string.Join(" ", parts) : "0М.";

            var name = JobNames.GetValueOrDefault(kv.Key, kv.Key);
            return $"{sign} {time} {name}";
        }).ToList();

        var columns = 3;
        var perColumn = (int) Math.Ceiling(lines.Count / (double) columns);

        for (var i = 0; i < columns; i++)
        {
            var chunk = lines.Skip(i * perColumn).Take(perColumn).ToList();
            if (chunk.Count == 0)
                continue;

            embed.Fields.Add(new WebhookEmbedField()
            {
                Name = i == 0 ? Loc.GetString("playtime-webhook-roles", ("count", timeData.Count)) : "\u200b",
                Value = CodeBlocked(string.Join("\n", chunk)),
                Inline = true
            });
        }
    }

    private void SendEqualTimeChanges(NetUserId adminId, NetUserId targetId, Dictionary<string, TimeSpan> timeData, bool isSet = false)
    {
        var embed = CreateBaseEmbedWithNames(adminId, targetId);

        if (!isSet)
        {
            if (timeData.First().Value < TimeSpan.Zero)
            {
                embed.Title = Loc.GetString("playtime-webhook-title-rem");
                embed.Color = WebhookEmbedColors[WebhookType.PlayTimeRem];
            }
            else
            {
                embed.Title = Loc.GetString("playtime-webhook-title-add");
                embed.Color = WebhookEmbedColors[WebhookType.PlayTimeAdd];
            }
        }
        else
        {
            embed.Title = Loc.GetString("playtime-webhook-title-set");
            embed.Color = WebhookEmbedColors[WebhookType.PlayTimeSet];
        }

        var timeTimeSpan = timeData.First().Value;

        var days = Loc.GetString("ban-webhook-days", ("days", timeTimeSpan.Days));
        var hours = Loc.GetString("ban-webhook-hours", ("hours", timeTimeSpan.Hours));
        var minutes = Loc.GetString("ban-webhook-minutes", ("minutes", timeTimeSpan.Minutes));

        var time = $"{days} {hours} {minutes}";

        embed.Fields.Add(new WebhookEmbedField()
        {
            Name = "",
            Value = Loc.GetString("playtime-webhook-added-time", ("time", time))
        });

        var roleNames = string.Join(", ",
            timeData.Keys.Select(k => JobNames.GetValueOrDefault(k, k)));

        embed.Fields.Add(new WebhookEmbedField()
        {
            Name = Loc.GetString("playtime-webhook-roles", ("count", timeData.Count)),
            Value = CodeBlocked(roleNames),
            Inline = false
        });

        var payload = new WebhookPayload()
        {
            Username = Loc.GetString("nebula-webhook-server-name"),
            Embeds = [
                embed
            ]
        };

        SendMessage(_webhookIdentifierPlayTime!.Value, payload);
    }
}
