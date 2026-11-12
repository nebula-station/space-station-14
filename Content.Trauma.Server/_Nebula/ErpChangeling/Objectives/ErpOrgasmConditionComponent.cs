// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server._Nebula.ErpChangeling.Objectives;

/// <summary>
/// Completed once the objective's target orgasms from an ERP panel interaction with the objective owner.
/// </summary>
[RegisterComponent, Access(typeof(ErpChangelingObjectivesSystem))]
public sealed partial class ErpOrgasmConditionComponent : Component
{
    [DataField]
    public bool Completed;
}
