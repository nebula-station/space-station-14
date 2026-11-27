// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/b52ef462f193de8563488db184be5393b9881342/Content.Server/Administration/Managers/BanManager.cs

using Content.Server._Arcane.ServerProtection;

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager
{
    [Dependency] private IEntityManager _entityManager = default!;

    /// <summary>
    /// Lets the admin action protection count bans placed by admins (Orion #318).
    /// </summary>
    private void RaiseBanPlaced(CreateServerBanInfo banInfo, string adminName, string targetName)
    {
        if (banInfo.BanningAdmin is not { } banningAdmin)
            return;

        var ev = new AdminBanPlacedEvent(banningAdmin, adminName, targetName);
        _entityManager.EventBus.RaiseEvent(EventSource.Local, ref ev);
    }
}
