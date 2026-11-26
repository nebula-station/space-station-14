// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Shared/Power/MegaCellComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared._Arcane.Power;

/// <summary>
/// A large cell for machines like SMES. Only fits power cell slots marked with <see cref="MegaCellSlotComponent"/> (Orion #192, #323).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MegaCellComponent : Component;

/// <summary>
/// Lets this entity's power cell slot accept mega cells.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MegaCellSlotComponent : Component;
