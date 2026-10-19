// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.ERP;

public sealed partial class ERPStatusExamine : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;

    private static readonly SpriteSpecifier VerbIcon = new SpriteSpecifier.Texture(
        new("/Textures/_Nebula/Interface/ERP/heartIcon.png"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ERPManagerComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(Entity<ERPManagerComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        var user = args.User;
        var inRange = _examine.IsInDetailsRange(user, ent);

        var verb = new ExamineVerb
        {
            Act = () =>
            {
                var msg = new FormattedMessage();
                msg.AddMarkupOrThrow(Loc.GetString("erp-examine-status-header"));
                msg.AddMarkupOrThrow(Loc.GetString("erp-status-" + ent.Comp.Status));

                _examine.SendExamineTooltip(user, ent, msg, false, false);
            },
            Text = Loc.GetString("erp-examine-status-text"),
            Category = VerbCategory.Examine,
            Disabled = !inRange,
            Message = inRange ? null : Loc.GetString("erp-examine-verb-disabled"),
            Icon = VerbIcon
        };

        args.Verbs.Add(verb);
    }
}
