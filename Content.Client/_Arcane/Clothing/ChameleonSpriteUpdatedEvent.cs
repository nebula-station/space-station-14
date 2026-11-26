// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Client/Clothing/Systems/ChameleonClothingSystem.cs

using Robust.Shared.Prototypes;

namespace Content.Client._Arcane.Clothing;

/// <summary>
/// Raised on a chameleon item after it copied the sprite of <see cref="Disguise"/>, which wipes runtime sprite layers.
/// </summary>
[ByRefEvent]
public readonly record struct ChameleonSpriteUpdatedEvent(EntityPrototype Disguise);
