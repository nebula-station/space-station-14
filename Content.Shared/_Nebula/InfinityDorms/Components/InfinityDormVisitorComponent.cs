// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.InfinityDorms.Components;

[RegisterComponent]
public sealed partial class InfinityDormVisitorComponent : Component
{
    [DataField(readOnly: true)]
    public ProtoId<TagPrototype> NotInfinityDormItemTag = "NotInfinityDormItemTag";

    public EntityCoordinates ReturnCoordinates;
}
