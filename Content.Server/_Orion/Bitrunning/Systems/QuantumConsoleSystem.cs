// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/_Orion/Bitrunning/Systems/QuantumConsoleSystem.cs

using System.Linq;
using Content.Server._Orion.Bitrunning.Components;
using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed partial class QuantumConsoleSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private QuantumServerSystem _server = default!;
    [Dependency] private BitrunningDomainSystem _domains = default!;
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan UiRefresh = TimeSpan.FromSeconds(1);
    private const string ServerSinkPort = "BitrunningConsoleSink";
    private TimeSpan _nextRefresh;
    private readonly HashSet<EntityUid> _openUis = [];
    private readonly List<BitrunningDomainListing> _domainBuffer = [];
    private readonly List<BitrunningOccupantListing> _occupantBuffer = [];

    [SubscribeLocalEvent]
    private void OnInit(Entity<QuantumConsoleComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent.Owner, ServerSinkPort);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<QuantumConsoleComponent> ent, ref MapInitEvent args)
    {
        RefreshLinkedServer(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextRefresh)
            return;

        _nextRefresh = _timing.CurTime + UiRefresh;

        if (_openUis.Count == 0)
            return;

        List<EntityUid>? removedUids = null;
        foreach (var uid in _openUis)
        {
            if (!TryComp<QuantumConsoleComponent>(uid, out var comp) || !_ui.IsUiOpen(uid, QuantumConsoleUiKey.Key))
            {
                removedUids ??= new();
                removedUids.Add(uid);
                continue;
            }

            UpdateUi((uid, comp));
        }

        if (removedUids == null)
            return;

        foreach (var uid in removedUids)
        {
            _openUis.Remove(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnUiOpened(Entity<QuantumConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        _openUis.Add(ent.Owner);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnUiClosed(Entity<QuantumConsoleComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!_ui.IsUiOpen(ent.Owner, QuantumConsoleUiKey.Key))
            _openUis.Remove(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnConsoleShutdown(Entity<QuantumConsoleComponent> ent, ref ComponentShutdown args)
    {
        _openUis.Remove(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnLoadDomain(Entity<QuantumConsoleComponent> ent, ref QuantumConsoleLoadDomainMessage args)
    {
        var server = FindServer(ent);
        if (server == null)
            return;

        _server.TryColdBoot(server.Value, args.DomainId);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnRandomDomain(Entity<QuantumConsoleComponent> ent, ref QuantumConsoleRandomDomainMessage args)
    {
        var server = FindServer(ent);
        if (server == null)
            return;

        var domain = _server.GetRandomDomainId(server.Value);
        if (domain == null)
            return;

        _server.TryColdBoot(server.Value, domain, true);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnStopDomain(Entity<QuantumConsoleComponent> ent, ref QuantumConsoleStopDomainMessage args)
    {
        var serverUid = FindServer(ent);
        if (serverUid == null || !TryComp<QuantumServerComponent>(serverUid, out var serverComp))
            return;

        _server.StopDomain((serverUid.Value, serverComp));
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnRefresh(Entity<QuantumConsoleComponent> ent, ref QuantumConsoleRefreshMessage args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnBroadcast(Entity<QuantumConsoleComponent> ent, ref QuantumConsoleBroadcastMessage args)
    {
        var serverUid = FindServer(ent);
        if (serverUid == null || !HasComp<QuantumServerComponent>(serverUid))
            return;

        _server.SetBroadcastState(serverUid.Value, args.Enabled);
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<QuantumConsoleComponent> ent)
    {
        var serverUid = FindServer(ent);
        if (serverUid == null || !TryComp<QuantumServerComponent>(serverUid, out var server))
        {
            _ui.SetUiState(ent.Owner,
                QuantumConsoleUiKey.Key,
                new QuantumConsoleBoundUiState(
                connected: false,
                server: null,
                currentDomain: null,
                occupants: 0,
                connectedPods: 0,
                serverPoints: 0,
                scannerTier: 0,
                state: BitrunningServerState.Ready,
                broadcast: false,
                extremeDifficultyUnlocked: false,
                cooldownTotalSeconds: 0f,
                cooldownEndTime: TimeSpan.Zero, // Nebula edit
                domains: [],
                connectedAvatars: []));
            return;
        }

        _domainBuffer.Clear();
        var emagged = HasComp<EmaggedComponent>(serverUid.Value);
        foreach (var domain in _domains.GetAllDomains()) // Nebula edit
        {
            if (domain.Difficulty == BitrunningDifficulty.Extreme && !emagged)
                continue;

            _domainBuffer.Add(new BitrunningDomainListing(
                domain.ID,
                _domains.GetDisplayName(domain, server.ScannerTier, server.Points),
                _domains.GetDisplayDescription(domain, server.ScannerTier, server.Points),
                domain.Cost,
                _domains.GetDisplayReward(domain, server.ScannerTier, server.Points),
                domain.Difficulty,
                domain.IsModular,
                domain.HasSecondaryObjectives));
        }

        _occupantBuffer.Clear();
        foreach (var uid in server.ActiveConnections)
        {
            if (!Exists(uid))
                continue;

            var name = Name(uid);
            var noHit = CompOrNull<AvatarConnectionComponent>(uid)?.NoHit ?? false;
            _occupantBuffer.Add(new BitrunningOccupantListing(name, noHit));
        }

        var cooldownTotal = (float) server.Cooldown.TotalSeconds;
        var connectedPods = 0;
        var netpodQuery = EntityQueryEnumerator<NetpodComponent>();
        while (netpodQuery.MoveNext(out _, out var pod))
        {
            if (pod.LinkedServer == serverUid)
                connectedPods++;
        }

        _ui.SetUiState(ent.Owner,
            QuantumConsoleUiKey.Key,
            new QuantumConsoleBoundUiState(
            connected: true,
            server: GetNetEntity(serverUid.Value),
            currentDomain: server.CurrentDomain,
            occupants: server.ActiveConnections.Count,
            connectedPods: connectedPods,
            serverPoints: server.Points,
            scannerTier: server.ScannerTier,
            state: server.State,
            broadcast: server.BroadcastEnabled,
            extremeDifficultyUnlocked: emagged,
            cooldownTotalSeconds: cooldownTotal,
            cooldownEndTime: server.CooldownEndTime, // Nebula edit
            domains: _domainBuffer.ToList(),
            connectedAvatars: _occupantBuffer.ToList()));
    }

    private EntityUid? FindServer(Entity<QuantumConsoleComponent> ent)
    {
        if (ent.Comp.LinkedServerId is { } linkedUid && Exists(linkedUid) && HasComp<QuantumServerComponent>(linkedUid))
            return linkedUid;

        RefreshLinkedServer(ent);
        return ent.Comp.LinkedServerId;
    }

    [SubscribeLocalEvent]
    private void OnNewLink(Entity<QuantumConsoleComponent> ent, ref NewLinkEvent args)
    {
        if (args.Sink != ent.Owner || args.SinkPort != ServerSinkPort)
            return;

        if (!HasComp<QuantumServerComponent>(args.Source))
            return;

        ent.Comp.LinkedServerId = args.Source;
    }

    [SubscribeLocalEvent]
    private static void OnPortDisconnected(Entity<QuantumConsoleComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != ServerSinkPort || ent.Comp.LinkedServerId != args.RemovedPortUid)
            return;

        ent.Comp.LinkedServerId = null;
    }

    private void RefreshLinkedServer(Entity<QuantumConsoleComponent> ent)
    {
        if (!TryComp<DeviceLinkSinkComponent>(ent.Owner, out var sink))
            return;

        foreach (var source in sink.LinkedSources)
        {
            if (!HasComp<QuantumServerComponent>(source))
                continue;

            ent.Comp.LinkedServerId = source;
            return;
        }

        ent.Comp.LinkedServerId = null;
    }

    public void RefreshServerConsoles(EntityUid serverUid)
    {
        var query = EntityQueryEnumerator<QuantumConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (FindServer((uid, console)) != serverUid)
                continue;

            UpdateUi((uid, console));
        }
    }
}
