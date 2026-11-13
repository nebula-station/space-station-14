// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Humanoid.Markings;

/// <summary>
/// Tells the marking picker which markings are meant for a markings group, so it can show them first.
/// It restricts nothing: any group can still pick any marking.
/// </summary>
[Prototype]
public sealed partial class MarkingRecommendationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<MarkingsGroupPrototype> Group;

    /// <summary>
    /// Case-insensitive fragments of the RSI path of a marking's first sprite.
    /// </summary>
    [DataField]
    public List<string> Paths = [];

    /// <summary>
    /// Markings recommended by id. A marking listed in any recommendation ignores every <see cref="Paths"/>.
    /// </summary>
    [DataField]
    public List<ProtoId<MarkingPrototype>> Markings = [];
}
