// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/b52ef462f193de8563488db184be5393b9881342/Content.Server/Administration/Systems/BwoinkSystem.Arcane.cs

using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems;

/// <summary>
/// AHelp anti-spam: a short per-sender cooldown and a temporary ban for repeating the same message.
/// </summary>
public sealed partial class BwoinkSystem
{
    [Dependency] private IBanManager _banManager = default!;

    private static readonly TimeSpan AntiSpamCooldown = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Only messages this recent count towards a repeat; the same short answer to separate admin questions is not spam.
    /// </summary>
    private static readonly TimeSpan AntiSpamRepeatWindow = TimeSpan.FromMinutes(1);
    private const int AntiSpamMaxRecentMessages = 10;
    private const int AntiSpamRepeatCount = 5;
    private const int AntiSpamBanMinutes = 10;

    private readonly Dictionary<NetUserId, Queue<(string Text, TimeSpan Timestamp)>> _recentAHelpMessages = [];

    /// <summary>
    /// Returns true if the message must be dropped. Bans the sender if they keep repeating the same text.
    /// </summary>
    private bool CheckAHelpSpam(ICommonSession sender, string text)
    {
        var now = _timing.RealTime;
        if (!_recentAHelpMessages.TryGetValue(sender.UserId, out var queue))
        {
            queue = [];
            _recentAHelpMessages[sender.UserId] = queue;
        }

        if (queue.Count > 0 && now - queue.Last().Timestamp < AntiSpamCooldown)
            return true;

        while (queue.Count > 0 && now - queue.Peek().Timestamp > AntiSpamRepeatWindow)
            queue.Dequeue();

        // Spam is this text repeated right after the same text several times within the window.
        var isSpam = queue.Count >= AntiSpamRepeatCount &&
                     queue.Skip(queue.Count - AntiSpamRepeatCount).All(msg => msg.Text == text);

        queue.Enqueue((text, now));
        if (queue.Count > AntiSpamMaxRecentMessages)
            queue.Dequeue();

        if (!isSpam)
            return false;

        queue.Clear();

        var ban = new CreateServerBanInfo(Loc.GetString("bwoink-anti-spam-ban-reason"));
        ban.AddUser(sender.UserId, sender.Name);
        ban.WithMinutes(AntiSpamBanMinutes);
        ban.WithSeverity(NoteSeverity.High);
        _banManager.CreateServerBan(ban);
        return true;
    }
}
