// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Nebula.PointTether;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server._Nebula.InfinityDorm;
using Content.Shared._Nebula.Summoning;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Buckle;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Station.Systems;
using Content.Trauma.Common.Effects;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Nebula.Summoning;

public sealed partial class SummonSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PointTetherSystem _tether = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private ServerGameTicker _gameTicker = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private CommonSparksSystem _sparks = default!;
    [Dependency] private InfinityDormSystem _dorm = default!;

    private static readonly TimeSpan InviteTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan InviteDelay = TimeSpan.FromSeconds(2);
    private static readonly SoundSpecifier SummonSound = new SoundPathSpecifier("/Audio/_Nebula/Effects/summon.ogg");
    private const int MaxRefusalLength = 256;
    private const int ChantCount = 3;

    /// <summary>
    /// Pending invites keyed by the invited entity.
    /// </summary>
    private readonly Dictionary<EntityUid, SummonInvite> _invites = [];

    private readonly List<EntityUid> _expiredInvites = [];

    private readonly List<EntityUid> _returning = [];
    private TimeSpan _nextUpdate = TimeSpan.Zero;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + TimeSpan.FromSeconds(1);

        foreach (var (target, invite) in _invites)
        {
            if (now >= invite.Deadline)
                _expiredInvites.Add(target);
        }

        foreach (var target in _expiredInvites)
        {
            if (!_invites.Remove(target, out var invite))
                continue;

            invite.Eui.Close();
            OnRefused(invite, null);
        }

        _expiredInvites.Clear();
    }

    [SubscribeLocalEvent]
    private void OnRoundCleanup(RoundRestartCleanupEvent _)
    {
        _expiredInvites.AddRange(_invites.Keys);
        foreach (var target in _expiredInvites)
        {
            if (_invites.Remove(target, out var invite))
                invite.Eui.Close();
        }

        _invites.Clear();
        _expiredInvites.Clear();
        _returning.Clear();
    }

    #region Chalk

    [SubscribeLocalEvent]
    private void OnChalkAfterInteract(Entity<SummonChalkComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!IsPresetAllowed(ent.Comp))
        {
            _popup.PopupEntity(Loc.GetString("summon-chalk-popup-no-magic"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        if (!_turf.TryGetTileRef(args.ClickLocation, out var tile)
            || _turf.IsSpace(tile.Value)
            || _turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
        {
            _popup.PopupEntity(Loc.GetString("summon-chalk-popup-bad-tile"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        args.Handled = true;

        if (HasRune(tile.Value))
        {
            _popup.PopupEntity(Loc.GetString("summon-chalk-popup-occupied"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        var coords = _map.ToCenterCoordinates(tile.Value);
        var doAfter = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.DrawDelay,
            new SummonDrawDoAfterEvent(GetNetCoordinates(coords)),
            ent.Owner,
            used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString("summon-chalk-popup-start", ("user", args.User)),
            args.User, PopupType.Medium);
    }

    [SubscribeLocalEvent]
    private void OnChalkDoAfter(Entity<SummonChalkComponent> ent, ref SummonDrawDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        var coords = GetCoordinates(args.Coordinates);
        if (!_turf.TryGetTileRef(coords, out var tile) || HasRune(tile.Value))
            return;

        var rune = Spawn(ent.Comp.Rune, _map.ToCenterCoordinates(tile.Value));
        _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):user} drew {ToPrettyString(rune):rune}");

        QueueDel(ent);
    }

    private bool HasRune(TileRef tile)
    {
        if (!TryComp<MapGridComponent>(tile.GridUid, out var grid))
            return false;

        var anchored = _map.GetAnchoredEntities(tile.GridUid, grid, tile.GridIndices);
        while (anchored.MoveNext(out var uid))
        {
            if (HasComp<SummonRuneComponent>(uid))
                return true;
        }

        return false;
    }

    private bool IsPresetAllowed(SummonChalkComponent chalk)
    {
        var query = EntityQueryEnumerator<SecretRuleComponent>();
        while (query.MoveNext(out var secret))
        {
            if (secret.SelectedPreset is { } picked)
                return chalk.AllowedPresets.Contains(picked);
        }

        return _gameTicker.CurrentPreset is { } preset && chalk.AllowedPresets.Contains(preset.ID);
    }

    #endregion

    #region Rune

    [SubscribeLocalEvent]
    private void OnRuneActivate(Entity<SummonRuneComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        args.Handled = true;

        var user = args.User;
        if (ent.Comp.Summoned is { } summoned)
        {
            if (summoned != user)
                return;

            OpenConfirm(actor.PlayerSession,
                Loc.GetString("summon-return-window-title"),
                Loc.GetString("summon-return-window-text"),
                TimeSpan.Zero,
                () =>
                {
                    if (ent.Comp.Summoned == user)
                        Return(user);
                });
            return;
        }

        if (!CheckRuneReady(ent, user))
            return;

        OpenConfirm(actor.PlayerSession,
            Loc.GetString("summon-attempt-window-title"),
            Loc.GetString("summon-attempt-window-text"),
            TimeSpan.Zero,
            () => OpenSelect(ent, user));
    }

    [SubscribeLocalEvent]
    private void OnRuneMapInit(Entity<SummonRuneComponent> ent, ref MapInitEvent args)
    {
        if (Transform(ent).GridUid is { } grid)
            EnsureComp<SummonRuneGridComponent>(grid);
    }

    [SubscribeLocalEvent]
    private void OnRuneGridTerminating(Entity<SummonRuneGridComponent> ent, ref EntityTerminatingEvent args)
    {
        var query = EntityQueryEnumerator<SummonedComponent>();
        while (query.MoveNext(out var uid, out var summoned))
        {
            if (Exists(summoned.Rune) && Transform(summoned.Rune).GridUid == ent.Owner)
                _returning.Add(uid);
        }

        foreach (var uid in _returning)
        {
            if (!TerminatingOrDeleted(uid))
                Return(uid);
        }

        _returning.Clear();
    }

    [SubscribeLocalEvent]
    private void OnRuneTerminating(Entity<SummonRuneComponent> ent, ref EntityTerminatingEvent args)
    {
        foreach (var (target, invite) in _invites)
        {
            if (invite.Rune == ent.Owner)
                _expiredInvites.Add(target);
        }

        foreach (var target in _expiredInvites)
        {
            if (_invites.Remove(target, out var invite))
                invite.Eui.Close();
        }

        _expiredInvites.Clear();

        if (ent.Comp.Summoned is not { } summoned)
            return;

        ent.Comp.Summoned = null;

        if (!TerminatingOrDeleted(summoned))
            Return(summoned, ent.Owner);
    }

    private bool CheckRuneReady(Entity<SummonRuneComponent> ent, EntityUid user)
    {
        if (ent.Comp.Summoned != null)
            return false;

        if (!_mobState.IsAlive(user) || !_interaction.InRangeUnobstructed(user, ent.Owner, popup: true))
            return false;

        if (_timing.CurTime < ent.Comp.NextUse)
        {
            _popup.PopupEntity(Loc.GetString("summon-rune-popup-cooldown"), ent, user, PopupType.MediumCaution);
            return false;
        }

        return true;
    }

    private void OpenSelect(Entity<SummonRuneComponent> ent, EntityUid user)
    {
        if (TerminatingOrDeleted(ent) || !CheckRuneReady(ent, user))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        List<SummonCandidate> candidates = [];
        var query = EntityQueryEnumerator<RuneSummonableComponent, HumanoidProfileComponent, ActorComponent>();
        while (query.MoveNext(out var uid, out _, out var profile, out _))
        {
            if (uid == user
                || HasComp<SummonedComponent>(uid)
                || _invites.ContainsKey(uid)
                || !_mobState.IsAlive(uid))
                continue;

            candidates.Add(new SummonCandidate
            {
                Entity = GetNetEntity(uid),
                Label = GetLabel(uid, profile),
            });
        }

        if (candidates.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("summon-rune-popup-nobody"), ent, user, PopupType.MediumCaution);
            return;
        }

        candidates.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.CurrentCulture));

        _eui.OpenEui(new SummonSelectEui(candidates, target => SendInvite(ent, user, GetEntity(target))),
            actor.PlayerSession);
    }

    #endregion

    #region Invite

    private void SendInvite(Entity<SummonRuneComponent> ent, EntityUid summoner, EntityUid target)
    {
        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(summoner) || !CheckRuneReady(ent, summoner))
            return;

        if (!HasComp<RuneSummonableComponent>(target)
            || HasComp<SummonedComponent>(target)
            || _invites.ContainsKey(target)
            || !_mobState.IsAlive(target)
            || !TryComp<ActorComponent>(target, out var actor))
        {
            _popup.PopupEntity(Loc.GetString("summon-rune-popup-taken"), ent, summoner, PopupType.MediumCaution);
            return;
        }

        var now = _timing.CurTime;
        ent.Comp.LastAttempt = now;
        ent.Comp.NextUse = now + ent.Comp.Cooldown;

        var invite = new SummonInvite
        {
            Rune = ent.Owner,
            Summoner = summoner,
            Target = target,
            Attempt = now,
            Deadline = now + InviteTimeout,
        };

        var summonerLabel = TryComp<HumanoidProfileComponent>(summoner, out var profile)
            ? GetLabel(summoner, profile)
            : Name(summoner);

        invite.Eui = new SummonConfirmEui(Loc.GetString("summon-invite-window-title"),
            Loc.GetString("summon-invite-window-text", ("summoner", summonerLabel)),
            InviteDelay,
            () => OnInviteAnswered(target, true),
            () => OnInviteAnswered(target, false),
            () => OnInviteClosed(target));

        _invites[target] = invite;
        _eui.OpenEui(invite.Eui, actor.PlayerSession);

        _popup.PopupEntity(Loc.GetString("summon-rune-popup-calling"), ent, summoner);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(summoner):user} tried to summon {ToPrettyString(target):target} via {ToPrettyString(ent):rune}");
    }

    private void OnInviteAnswered(EntityUid target, bool accepted)
    {
        if (!_invites.Remove(target, out var invite))
            return;

        if (!accepted)
        {
            if (!TryComp<ActorComponent>(target, out var actor))
            {
                OnRefused(invite, null);
                return;
            }

            _eui.OpenEui(new SummonRefusalEui(text => OnRefused(invite, text)), actor.PlayerSession);
            return;
        }

        if (!TryComp<SummonRuneComponent>(invite.Rune, out var rune)
            || TerminatingOrDeleted(invite.Rune)
            || rune.Summoned != null
            || HasComp<SummonedComponent>(target)
            || !_mobState.IsAlive(target))
        {
            _popup.PopupEntity(Loc.GetString("summon-invite-popup-failed"), target, target, PopupType.MediumCaution);
            return;
        }

        Summon((invite.Rune, rune), invite.Summoner, target);
    }

    private void OnInviteClosed(EntityUid target)
    {
        if (_invites.Remove(target, out var invite))
            OnRefused(invite, null);
    }

    private void OnRefused(SummonInvite invite, string? text)
    {
        if (TryComp<SummonRuneComponent>(invite.Rune, out var rune)
            && rune.Summoned == null
            && rune.LastAttempt == invite.Attempt)
            rune.NextUse = rune.LastAttempt + rune.RefusalCooldown;

        if (!TryComp<ActorComponent>(invite.Summoner, out var actor))
            return;

        text = text?.Trim();
        if (text?.Length > MaxRefusalLength)
            text = text[..MaxRefusalLength];

        var message = string.IsNullOrEmpty(text)
            ? Loc.GetString("summon-refusal-silent")
            : Loc.GetString("summon-refusal-text", ("text", FormattedMessage.EscapeText(text)));

        _chatManager.ChatMessageToOne(ChatChannel.Local, message, message, EntityUid.Invalid, false,
            actor.PlayerSession.Channel);
    }

    private void OpenConfirm(ICommonSession session, string title, string text, TimeSpan delay, Action onAccept)
    {
        _eui.OpenEui(new SummonConfirmEui(title, text, delay, onAccept, null), session);
    }

    private string GetLabel(EntityUid uid, HumanoidProfileComponent profile)
    {
        var gender = Loc.GetString(profile.Gender switch
        {
            Gender.Male => "summon-gender-male",
            Gender.Female => "summon-gender-female",
            Gender.Neuter => "summon-gender-neuter",
            _ => "summon-gender-epicene",
        });

        var species = !string.IsNullOrWhiteSpace(profile.CustomSpecies)
            ? profile.CustomSpecies
            : _proto.TryIndex<SpeciesPrototype>(profile.Species, out var speciesProto)
                ? Loc.GetString(speciesProto.Name)
                : profile.Species.Id;

        return Loc.GetString("summon-candidate-label", ("gender", gender), ("species", species), ("name", Name(uid)));
    }

    #endregion

    private sealed class SummonInvite
    {
        public EntityUid Rune;
        public EntityUid Summoner;
        public EntityUid Target;
        public TimeSpan Attempt;
        public TimeSpan Deadline;
        public SummonConfirmEui Eui = default!;
    }
}
