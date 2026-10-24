// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Nebula.GameTicking;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Preferences.Managers;
using Content.Shared._Nebula.CCVar;
using Content.Shared._Nebula.ReadyManifest;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Events;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.ReadyManifest;

/// <summary>
/// Shows lobby players which jobs the ready players want: every high priority job, and any wanted head job.
/// </summary>
public sealed partial class ReadyManifestSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private ServerGameTicker _gameTicker = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IServerPreferencesManager _preferences = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    /// <summary>
    /// Jobs with at least this display weight are listed at any priority, so heads show up even when not high.
    /// </summary>
    private const int HeadJobWeight = 10;

    private readonly Dictionary<ICommonSession, ReadyManifestEui> _openEuis = new();
    private ReadyManifestEuiState? _state;
    private JobUIComparer? _weights;

    public override void Initialize()
    {
        base.Initialize();
        _preferences.PreferencesChanged += OnPreferencesChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _preferences.PreferencesChanged -= OnPreferencesChanged;
    }

    [SubscribeNetworkEvent]
    private void OnRequest(RequestReadyManifestEvent ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby
            || !_cfg.GetCVar(NCCVars.LobbyReadyManifest)
            || _openEuis.ContainsKey(session))
        {
            return;
        }

        var eui = new ReadyManifestEui(this);
        _openEuis.Add(session, eui);
        _eui.OpenEui(eui, session);
        eui.StateDirty();
    }

    private void OnPreferencesChanged(NetUserId userId)
    {
        if (_gameTicker.PlayerGameStatuses.TryGetValue(userId, out var status)
            && status == PlayerGameStatus.ReadyToPlay)
        {
            SetState(null);
        }
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<JobPrototype>() || ev.WasModified<JobWeightPrototype>())
            _weights = null;
    }

    [SubscribeLocalEvent]
    private void OnLobbyInfoUpdated(LobbyInfoUpdatedEvent ev)
    {
        SetState(null);
    }

    private void SetState(ReadyManifestEuiState? state)
    {
        _state = state;

        foreach (var eui in _openEuis.Values)
        {
            eui.StateDirty();
        }
    }

    [SubscribeLocalEvent]
    private void OnRoundStarting(RoundStartingEvent ev)
    {
        foreach (var eui in new List<ReadyManifestEui>(_openEuis.Values))
        {
            eui.Close();
        }

        _openEuis.Clear();
        _state = null;
    }

    public void OnEuiClosed(ICommonSession session)
    {
        _openEuis.Remove(session);
    }

    /// <summary>
    /// State shared by every open manifest.
    /// </summary>
    public ReadyManifestEuiState GetState()
    {
        return _state ??= BuildState();
    }

    private ReadyManifestEuiState BuildState()
    {
        var entries = new List<ReadyManifestEntry>();
        var readyCount = 0;

        if (_weights == null)
            JobUIComparer.TryCreate(_prototypes, null, out _weights);

        foreach (var session in _player.Sessions)
        {
            if (!_gameTicker.PlayerGameStatuses.TryGetValue(session.UserId, out var status)
                || status != PlayerGameStatus.ReadyToPlay)
            {
                continue;
            }

            readyCount++;

            if (!_preferences.TryGetCachedPreferences(session.UserId, out var preferences)
                || !preferences.Characters.TryGetValue(preferences.SelectedCharacterIndex, out var profile))
            {
                continue;
            }

            foreach (var (jobId, priority) in profile.JobPriorities)
            {
                if (priority == JobPriority.Never || !_prototypes.TryIndex(jobId, out var job))
                    continue;

                if (priority != JobPriority.High && (_weights?.GetWeight(job) ?? 0) < HeadJobWeight)
                    continue;

                entries.Add(new ReadyManifestEntry(jobId, profile.Name, session.Name));
            }
        }

        return new ReadyManifestEuiState(entries, readyCount);
    }
}
