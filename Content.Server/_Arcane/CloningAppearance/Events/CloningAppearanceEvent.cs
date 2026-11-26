// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/CloningAppearance/Events/CloningAppearanceEvent.cs

using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._Arcane.CloningAppearance;

public sealed class CloningAppearanceEvent : EntityEventArgs
{
    public ICommonSession Player = default!;
    public CloningAppearanceComponent Component = default!;
    public EntityCoordinates Coords { get; set; }
    public EntityUid? StationUid { get; set; }

    /// <summary>
    /// The placeholder body the player attached to; deleted once the new body has the mind.
    /// </summary>
    public EntityUid Placeholder { get; set; }
}
