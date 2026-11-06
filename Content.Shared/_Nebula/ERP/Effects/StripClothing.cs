// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.ERP.Effects;

/// <summary>
/// Drops the clothing worn in the given slots, except pressure protecting clothing.
/// </summary>
public sealed partial class StripClothing : EntityEffectBase<StripClothing>
{
    [DataField(required: true)]
    public List<string> Slots = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-strip-clothing", ("chance", Probability));
}
