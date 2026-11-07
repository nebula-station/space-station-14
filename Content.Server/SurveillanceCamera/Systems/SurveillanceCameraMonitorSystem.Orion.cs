// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/SurveillanceCamera/Systems/SurveillanceCameraMonitorSystem.cs

using Content.Server._Orion.Bitrunning.Components;
using Content.Shared._Orion.Bitrunning.Components;

namespace Content.Server.SurveillanceCamera;

public sealed partial class SurveillanceCameraMonitorSystem
{
    /// <summary>
    /// Entity the monitor UI should show for the active camera: the avatar behind a netpod, or null if it is gone.
    /// </summary>
    private EntityUid? ResolveUiCamera(EntityUid? activeCamera)
    {
        // Nebula start
        if (activeCamera is not { } camera
            || !TryComp<NetpodComponent>(camera, out var pod)
            || !TryComp<AvatarNavRelayComponent>(camera, out var relay))
            return activeCamera;

        if (relay.RelayEntity is { } relayUid && Exists(relayUid))
            return relayUid;

        return pod.Avatar is { } avatar && Exists(avatar) ? avatar : null;
        // Nebula end
    }

    // Nebula start
    public void RefreshUi(EntityUid monitor)
    {
        UpdateUserInterface(monitor);
    }
    // Nebula end
}
