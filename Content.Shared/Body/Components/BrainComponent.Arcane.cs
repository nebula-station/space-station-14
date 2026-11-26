// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Shared/_Arcane/Body/Components/BrainComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

public sealed partial class BrainComponent
{
    /// <summary>
    /// Brains are named after their owner once removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Renamed;
}
