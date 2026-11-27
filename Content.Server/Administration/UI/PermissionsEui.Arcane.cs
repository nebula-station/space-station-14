// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/b52ef462f193de8563488db184be5393b9881342/Content.Server/Administration/UI/PermissionsEui.cs
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/b52ef462f193de8563488db184be5393b9881342/Content.Server/Administration/Managers/AdminManager.cs

using System.Linq;
using Content.Server._Arcane.ServerProtection;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Shared.Administration;
using DbAdminRank = Content.Server.Database.AdminRank;

namespace Content.Server.Administration.UI;

/// <summary>
/// Reports permission reductions and rank escalation attempts to the admin action protection (Orion #318).
/// </summary>
public sealed partial class PermissionsEui
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IEntityManager _entityManager = default!;

    /// <summary>
    /// One rank-wide change is one protection action, whatever the number of its admins.
    /// </summary>
    private void OnRankRemoved(DbAdminRank rank)
    {
        if (rank.Flags.Count > 0)
            RaisePermissionsReduced(rank.Name, "remove-rank");
    }

    private void OnRankUpdated(DbAdminRank oldRank, AdminFlags newFlags)
    {
        var oldFlags = AdminFlagsHelper.NamesToFlags(oldRank.Flags.Select(f => f.Flag));
        if ((oldFlags & ~newFlags) != 0)
            RaisePermissionsReduced(oldRank.Name, "update-rank-restrict");
    }

    private void OnAdminRemoved(string name)
    {
        RaisePermissionsReduced(name, "remove-admin");
    }

    private void OnAdminUpdated(string name, Admin oldAdmin, Admin newAdmin)
    {
        if (IsPermissionReduction(oldAdmin, newAdmin))
            RaisePermissionsReduced(name, "update-admin-restrict");
    }

    private void OnRankEscalation()
    {
        _chat.SendAdminAlert(Loc.GetString("admin-action-protection-rank-escalation", ("admin", Player.Name)));
    }

    private void RaisePermissionsReduced(string targetName, string action)
    {
        var ev = new AdminPermissionsReducedEvent(Player.UserId, Player.Name, targetName, action);
        _entityManager.EventBus.RaiseEvent(EventSource.Local, ref ev);
    }

    /// <summary>
    /// Compares the effective permissions before and after, rank included, the same way AdminManager computes them.
    /// </summary>
    private static bool IsPermissionReduction(Admin oldAdmin, Admin newAdmin)
    {
        if (newAdmin.Suspended && !oldAdmin.Suspended)
            return true;

        var oldFlags = GetEffectiveFlags(oldAdmin.AdminRank, oldAdmin.Flags);
        var newFlags = GetEffectiveFlags(newAdmin.AdminRank, newAdmin.Flags);
        return (oldFlags & ~newFlags) != 0;
    }

    /// <summary>
    /// The permissions an admin record grants: its rank's flags with the record's own flags added or removed.
    /// </summary>
    private static AdminFlags GetEffectiveFlags(DbAdminRank? rank, IEnumerable<AdminFlag> adminFlags)
    {
        var flags = rank == null ? AdminFlags.None : AdminFlagsHelper.NamesToFlags(rank.Flags.Select(p => p.Flag));

        foreach (var dbFlag in adminFlags)
        {
            var flag = AdminFlagsHelper.NameToFlag(dbFlag.Flag);
            if (dbFlag.Negative)
                flags &= ~flag;
            else
                flags |= flag;
        }

        return flags;
    }
}
