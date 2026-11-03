// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Threading.Tasks;
using Content.Server._Nebula.Administration;
using Content.Server._Nebula.Discord;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared._Nebula.AntagTokens;
using Content.Shared._Nebula.CCVar;
using Content.Shared.Antag;
using Content.Shared.Antag.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.AntagTokens;

/// <summary>
/// Sells antagonists for tokens stored in the bot and grants them to single players during the round.
/// A token is spent at purchase and returned by a refund if the antagonist was not granted.
/// </summary>
public sealed partial class AntagTokenSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private NebulaServerApi _api = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private ServerGameTicker _ticker = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private NebulaWebhooks _webhooks = default!;

    private static readonly TimeSpan StateRequestCooldown = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ActionCooldown = TimeSpan.FromSeconds(3);

    private sealed class Purchase(Guid id, ProtoId<AntagTokenListingPrototype> listing)
    {
        public readonly Guid Id = id;
        public readonly ProtoId<AntagTokenListingPrototype> Listing = listing;
        public TimeSpan? GrantAt;
    }

    private enum GrantCheck : byte
    {
        Ready,
        Retry,
        Refund,
    }

    private readonly Dictionary<NetUserId, Purchase> _purchases = [];
    private readonly HashSet<NetUserId> _busy = [];
    private readonly HashSet<Guid> _unconfirmedFinishes = [];
    private readonly Dictionary<NetUserId, TimeSpan> _nextStateRequest = [];
    private readonly Dictionary<NetUserId, TimeSpan> _nextAction = [];
    private readonly List<(Guid Id, bool Granted)> _unsentFinishes = [];

    /// <summary>
    /// Purchases stay closed until the leftovers of the previous server run are refunded.
    /// </summary>
    public bool StartupRefundDone => _startupRefundDone;

    private bool _startupRefundDone;
    private bool _startupRefundRunning;
    private bool _flushRunning;
    private TimeSpan _nextCheck;

    public override void Initialize()
    {
        base.Initialize();

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    /// <summary>
    /// Online players needed for this listing, taken from its game rule.
    /// </summary>
    public int GetMinPlayers(AntagTokenListingPrototype listing)
    {
        return _proto.Index(listing.Rule).TryComp<GameRuleComponent>(out var rule, Factory) ? rule.MinPlayers : 0;
    }

    public bool HasPurchase(NetUserId user)
    {
        return _purchases.ContainsKey(user);
    }

    /// <summary>
    /// When the purchase will be granted, null before the round starts.
    /// </summary>
    public TimeSpan? GetGrantTime(NetUserId user)
    {
        return _purchases.GetValueOrDefault(user)?.GrantAt;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + TimeSpan.FromSeconds(_cfg.GetCVar(NCCVars.AntagTokensRetryInterval));

        RefundLeftoversAfterStartup();
        FlushUnsentFinishes();
        ProcessPurchases();
    }

    private string ServerId => _cfg.GetCVar(NCCVars.AntagTokensServerId);

    /// <summary>
    /// The list of purchases lives in memory, so after a restart this server cannot grant the old ones.
    /// </summary>
    private async void RefundLeftoversAfterStartup()
    {
        if (_startupRefundDone || _startupRefundRunning || ServerId == string.Empty)
            return;

        _startupRefundRunning = true;
        try
        {
            _startupRefundDone = await _api.RefundPendingTokenPurchases(ServerId);
        }
        finally
        {
            _startupRefundRunning = false;
        }
    }

    private async void FlushUnsentFinishes()
    {
        if (_flushRunning || _unsentFinishes.Count == 0)
            return;

        _flushRunning = true;
        try
        {
            foreach (var finish in _unsentFinishes.ToArray())
            {
                if (!await SendFinish(finish.Id, finish.Granted))
                    continue;

                _unsentFinishes.Remove(finish);
                _unconfirmedFinishes.Remove(finish.Id);
            }
        }
        finally
        {
            _flushRunning = false;
        }
    }

    private Task<bool> SendFinish(Guid id, bool granted)
    {
        return granted ? _api.MarkTokenPurchaseGranted(id) : _api.RefundTokenPurchase(id);
    }

    private async Task FinishPurchase(Guid id, bool granted)
    {
        _unconfirmedFinishes.Add(id);

        if (await SendFinish(id, granted))
            _unconfirmedFinishes.Remove(id);
        else
            _unsentFinishes.Add((id, granted));
    }

    private Task Refund(NetUserId user, Purchase purchase, string reason)
    {
        _purchases.Remove(user);

        Log.Info($"Antag token purchase {purchase.Id} of {user} ({purchase.Listing}) refunded: {reason}");
        _adminLog.Add(LogType.AntagSelection, LogImpact.Medium,
            $"Antag token purchase {purchase.Id} of {user} ({purchase.Listing}) refunded: {reason}");
        _webhooks.SendAntagTokenMessage(user, "tokens-webhook-title-antag-refund", ListingName(purchase.Listing));

        return FinishPurchase(purchase.Id, false);
    }

    private string ListingName(ProtoId<AntagTokenListingPrototype> listing)
    {
        return _proto.TryIndex(listing, out var proto) && _proto.TryIndex(proto.Antag, out var antag)
            ? Loc.GetString(antag.Name)
            : listing.Id;
    }

    [SubscribeLocalEvent]
    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.Old == GameRunLevel.InRound)
            RefundAll("round ended before the grant");

        if (ev.New == GameRunLevel.InRound)
            OnRoundStarted();

        RaiseNetworkEvent(new AntagTokenRunLevelMessage { InLobby = ev.New == GameRunLevel.PreRoundLobby });
    }

    private void RefundAll(string reason)
    {
        foreach (var (user, purchase) in _purchases.ToArray())
        {
            _ = Refund(user, purchase, reason);
        }
    }

    private void OnRoundStarted()
    {
        var preset = _ticker.CurrentPreset?.ID;
        var presets = _cfg.GetCVar(NCCVars.AntagTokensPresets)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (preset == null || !presets.Contains(preset))
        {
            RefundAll($"preset {preset} is not dynamic");
            return;
        }

        var (min, max) = WindowMinutes();
        foreach (var purchase in _purchases.Values)
        {
            // RoundStartTimeSpan still holds the previous round here: the run level changes before it is updated.
            purchase.GrantAt = _timing.CurTime + TimeSpan.FromMinutes(_random.NextFloat(min, max));
        }
    }

    private (float Min, float Max) WindowMinutes()
    {
        var min = _cfg.GetCVar(NCCVars.AntagTokensWindowMin);
        return (min, MathF.Max(min, _cfg.GetCVar(NCCVars.AntagTokensWindowMax)));
    }

    private void ProcessPurchases()
    {
        if (_ticker.RunLevel != GameRunLevel.InRound)
            return;

        var windowEnd = _ticker.RoundStartTimeSpan + TimeSpan.FromMinutes(WindowMinutes().Max);

        foreach (var (user, purchase) in _purchases.ToArray())
        {
            if (purchase.GrantAt is not { } grantAt || _timing.CurTime < grantAt)
                continue;

            switch (TryGrant(user, purchase, out var reason))
            {
                case GrantCheck.Ready:
                    break;
                case GrantCheck.Refund:
                    _ = Refund(user, purchase, reason);
                    break;
                case GrantCheck.Retry when _timing.CurTime >= windowEnd:
                    _ = Refund(user, purchase, $"window ended: {reason}");
                    break;
            }
        }
    }

    private GrantCheck TryGrant(NetUserId user, Purchase purchase, out string reason)
    {
        reason = string.Empty;

        if (!_proto.TryIndex(purchase.Listing, out var listing)
            || !_proto.Index(listing.Rule).TryComp<AntagSelectionComponent>(out var selection, Factory)
            || selection.Antags.Length == 0
            || !_proto.TryIndex(selection.Antags[0].Proto, out AntagSpecifierPrototype? definition))
        {
            reason = "invalid listing";
            Log.Error($"Antag token listing {purchase.Listing} cannot be granted");
            return GrantCheck.Refund;
        }

        if (!_player.TryGetSessionById(user, out var session)
            || session.Status != SessionStatus.InGame
            || session.AttachedEntity is not { } entity)
        {
            reason = "not in game";
            return GrantCheck.Retry;
        }

        if (HasComp<GhostComponent>(entity) || !_mobState.IsAlive(entity))
        {
            reason = "dead";
            return GrantCheck.Retry;
        }

        if (!_mind.TryGetMind(session, out var mind, out _))
        {
            reason = "no mind";
            return GrantCheck.Retry;
        }

        if (_role.MindIsAntagonist(mind))
        {
            reason = "already an antagonist";
            return GrantCheck.Refund;
        }

        if (_antag.IsAntagBanned(session, definition))
        {
            reason = "antag banned";
            return GrantCheck.Refund;
        }

        if (!_antag.IsMindValid(session, definition))
        {
            reason = "job does not fit";
            return GrantCheck.Refund;
        }

        if (!_antag.IsEntityValid(session.AttachedEntity, definition))
        {
            reason = $"character {ToPrettyString(entity)} does not fit";
            return GrantCheck.Refund;
        }

        var minPlayers = GetMinPlayers(listing);
        if (_antag.GetActivePlayerCount() < minPlayers)
        {
            reason = $"less than {minPlayers} players";
            return GrantCheck.Retry;
        }

        _antag.ForceMakeAntag(session, listing.Rule, listing.RuleComp);

        if (!_role.MindIsAntagonist(mind))
        {
            reason = "the rule did not make an antagonist";
            Log.Error($"Antag token listing {purchase.Listing} did not make {session} an antagonist");
            return GrantCheck.Refund;
        }

        _purchases.Remove(user);
        _ = FinishPurchase(purchase.Id, true);

        _adminLog.Add(LogType.AntagSelection, LogImpact.High,
            $"Antag token purchase {purchase.Id}: {session} became {purchase.Listing}");
        _webhooks.SendAntagTokenMessage(user, "tokens-webhook-title-antag-grant", ListingName(purchase.Listing));

        return GrantCheck.Ready;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus is SessionStatus.Connected or SessionStatus.Disconnected)
            RaiseNetworkEvent(new AntagTokenOnlineMessage { Online = _player.PlayerCount });

        if (args.NewStatus != SessionStatus.Disconnected)
            return;

        _nextStateRequest.Remove(args.Session.UserId);
        _nextAction.Remove(args.Session.UserId);
    }

    private bool TryPassCooldown(Dictionary<NetUserId, TimeSpan> nextRequest, NetUserId user, TimeSpan cooldown)
    {
        if (_timing.CurTime < nextRequest.GetValueOrDefault(user))
            return false;

        nextRequest[user] = _timing.CurTime + cooldown;
        return true;
    }

    private bool TryPassActionCooldown(ICommonSession session)
    {
        if (TryPassCooldown(_nextAction, session.UserId, ActionCooldown))
            return true;

        RaiseNetworkEvent(new AntagTokenErrorMessage { Error = AntagTokenError.Busy }, session);
        return false;
    }

    [SubscribeNetworkEvent]
    private void OnStateRequest(AntagTokenStateRequest msg, EntitySessionEventArgs args)
    {
        if (TryPassCooldown(_nextStateRequest, args.SenderSession.UserId, StateRequestCooldown))
            _ = SendState(args.SenderSession, AntagTokenError.None);
    }

    [SubscribeNetworkEvent]
    private void OnBuyRequest(AntagTokenBuyRequest msg, EntitySessionEventArgs args)
    {
        if (TryPassActionCooldown(args.SenderSession))
            _ = Buy(args.SenderSession, msg.Listing);
    }

    [SubscribeNetworkEvent]
    private void OnCancelRequest(AntagTokenCancelRequest msg, EntitySessionEventArgs args)
    {
        if (TryPassActionCooldown(args.SenderSession))
            _ = Cancel(args.SenderSession);
    }

    /// <summary>
    /// Buys the listing for the player and sends them the new state.
    /// </summary>
    public async Task Buy(ICommonSession session, ProtoId<AntagTokenListingPrototype> listingId)
    {
        var user = session.UserId;

        if (!_proto.TryIndex(listingId, out var listing))
            return;

        var error = CheckCanBuy(user, listing);
        if (error != AntagTokenError.None)
        {
            await SendState(session, error);
            return;
        }

        var purchaseId = Guid.NewGuid();

        _busy.Add(user);
        TokenPurchaseResult result;
        try
        {
            result = await _api.PurchaseToken(user.UserId, purchaseId, listing.ID, ServerId);
        }
        finally
        {
            _busy.Remove(user);
        }

        switch (result)
        {
            case TokenPurchaseResult.Created when _ticker.RunLevel != GameRunLevel.PreRoundLobby:
                // The round started while the bot was answering, this purchase has no grant time.
                await FinishPurchase(purchaseId, false);
                error = AntagTokenError.NotInLobby;
                break;
            case TokenPurchaseResult.Created:
                _purchases[user] = new Purchase(purchaseId, listing.ID);
                _adminLog.Add(LogType.AntagSelection, LogImpact.Medium,
                    $"Antag token purchase {purchaseId}: {session} bought {listing.ID}");
                _webhooks.SendAntagTokenMessage(user, "tokens-webhook-title-antag-buy", ListingName(listing.ID));
                break;
            case TokenPurchaseResult.NotLinked:
                error = AntagTokenError.NotLinked;
                break;
            case TokenPurchaseResult.InsufficientTokens:
                error = AntagTokenError.InsufficientTokens;
                break;
            case TokenPurchaseResult.ActivePurchase:
                error = AntagTokenError.ActivePurchase;
                break;
            case TokenPurchaseResult.Unknown:
                await FinishPurchase(purchaseId, false);
                error = AntagTokenError.Unavailable;
                break;
        }

        await SendState(session, error);
    }

    private AntagTokenError CheckCanBuy(NetUserId user, AntagTokenListingPrototype listing)
    {
        if (_ticker.RunLevel != GameRunLevel.PreRoundLobby)
            return AntagTokenError.NotInLobby;

        if (ServerId == string.Empty || !_startupRefundDone)
            return AntagTokenError.Unavailable;

        if (_busy.Contains(user))
            return AntagTokenError.Busy;

        if (_purchases.ContainsKey(user))
            return AntagTokenError.ActivePurchase;

        return _player.PlayerCount < GetMinPlayers(listing)
            ? AntagTokenError.NotEnoughPlayers
            : AntagTokenError.None;
    }

    /// <summary>
    /// Cancels the player's purchase on this server, possible only in the lobby.
    /// </summary>
    public async Task Cancel(ICommonSession session)
    {
        var user = session.UserId;
        var error = AntagTokenError.None;

        if (_ticker.RunLevel != GameRunLevel.PreRoundLobby)
            error = AntagTokenError.NotInLobby;
        else if (_purchases.TryGetValue(user, out var purchase))
            await Refund(user, purchase, "cancelled by the player");

        await SendState(session, error);
    }

    /// <summary>
    /// A purchase of this server that it does not hold can never be granted. It appears when the bot saved a purchase
    /// after answering too late, so the refund for it came first and found nothing.
    /// </summary>
    /// <returns>Whether a purchase was refunded.</returns>
    private async Task<bool> RefundLostPurchase(NetUserId user, TokenStatus? status)
    {
        // A finished purchase can still look active here if the bot has not got the call yet.
        if (status?.ActivePurchase is not { } active
            || active.ServerId != ServerId
            || _busy.Contains(user)
            || _unconfirmedFinishes.Contains(active.Id)
            || _purchases.Values.Any(p => p.Id == active.Id))
        {
            return false;
        }

        Log.Warning($"Antag token purchase {active.Id} of {user} ({active.ListingId}) is lost, refunding it");
        _adminLog.Add(LogType.AntagSelection, LogImpact.Medium,
            $"Antag token purchase {active.Id} of {user} ({active.ListingId}) refunded: lost by the server");

        return await _api.RefundTokenPurchase(active.Id);
    }

    private async Task SendState(ICommonSession session, AntagTokenError error)
    {
        var state = new AntagTokenStateMessage
        {
            Error = error,
        };

        foreach (var listing in _proto.EnumeratePrototypes<AntagTokenListingPrototype>().OrderBy(l => l.ID))
        {
            state.Listings.Add(new AntagTokenListingState(listing.ID, GetMinPlayers(listing)));
        }

        var (answered, status) = await _api.GetTokenStatus(session.UserId.UserId);

        if (await RefundLostPurchase(session.UserId, status))
            (answered, status) = await _api.GetTokenStatus(session.UserId.UserId);

        // The round may have moved on while the bot was answering.
        state.InLobby = _ticker.RunLevel == GameRunLevel.PreRoundLobby;
        state.Online = _player.PlayerCount;

        state.Available = answered && ServerId != string.Empty && _startupRefundDone;
        state.Linked = status != null;
        state.Balance = status?.Balance ?? 0;
        state.WeeklyTokens = status?.WeeklyTokens ?? 0;
        state.WeeklyTokenCap = status?.WeeklyTokenCap ?? 0;

        if (status?.NextWeeklyGrantAt is { } nextGrant)
            state.NextWeeklyGrantIn = nextGrant - DateTimeOffset.UtcNow;

        if (status?.ActivePurchase is { } active && _proto.HasIndex<AntagTokenListingPrototype>(active.ListingId))
        {
            state.ActiveListing = active.ListingId;
            state.ActiveElsewhere = active.ServerId != ServerId;
        }

        if (session.Status != SessionStatus.Disconnected)
            RaiseNetworkEvent(state, session);
    }
}
