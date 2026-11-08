// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.ERP;

/// <summary>
/// Reads the consent tags a character set in their profile.
/// </summary>
public sealed partial class ErpConsentSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    public static readonly ProtoId<ErpConsentTagPrototype> HornyAntagVictim = "HornyAntagVictim";

    /// <summary>
    /// The consent the entity's character picked, or the tag's default if it has no profile.
    /// </summary>
    public ErpConsent GetConsent(EntityUid uid, ProtoId<ErpConsentTagPrototype> tag)
    {
        var proto = _prototypes.Index(tag);
        if (!TryComp<HumanoidProfileComponent>(uid, out var profile))
            return proto.Default;

        return profile.DetailExaminableData.GetConsent(proto);
    }
}
