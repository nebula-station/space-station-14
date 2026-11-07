// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/SurveillanceCamera/Systems/SurveillanceCameraSystem.cs

using Content.Server._Orion.Bitrunning.Components;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SurveillanceCamera;

public sealed partial class SurveillanceCameraSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SurveillanceCameraMonitorSystem _monitor = default!; // Nebula edit

    public void ConfigureCameraNetwork(
        EntityUid uid,
        ProtoId<DeviceFrequencyPrototype> receiveFrequencyId,
        ProtoId<DeviceFrequencyPrototype>? transmitFrequencyId = null,
        SurveillanceCameraComponent? camera = null,
        DeviceNetworkComponent? deviceNet = null)
    {
        if (!Resolve(uid, ref camera, ref deviceNet))
            return;

        deviceNet.ReceiveFrequencyId = receiveFrequencyId;
        _deviceNetworkSystem.SetReceiveFrequency((uid, deviceNet), _proto.Index(receiveFrequencyId).Frequency);

        if (transmitFrequencyId is { } transmit)
        {
            deviceNet.TransmitFrequencyId = transmit;
            _deviceNetworkSystem.SetTransmitFrequency((uid, deviceNet), _proto.Index(transmit).Frequency);
        }

        if (!camera.AvailableNetworks.Contains(receiveFrequencyId))
            camera.AvailableNetworks.Add(receiveFrequencyId);

        camera.NetworkSet = true;
    }

    public void ClearActiveViewers(EntityUid camera, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
            return;

        RemoveActiveViewers(camera, new HashSet<EntityUid>(component.ActivePvsViewers), component: component);
    }

    /// <summary>
    /// Entity whose view a camera viewer should be subscribed to: the linked avatar for a netpod, the camera otherwise.
    /// </summary>
    public EntityUid ResolveSubscribeTarget(EntityUid camera)
    {
        if (!TryComp<NetpodComponent>(camera, out var pod)) // Nebula edit
            return camera; // Nebula edit

        if (TryComp<AvatarNavRelayComponent>(camera, out var relay)
            && relay.RelayEntity is { } relayUid
            && Exists(relayUid))
            return relayUid;

        if (pod.Avatar is { } avatar // Nebula edit
            && Exists(avatar))
            return avatar;

        return camera;
    }

    // Nebula start
    /// <summary>
    /// Moves the camera's viewers to its current subscribe target after the relay changed, and refreshes the monitors showing it.
    /// </summary>
    public void RefreshViewerTargets(EntityUid camera, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component, false))
            return;

        var target = ResolveSubscribeTarget(camera);
        List<EntityUid> players = [.. component.ViewerTargets.Keys];
        foreach (var player in players)
        {
            var oldTarget = component.ViewerTargets[player];
            if (oldTarget == target || !TryComp<ActorComponent>(player, out var actor))
                continue;

            _viewSubscriberSystem.RemoveViewSubscriber(oldTarget, actor.PlayerSession);
            _viewSubscriberSystem.AddViewSubscriber(target, actor.PlayerSession);
            component.ViewerTargets[player] = target;
        }

        foreach (var monitor in component.ActiveMonitors)
        {
            _monitor.RefreshUi(monitor);
        }
    }
    // Nebula end
}
