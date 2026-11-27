// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Common/PDA/PdaPowerOffMessage.cs

using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.PDA;

/// <summary>
/// Closes the PDA interface and turns its screen off.
/// </summary>
[Serializable, NetSerializable]
public sealed class PdaPowerOffMessage : BoundUserInterfaceMessage;
