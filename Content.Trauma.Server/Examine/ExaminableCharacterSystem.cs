// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Managers;
using Content.Server._Nebula.Examine;
using Content.Shared._Nebula.Examine;
using Content.Goobstation.Common.Examine; // Goobstation Change
using Content.Goobstation.Common.CCVar; // Goobstation Change
using Content.Shared.Chat;
using Content.Shared.Examine;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using System.Globalization;

namespace Content.Trauma.Server.Examine;

public sealed partial class ExaminableCharacterSystem : EntitySystem
{
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private INetConfigurationManager _netConfigManager = default!;
    [Dependency] private CharacterExamineSystem _characterExamine = default!; // Nebula edit

    public override void Initialize()
    {
        SubscribeLocalEvent<MetaDataComponent, ExamineCompletedEvent>(HandleExamine);
    }

    private void HandleExamine(Entity<MetaDataComponent> ent, ref ExamineCompletedEvent args)
    {
        if (HasComp<ExaminableCharacterComponent>(args.Examined)
            && !args.IsSecondaryInfo)
            return;

        if (TryComp<ActorComponent>(args.Examiner, out var actorComponent)
            && _netConfigManager.GetClientCVar(actorComponent.PlayerSession.Channel, GoobCVars.DetailedExamine)
            && _netConfigManager.GetClientCVar(actorComponent.PlayerSession.Channel, GoobCVars.LogInChat))
        {
            FormattedMessage message = new();
            message.PushTag(new MarkupNode("examineborder", null, null)); // border
            message.PushNewline();
            message.Pop();

            if (!args.IsSecondaryInfo)
            {
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                var name = textInfo.ToTitleCase(ent.Comp.EntityName);
                name = FormattedMessage.EscapeText(name);
                var item = Loc.GetString("examine-present-tex",
                    ("name", name),
                    ("id", GetNetEntity(ent, ent.Comp).Id),
                    ("size", 14));
                message.AddMarkupPermissive($"[color=DarkGray][font size=11]{item}[/font][/color]");
                message.PushNewline();
            }

            _characterExamine.AddLine(message); // Nebula edit
            message.AddMarkupPermissive($"[font size=10]{args.Message.ToMarkup()}[/font]");

            var ev = new UserExaminedEvent(message, args.Examined);
            RaiseLocalEvent(args.Examiner, ref ev);
            message = ev.Message;

            message.PushNewline();
            _characterExamine.AddLine(message); // Nebula edit
            message.Pop();

            _chatManager.ChatMessageToOne(ChatChannel.Emotes,
                message.ToString(),
                message.ToMarkup(),
                EntityUid.Invalid,
                false,
                actorComponent.PlayerSession.Channel,
                recordReplay: false,
                canCoalesce: false); // Goobstation Edit
        }
    }
}
