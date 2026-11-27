// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/b52ef462f193de8563488db184be5393b9881342/Content.Arcane.Server/ServerProtection/EmoteProtectionSystem.cs

using Content.Server._Orion.ServerProtection;
using Content.Shared.Administration.Managers;
using Content.Shared.Speech;
using Content.Shared._Arcane.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server._Arcane.ServerProtection;

/// <summary>
///     Punishes players that send hundreds of emotes in a few seconds, which lags or crashes the server (Orion #175).
///     Vocal emote spam at much lower rates is handled by the Goob emote spam countermeasure.
/// </summary>
public sealed partial class EmoteProtectionSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminManager _admin = default!;
    [Dependency] private ServerProtectionAuditSystem _audit = default!;
    [Dependency] private ServerProtectionPunishmentSystem _punishment = default!;

    private bool _enabled;
    private bool _ban;
    private bool _kick;
    private bool _erase;
    private bool _deleteMessages;
    private int _banDuration;
    private int _hardThreshold;
    private int _softVariance;
    private float _postSoftProbability;
    private float _softRefreshCooldown;
    private float _clearInterval;

    private int? _softThreshold;
    private readonly Dictionary<EntityUid, int> _emotes = [];
    private float _sinceClear;
    private float _sinceRefresh;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, ArcaneCVars.EmoteProtectionEnabled, v => _enabled = v, true);
        Subs.CVar(_cfg, ArcaneCVars.EmoteProtectionBanEnabled, v => _ban = v, true);
        Subs.CVar(_cfg, ArcaneCVars.EmoteProtectionKickEnabled, v => _kick = v, true);
        Subs.CVar(_cfg, ArcaneCVars.EmoteProtectionEraseEnabled, v => _erase = v, true);
        Subs.CVar(_cfg, ArcaneCVars.EmoteProtectionDeleteMessages, v => _deleteMessages = v, true);
        Subs.CVar(_cfg, ArcaneCVars.EmoteProtectionBanDuration, v => _banDuration = v, true);
        Subs.CVar(_cfg, ArcaneCVars.EmoteProtectionHardThreshold, v => _hardThreshold = v, true);
        Subs.CVar(_cfg, ArcaneCVars.EmoteProtectionSoftVariance, v => _softVariance = v, true);
        Subs.CVar(_cfg, ArcaneCVars.EmoteProtectionPostSoftProbability, v => _postSoftProbability = v, true);
        Subs.CVar(_cfg, ArcaneCVars.EmoteProtectionSoftRefreshCooldown, v => _softRefreshCooldown = v, true);
        Subs.CVar(_cfg, ArcaneCVars.EmoteProtectionClearInterval, v => _clearInterval = v, true);

        _audit.AnnounceToggles(ArcaneCVars.EmoteProtectionEnabled, nameof(EmoteProtectionSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _sinceClear += frameTime;
        _sinceRefresh += frameTime;

        if (_sinceClear >= _clearInterval)
        {
            _emotes.Clear();
            _sinceClear = 0f;
        }

        if (_sinceRefresh < _softRefreshCooldown)
            return;

        GetSoftThreshold(true);
        _sinceRefresh = 0f;
    }

    [SubscribeLocalEvent]
    private void OnEmoteSent(ref EntityEmoteSentEvent args)
    {
        var uid = args.Source;
        if (!_enabled || !HasComp<SpeechComponent>(uid))
            return;

        // NPCs and admins are never punished
        if (!_player.TryGetSessionByEntity(uid, out _) || _admin.IsAdmin(uid, true))
            return;

        var count = _emotes.GetValueOrDefault(uid) + 1;
        _emotes[uid] = count;

        if (count >= _hardThreshold)
        {
            HandleViolation(uid, count);
            return;
        }

        var soft = GetSoftThreshold();
        if (count >= soft && _random.Prob(Math.Clamp((count - soft) * _postSoftProbability, 0f, 1f)))
            HandleViolation(uid, count);
    }

    private int GetSoftThreshold(bool refresh = false)
    {
        if (_softThreshold != null && !refresh)
            return _softThreshold.Value;

        _softThreshold = Math.Max(2, _hardThreshold * 3 / 4 - _random.Next(0, Math.Max(1, _softVariance)));
        return _softThreshold.Value;
    }

    private void HandleViolation(EntityUid uid, int count)
    {
        _emotes.Remove(uid);
        if (!_player.TryGetSessionByEntity(uid, out var session))
            return;

        Log.Info($"{session.Name} ({session.UserId}) exceeded the emote limit: {count} emotes.");

        if (_deleteMessages)
            _punishment.DeleteMessages(session);

        if (_erase)
            _punishment.EraseCharacter(session);

        if (_ban)
        {
            _punishment.SendAdminAlert(Loc.GetString("emote-protection-admin-announcement-ban", ("player", session.Name), ("count", count)));
            _punishment.ApplyBan(session.UserId, session.Name, Loc.GetString("emote-protection-ban-reason", ("count", count)), _banDuration);
        }
        else if (_kick)
        {
            _punishment.SendAdminAlert(Loc.GetString("emote-protection-admin-announcement-kick", ("player", session.Name), ("count", count)));
            _punishment.KickPlayer(session, Loc.GetString("emote-protection-kick-reason", ("count", count)));
        }
    }
}
