// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.InfinityDorms.Prototypes;

[Prototype]
public sealed partial class InfinityDormRoomPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<InfinityDormRoomCategoryPrototype> Category;

    [DataField(required: true)]
    public ResPath GridPath { get; private set; } = default!;
}
