// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared._Nebula.Implants;
using Content.Shared.Body;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared._Nebula.Implants.Cyberpunk;

/// <summary>
/// Stops cyberware from going into a body that has something listed in <see cref="CyberwareIncompatibleComponent"/>.
/// </summary>
public sealed partial class CyberwareCompatibilitySystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    [SubscribeLocalEvent]
    private void OnOrganInsertAttempt(Entity<BodyComponent> ent, ref OrganInsertAttemptEvent args)
    {
        if (args.Cancelled
            || Prototype(args.Organ) is not { } proto
            || CanInstall(ent, proto.ID, out var reason, args.Organ))
            return;

        args.Cancelled = true;
        if (_net.IsServer)
            _popup.PopupEntity(reason, ent, PopupType.MediumCaution);
    }

    [SubscribeLocalEvent]
    private void OnInstallAttempt(Entity<BodyComponent> ent, ref CyberwareInstallAttemptEvent args)
    {
        if (args.Cancelled || CanInstall(ent, args.Cyberware, out var reason))
            return;

        args.Cancelled = true;
        args.Reason = reason;
    }

    [SubscribeLocalEvent]
    private void OnAddImplantAttempt(Entity<BodyComponent> ent, ref AddImplantAttemptEvent args)
    {
        if (args.Cancelled
            || Prototype(args.Implant) is not { } proto
            || CanInstall(ent, proto.ID, out var reason))
            return;

        args.Cancel();
        if (_net.IsServer)
            _popup.PopupEntity(reason, ent, args.User, PopupType.MediumCaution);
    }

    /// <summary>
    /// Checks whether cyberware of the given prototype can go into the body.
    /// </summary>
    /// <param name="ignore">Entity of that cyberware already being inserted, skipped among the installed ones.</param>
    public bool CanInstall(EntityUid body, EntProtoId cyberware, out string reason, EntityUid? ignore = null)
    {
        reason = string.Empty;
        var incoming = GetIncompatible(cyberware);
        if (incoming?.RequireMindShield == true && !HasComp<MindShieldComponent>(body))
        {
            reason = Loc.GetString("cyberware-incompatible-popup-mindshield");
            return false;
        }

        foreach (var installed in GetInstalled(body))
        {
            if (installed == ignore || Prototype(installed) is not { } installedProto)
                continue;

            var clash = incoming?.Incompatible.Contains(installedProto.ID) == true
                || CompOrNull<CyberwareIncompatibleComponent>(installed)?.Incompatible.Contains(cyberware) == true;
            if (!clash)
                continue;

            reason = Loc.GetString("cyberware-incompatible-popup", ("installed", installedProto.Name));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Organs and subdermal implants in the body.
    /// </summary>
    public IEnumerable<EntityUid> GetInstalled(EntityUid body)
    {
        foreach (var organ in _body.GetOrgans(body))
        {
            yield return organ.Owner;
        }

        if (!TryComp<ImplantedComponent>(body, out var implanted))
            yield break;

        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            yield return implant;
        }
    }

    private CyberwareIncompatibleComponent? GetIncompatible(EntProtoId cyberware)
    {
        return _proto.Index(cyberware).TryComp<CyberwareIncompatibleComponent>(out var comp, Factory) ? comp : null;
    }
}
