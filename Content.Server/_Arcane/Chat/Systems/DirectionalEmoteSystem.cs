// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/862852d48934ecca32ab2d9b44eeb3ecf73874a0/Content.Arcane.Server/Chat/DirectionalEmoteSystem.cs

using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared._Arcane.CCVar;
using Content.Shared._Arcane.Chat;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Arcane.Chat.Systems;

/// <summary>
/// Emotes seen only by the sender and one target in range.
/// </summary>
public sealed partial class DirectionalEmoteSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    [SubscribeNetworkEvent]
    private void OnSendDirectionalEmote(SendDirectionalEmoteEvent args, EntitySessionEventArgs session)
    {
        if (!_cfg.GetCVar(ArcaneCVars.DirectionalEmotesEnabled) || string.IsNullOrWhiteSpace(args.Text))
            return;

        if (session.SenderSession.AttachedEntity is not { } source
            || !TryGetEntity(args.Target, out var target)
            || target == source
            || !TryComp<ActorComponent>(target, out var targetActor)
            || !TryComp<DirectionalEmoteTargetComponent>(source, out var emote)
            || !HasComp<DirectionalEmoteTargetComponent>(target))
            return;

        var now = _timing.CurTime;
        if (emote.LastSend + emote.Cooldown > now)
            return;

        // The same permission an ordinary emote needs: not dead, crit, asleep or otherwise unable to emote.
        if (!_actionBlocker.CanEmote(source))
            return;

        var channel = session.SenderSession.Channel;
        if (!_examine.InRangeUnOccluded(source, target.Value, _cfg.GetCVar(ArcaneCVars.DirectionalEmoteRange)))
        {
            var rangeError = Loc.GetString("directional-emote-range-error");
            _chat.ChatMessageToOne(ChatChannel.Emotes, rangeError, rangeError, default, false, channel);
            return;
        }

        var maxLength = _cfg.GetCVar(ArcaneCVars.DirectionalEmoteMaxLength);
        if (args.Text.Length > maxLength)
        {
            var lengthError = Loc.GetString("directional-emote-length-error", ("maxLength", maxLength));
            _chat.ChatMessageToOne(ChatChannel.Emotes, lengthError, lengthError, default, false, channel);
            return;
        }

        var text = args.Text.Trim();
        // The apparent identity, like ordinary emotes: a masked sender is not named.
        var wrapped = Loc.GetString("chat-manager-entity-directional-emote-wrap-message",
            ("entityName", FormattedMessage.EscapeText(Name(Identity.Entity(source, EntityManager)))),
            ("message", FormattedMessage.EscapeText(text)));

        _chat.ChatMessageToMany(ChatChannel.Emotes, text, wrapped, source, false, true,
            [targetActor.PlayerSession.Channel, channel]);

        emote.LastSend = now;
        _adminLogger.Add(LogType.Chat, LogImpact.Low,
            $"{ToPrettyString(source):source} sent a directional emote to {ToPrettyString(target):target}: {text}");
    }
}
