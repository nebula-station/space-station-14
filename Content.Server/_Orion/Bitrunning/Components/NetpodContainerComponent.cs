// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/_Orion/Bitrunning/Components/NetpodContainerComponent.cs

using Robust.Shared.Containers;

namespace Content.Server._Orion.Bitrunning.Components;

[RegisterComponent]
[Access(typeof(Systems.NetpodSystem))]
public sealed partial class NetpodContainerComponent : Component
{
    public ContainerSlot BodyContainer = default!;
}
