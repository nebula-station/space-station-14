// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.Bodycam;

public abstract partial class SharedBodycamSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly SpriteSpecifier ToggleIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodycamComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<BodycamComponent, ExaminedEvent>(OnExamined);
    }

    private void OnGetVerbs(Entity<BodycamComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        var enable = !ent.Comp.Enabled;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(enable ? "bodycam-verb-enable" : "bodycam-verb-disable"),
            Icon = ToggleIcon,
            Act = () => SetEnabled(ent, enable, user),
        });
    }

    private void OnExamined(Entity<BodycamComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.Enabled ? "bodycam-examine-enabled" : "bodycam-examine-disabled"));
    }

    public void SetEnabled(Entity<BodycamComponent> ent, bool enabled, EntityUid? user = null)
    {
        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, user);
        if (user != null)
            _popup.PopupEntity(Loc.GetString(enabled ? "bodycam-popup-enabled" : "bodycam-popup-disabled"), ent, user.Value);

        var ev = new BodycamToggledEvent(user, enabled);
        RaiseLocalEvent(ent, ref ev);
    }
}
