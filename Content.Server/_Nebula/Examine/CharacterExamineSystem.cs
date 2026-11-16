// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Examine;
using Content.Server.Chat.Managers;
using Content.Shared._Nebula.Examine;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Nebula.Examine;

public sealed partial class CharacterExamineSystem : SharedCharacterExamineSystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private INetConfigurationManager _netCfg = default!;

    private static readonly Color BorderColor = Color.FromHex("#282D31");

    protected override bool IsDetailedExamine(EntityUid examiner)
    {
        return TryComp<ActorComponent>(examiner, out var actor)
            && _netCfg.GetClientCVar(actor.PlayerSession.Channel, GoobCVars.DetailedExamine);
    }

    protected override void OnCharacterExamined(Entity<ExaminableCharacterComponent> ent,
        ExaminedEvent args,
        bool selfaware,
        string cansee,
        List<WornItem> worn)
    {
        if (!TryComp<ActorComponent>(args.Examiner, out var actor)
            || !_netCfg.GetClientCVar(actor.PlayerSession.Channel, GoobCVars.DetailedExamine)
            || !_netCfg.GetClientCVar(actor.PlayerSession.Channel, GoobCVars.LogInChat))
            return;

        var suffix = selfaware ? "-selfaware" : string.Empty;
        var name = Loc.GetString("examine-name" + suffix, ("name", _identity.GetEntityIdentity(ent.Owner)));

        FormattedMessage message = new();
        message.PushTag(new MarkupNode("examineborder", null, null));
        message.PushNewline();
        message.AddMarkupPermissive($"[color=DarkGray][font size=11]{name}[/font][/color]");
        message.PushNewline();
        AddLine(message);
        AddGrayLine(message, cansee);

        foreach (var item in worn)
        {
            AddGrayLine(message, Loc.GetString(item.Loc,
                ("item", item.Name),
                ("ent", ent.Owner),
                ("id", GetNetEntity(item.Item).Id),
                ("size", 14)));
        }

        if (worn.Count == 0)
            AddGrayLine(message, Loc.GetString("examine-can-see-nothing" + suffix, ("ent", ent.Owner)));

        var ev = new UserExaminedEvent(message, args.Examined);
        RaiseLocalEvent(args.Examiner, ref ev);
        message = ev.Message;

        AddLine(message);
        message.Pop();

        _chat.ChatMessageToOne(ChatChannel.Emotes,
            message.ToString(),
            message.ToMarkup(),
            EntityUid.Invalid,
            false,
            actor.PlayerSession.Channel,
            recordReplay: false,
            canCoalesce: false);
    }

    private static void AddGrayLine(FormattedMessage message, string line)
    {
        message.AddMarkupPermissive($"[color=DarkGray][font size=10]{line}[/font][/color]");
        message.PushNewline();
    }

    public void AddLine(FormattedMessage message)
    {
        message.PushColor(BorderColor);
        message.AddText(Loc.GetString("examine-border-line"));
        message.PushNewline();
        message.Pop();
    }
}
