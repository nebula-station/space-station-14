// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server._Nebula.ErpChangeling.Objectives;

/// <summary>
/// Counts the different people hit with erotic stings, the required amount comes from NumberObjective.
/// </summary>
[RegisterComponent]
public sealed partial class ErpArouseCrewConditionComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Targets = new();
}
