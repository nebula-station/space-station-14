// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/b52ef462f193de8563488db184be5393b9881342/Content.Arcane.Server/ServerProtection/ServerProtectionAuditSystem.cs

using Content.Server._Orion.ServerProtection;
using Robust.Shared.Configuration;

namespace Content.Server._Arcane.ServerProtection;

/// <summary>
///     Tells admins when a protection system is switched on or off. Protection cvars have no CVarControl, so
///     changecvar cannot touch them: they change from the server console or the config, and are reported as such.
/// </summary>
public sealed partial class ServerProtectionAuditSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ServerProtectionPunishmentSystem _punishment = default!;

    /// <summary>
    ///     Tells admins whenever the bool cvar is toggled.
    /// </summary>
    public void AnnounceToggles(CVarDef<bool> cvar, string systemName)
    {
        Subs.CVar(_cfg, cvar, enabled => AnnounceToggle(systemName, enabled));
    }

    private void AnnounceToggle(string systemName, bool enabled)
    {
        _punishment.SendAdminAlert(Loc.GetString(enabled ? "server-protection-toggled-on" : "server-protection-toggled-off",
            ("system", systemName),
            ("actor", Loc.GetString("server-protection-server-actor"))));
    }
}
