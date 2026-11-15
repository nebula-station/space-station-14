// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.ERP.Lactation;

[RegisterComponent, NetworkedComponent, Access(typeof(LactationSystem))]
public sealed partial class LactationComponent : Component
{
    [DataField]
    public EntProtoId Solution = "SolutionBreasts";

    [DataField]
    public List<string> BlockingSlots = new() { "jumpsuit", "outerClothing" };
}
