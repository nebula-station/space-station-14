// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from https://github.com/Goob-Station/Goob-Station/blob/707e36693d1d9289a4f04921c5e070b1e4195589/Content.Shared/Humanoid/Prototypes/SpeciesPrototype.Goob.cs

using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Prototypes;

public sealed partial class SpeciesPrototype
{
    [DataField]
    public SpriteSpecifier? InteractionHandSprite { get; private set; }
}
