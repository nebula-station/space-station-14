// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel.Requirements;

/// <summary>
/// A condition on the user or the target of an interaction. Checked by the client to list
/// the interaction and by the server before running it.
/// </summary>
[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class ErpRequirement
{
    public abstract bool IsAvailable(EntityUid uid, IEntityManager entityManager);
}

[Serializable, NetSerializable]
public abstract partial class InvertableErpRequirement : ErpRequirement
{
    [DataField]
    public bool Inverted;
}
