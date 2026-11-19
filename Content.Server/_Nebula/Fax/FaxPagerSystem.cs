// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared._Nebula.Fax;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Fax.Components;
using Content.Shared.Interaction;
using Content.Shared.Station.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Nebula.Fax;

public sealed partial class FaxPagerSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FaxPagerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FaxPagerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<FaxPagerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<FaxPagerComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<FaxMachineComponent, FaxReceivedEvent>(OnFaxReceived);
    }

    private void OnMapInit(Entity<FaxPagerComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.AutoLink)
            LinkStation(ent, null);
    }

    private void OnAfterInteract(Entity<FaxPagerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target
            || !TryComp<FaxMachineComponent>(target, out var fax))
            return;

        args.Handled = true;
        ent.Comp.Faxes.RemoveAll(uid => !Exists(uid));

        if (ent.Comp.Faxes.Contains(target))
        {
            Unlink(ent, target, args.User);
            return;
        }

        if (ent.Comp.Faxes.Count >= ent.Comp.MaxFaxes)
        {
            _popup.PopupEntity(Loc.GetString("fax-pager-full"), ent, args.User);
            return;
        }

        ent.Comp.Faxes.Add(target);
        _popup.PopupEntity(Loc.GetString("fax-pager-linked", ("fax", fax.FaxName)), ent, args.User);
    }

    private void OnExamined(Entity<FaxPagerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var names = new List<string>();
        foreach (var uid in ent.Comp.Faxes)
        {
            if (TryComp<FaxMachineComponent>(uid, out var fax))
                names.Add(fax.FaxName);
        }

        args.PushMarkup(names.Count == 0
            ? Loc.GetString("fax-pager-examine-none")
            : Loc.GetString("fax-pager-examine",
                ("faxes", FormattedMessage.EscapeText(string.Join(", ", names))),
                ("count", names.Count),
                ("max", ent.Comp.MaxFaxes)));
    }

    private void OnGetVerbs(Entity<FaxPagerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        if (ent.Comp.AutoLink)
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("fax-pager-verb-link-station"),
                Act = () => LinkStation(ent, user),
            });
        }

        var category = new VerbCategory("fax-pager-verb-unlink", null);
        foreach (var uid in ent.Comp.Faxes)
        {
            if (!TryComp<FaxMachineComponent>(uid, out var fax))
                continue;

            var target = uid;
            args.Verbs.Add(new Verb
            {
                Text = fax.FaxName,
                Category = category,
                IconEntity = GetNetEntity(target),
                Act = () => Unlink(ent, target, user),
            });
        }
    }

    private void Unlink(Entity<FaxPagerComponent> ent, EntityUid fax, EntityUid user)
    {
        if (!ent.Comp.Faxes.Remove(fax))
            return;

        var name = CompOrNull<FaxMachineComponent>(fax)?.FaxName ?? Loc.GetString("fax-machine-popup-source-unknown");
        _popup.PopupEntity(Loc.GetString("fax-pager-unlinked", ("fax", name)), ent, user);
    }

    private void LinkStation(Entity<FaxPagerComponent> ent, EntityUid? user)
    {
        var station = _station.GetOwningStation(ent);
        var grid = Transform(ent).GridUid;
        if (station == null && grid == null)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("fax-pager-no-grid"), ent, user.Value);
            return;
        }

        ent.Comp.Faxes.RemoveAll(uid => !Exists(uid));

        var added = 0;
        var query = EntityQueryEnumerator<FaxMachineComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (ent.Comp.Faxes.Count >= ent.Comp.MaxFaxes)
                break;

            if (ent.Comp.Faxes.Contains(uid))
                continue;

            if (station != null ? _station.GetOwningStation(uid, xform) != station : xform.GridUid != grid)
                continue;

            ent.Comp.Faxes.Add(uid);
            added++;
        }

        if (user != null)
            _popup.PopupEntity(Loc.GetString("fax-pager-linked-station", ("count", added)), ent, user.Value);
    }

    private void OnFaxReceived(Entity<FaxMachineComponent> ent, ref FaxReceivedEvent args)
    {
        var query = EntityQueryEnumerator<FaxPagerComponent>();
        while (query.MoveNext(out var uid, out var pager))
        {
            if (!pager.Faxes.Contains(ent))
                continue;

            _audio.PlayPvs(pager.BuzzSound, uid);

            if (GetHolder(uid) is not { } session)
                continue;

            var message = Loc.GetString("fax-pager-message",
                ("fax", ent.Comp.FaxName),
                ("from", args.FromName));
            var wrappedMessage = Loc.GetString("fax-pager-message-wrap",
                ("message", FormattedMessage.EscapeText(message)));

            _chat.ChatMessageToOne(ChatChannel.Notifications,
                message,
                wrappedMessage,
                EntityUid.Invalid,
                false,
                session.Channel);
        }
    }

    private ICommonSession? GetHolder(EntityUid uid)
    {
        var current = uid;
        while (_container.TryGetContainingContainer((current, null, null), out var container))
        {
            current = container.Owner;
            if (TryComp<ActorComponent>(current, out var actor))
                return actor.PlayerSession;
        }

        return null;
    }
}
