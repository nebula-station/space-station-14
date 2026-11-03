// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.ERP;

/// <summary>
/// Hurts partners of another species on erotic contact, e.g. the poisonous skin of arirals.
/// </summary>
[RegisterComponent]
public sealed partial class ErpContactDamageComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? Popups;
}
