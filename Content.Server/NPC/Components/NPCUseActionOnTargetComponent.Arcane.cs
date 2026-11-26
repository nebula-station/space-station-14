// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Server/NPC/Components/NPCUseActionOnTargetComponent.cs

using Content.Shared.Actions.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Components;

public sealed partial class NPCUseActionOnTargetComponent
{
    /// <summary>
    /// Additional actions; each attempt picks one of all actions at random.
    /// </summary>
    [DataField]
    public List<EntProtoId<TargetActionComponent>> ExtraActionIds = [];

    [DataField]
    public List<EntityUid> ExtraActionEnts = [];
}
