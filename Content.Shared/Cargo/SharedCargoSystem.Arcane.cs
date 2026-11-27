// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Shared/Cargo/SharedCargoSystem.Arcane.cs

using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Storage.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo;

/// <summary>
/// Arcane - requester names and secured delivery checks (Orion #377).
/// </summary>
public abstract partial class SharedCargoSystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;

    /// <summary>
    /// Items always fit a secure crate; crates only when the account's department could open them anyway.
    /// </summary>
    public bool CanBeSecuredDelivery(Entity<CargoOrderConsoleComponent> console, EntityPrototype product)
    {
        if (!ProtoMan.Resolve(console.Comp.Account, out var account) || account.SecureCratePrototype == null)
            return false;

        var factory = EntityManager.ComponentFactory;
        if (product.TryComp<EntityStorageComponent>(out _, factory))
        {
            return !product.TryGetComponent<AccessReaderComponent>(out var reader, factory)
                || _accessReader.AreAccessTagsAllowed(account.SecureCrateOrderAccess, reader);
        }

        return product.TryGetComponent<ItemComponent>(out _, factory);
    }

    /// <summary>
    /// "Name, job" from the requester's ID, or their visible name without one.
    /// </summary>
    public string GenerateRequesterName(EntityUid requester)
    {
        if (_idCard.TryFindIdCard(requester, out var id))
        {
            return Loc.GetString("cargo-console-menu-order-requester-format",
                ("name", id.Comp.FullName ?? Identity.Name(requester, EntityManager)),
                ("job", id.Comp.LocalizedJobTitle ?? string.Empty));
        }

        return Identity.Name(requester, EntityManager);
    }
}
