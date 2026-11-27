// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/b52ef462f193de8563488db184be5393b9881342/Content.Arcane.Server/ServerProtection/AdminActionProtectionSystem.cs

using Content.Server._Orion.ServerProtection;
using Content.Shared._Arcane.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.ServerProtection;

/// <summary>
///     Alerts about and stops admins that place many bans or strip many admins in a short time (Orion #318).
/// </summary>
public sealed partial class AdminActionProtectionSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ServerProtectionAuditSystem _audit = default!;
    [Dependency] private ServerProtectionPunishmentSystem _punishment = default!;

    private bool _enabled;
    private int _banThreshold;
    private float _banWindow;
    private int _permissionThreshold;
    private float _permissionWindow;
    private float _alertCooldown;
    private bool _autoDeAdmin;
    private bool _autoBan;
    private int _autoBanMinutes;

    private readonly Dictionary<(NetUserId Admin, ActionKind Kind), Queue<TimeSpan>> _actions = [];
    private readonly Dictionary<(NetUserId Admin, ActionKind Kind), TimeSpan> _lastAlert = [];

    private enum ActionKind : byte
    {
        Ban,
        Permission,
    }

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, ArcaneCVars.AdminActionProtectionEnabled, v => _enabled = v, true);
        Subs.CVar(_cfg, ArcaneCVars.AdminActionProtectionBanThreshold, v => _banThreshold = v, true);
        Subs.CVar(_cfg, ArcaneCVars.AdminActionProtectionBanWindowSeconds, v => _banWindow = v, true);
        Subs.CVar(_cfg, ArcaneCVars.AdminActionProtectionPermissionThreshold, v => _permissionThreshold = v, true);
        Subs.CVar(_cfg, ArcaneCVars.AdminActionProtectionPermissionWindowSeconds, v => _permissionWindow = v, true);
        Subs.CVar(_cfg, ArcaneCVars.AdminActionProtectionAlertCooldownSeconds, v => _alertCooldown = v, true);
        Subs.CVar(_cfg, ArcaneCVars.AdminActionProtectionAutoDeAdminEnabled, v => _autoDeAdmin = v, true);
        Subs.CVar(_cfg, ArcaneCVars.AdminActionProtectionAutoBanEnabled, v => _autoBan = v, true);
        Subs.CVar(_cfg, ArcaneCVars.AdminActionProtectionAutoBanMinutes, v => _autoBanMinutes = v, true);

        _audit.AnnounceToggles(ArcaneCVars.AdminActionProtectionEnabled, nameof(AdminActionProtectionSystem));
        _audit.AnnounceToggles(ArcaneCVars.AdminActionProtectionAutoDeAdminEnabled, "AdminActionProtection.AutoDeAdmin");
        _audit.AnnounceToggles(ArcaneCVars.AdminActionProtectionAutoBanEnabled, "AdminActionProtection.AutoBan");
    }

    [SubscribeLocalEvent]
    private void OnBanPlaced(ref AdminBanPlacedEvent args)
    {
        if (!_enabled || !RegisterAction(args.Admin, ActionKind.Ban, _banThreshold, _banWindow, out var alert))
            return;

        if (alert)
        {
            _punishment.SendAdminAlert(Loc.GetString("admin-action-protection-mass-ban",
                ("admin", args.AdminName),
                ("userId", args.Admin.ToString()),
                ("target", args.TargetName)));
        }

        EnforceMeasures(args.Admin, args.AdminName, "mass-ban-detected", alert);
    }

    [SubscribeLocalEvent]
    private void OnPermissionsReduced(ref AdminPermissionsReducedEvent args)
    {
        if (!_enabled || !RegisterAction(args.Admin, ActionKind.Permission, _permissionThreshold, _permissionWindow, out var alert))
            return;

        if (alert)
        {
            _punishment.SendAdminAlert(Loc.GetString("admin-action-protection-mass-permission-removal",
                ("admin", args.AdminName),
                ("userId", args.Admin.ToString()),
                ("action", args.Action),
                ("target", args.TargetName)));
        }

        EnforceMeasures(args.Admin, args.AdminName, "permission-stripping-detected", alert);
    }

    /// <summary>
    ///     Applies the protection every time the threshold is crossed; only the alerts are rate limited.
    /// </summary>
    private void EnforceMeasures(NetUserId admin, string adminName, string reasonTag, bool alert)
    {
        if (_autoDeAdmin)
        {
            _punishment.Suspend(admin, adminName, $"AdminActionProtection: {reasonTag}");
            if (alert)
            {
                _punishment.SendAdminAlert(Loc.GetString("admin-action-protection-deadmined",
                    ("admin", adminName),
                    ("userId", admin.ToString()),
                    ("reason", reasonTag)));
            }
        }

        if (!_autoBan)
            return;

        _punishment.ApplyBan(admin, adminName, Loc.GetString("admin-action-protection-ban-reason", ("reason", reasonTag)), _autoBanMinutes);
        if (alert)
        {
            _punishment.SendAdminAlert(Loc.GetString("admin-action-protection-banned",
                ("admin", adminName),
                ("userId", admin.ToString()),
                ("minutes", _autoBanMinutes),
                ("reason", reasonTag)));
        }
    }

    /// <summary>
    ///     Records the action and returns true when it crosses the threshold; <paramref name="alert"/> is false
    ///     while an alert for this admin and kind was sent recently.
    /// </summary>
    private bool RegisterAction(NetUserId admin, ActionKind kind, int threshold, float windowSeconds, out bool alert)
    {
        alert = false;
        var key = (admin, kind);
        if (!_actions.TryGetValue(key, out var queue))
        {
            queue = [];
            _actions[key] = queue;
        }

        var now = _timing.CurTime;
        var window = TimeSpan.FromSeconds(Math.Max(1f, windowSeconds));

        queue.Enqueue(now);
        while (queue.Count > 0 && now - queue.Peek() > window)
        {
            queue.Dequeue();
        }

        if (queue.Count < Math.Max(1, threshold))
            return false;

        if (_lastAlert.TryGetValue(key, out var lastAlert) && now - lastAlert < TimeSpan.FromSeconds(Math.Max(1f, _alertCooldown)))
            return true;

        _lastAlert[key] = now;
        alert = true;
        return true;
    }
}
