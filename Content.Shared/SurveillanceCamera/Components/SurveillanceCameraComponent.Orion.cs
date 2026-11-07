// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Shared/SurveillanceCamera/Components/SurveillanceCameraComponent.cs

namespace Content.Shared.SurveillanceCamera.Components;

public sealed partial class SurveillanceCameraComponent
{
    /// <summary>
    /// Entity each active viewer is subscribed to. Differs from the camera when it relays to a bitrunning avatar.
    /// </summary>
    [ViewVariables]
    public Dictionary<EntityUid, EntityUid> ViewerTargets = [];
}
