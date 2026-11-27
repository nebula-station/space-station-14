// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/862852d48934ecca32ab2d9b44eeb3ecf73874a0/Content.Arcane.Client/Chat/DirectionalEmote/DirectionalEmoteSystem.cs

using Content.Shared._Arcane.CCVar;
using Content.Shared._Arcane.Chat;
using Content.Shared.Verbs;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client._Arcane.Chat.DirectionalEmote;

public sealed partial class DirectionalEmoteSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private static readonly SpriteSpecifier VerbIcon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/emotes.svg.192dpi.png"));

    public void Send(NetEntity target, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > _cfg.GetCVar(ArcaneCVars.DirectionalEmoteMaxLength))
            return;

        RaiseNetworkEvent(new SendDirectionalEmoteEvent(target, text));
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<DirectionalEmoteTargetComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!_cfg.GetCVar(ArcaneCVars.DirectionalEmotesEnabled)
            || args.Target == args.User
            || !HasComp<DirectionalEmoteTargetComponent>(args.User))
            return;

        var target = GetNetEntity(args.Target);
        args.Verbs.Add(new Verb
        {
            Act = () => _ui.GetUIController<DirectionalEmoteUIController>().ToggleWindow(target),
            Priority = 15,
            Text = Loc.GetString("directional-emote-verb-name"),
            Icon = VerbIcon,
            ClientExclusive = true,
        });
    }
}
