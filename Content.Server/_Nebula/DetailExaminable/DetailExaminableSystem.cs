// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.EUI;
using Content.Server.Mind;
using Content.Shared._Nebula.DetailExaminable;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Nebula.DetailExaminable;

public sealed partial class DetailExaminableSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private EuiManager _euiMan = default!;
    [Dependency] private MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(Entity<HumanoidProfileComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        var detailsRange = _examine.IsInDetailsRange(args.User, ent);

        var user = args.User;

        var verb = new ExamineVerb
        {
            Act = () => OpenEui(user, ent.Owner, ent.Comp),
            Text = Loc.GetString("detail-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("detail-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    private void OpenEui(EntityUid user, EntityUid target, HumanoidProfileComponent profile)
    {
        if (!_mind.TryGetMind(user, out _, out var mind)
            || mind is not { UserId: not null } || !_player.TryGetSessionById(mind.UserId, out var session))
            return;

        var state = new DetailExaminableEuiState
        {
            Target = GetNetEntity(target),
            Name = Identity.Name(target, EntityManager),
            SpeciesName = string.IsNullOrEmpty(profile.Species)
                ? Loc.GetString($"species-name-{profile.Species.ToString().ToLower()}") : profile.CustomSpecies,
            Sex = profile.Sex,
            Gender = profile.Gender,
            Data = profile.DetailExaminableData,
        };

        var window = new DetailExaminableEui(state);
        _euiMan.OpenEui(window, session);
        window.StateDirty();
    }
}
