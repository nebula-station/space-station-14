// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Radio.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Radio;

/// <summary>
/// Sends radio messages that also play a sound to the wearers of headsets that received them.
/// </summary>
public sealed partial class RadioSoundSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private InventorySystem _inventory = default!;

    private const string EarsSlot = "ears";

    /// <summary>
    /// Radio sending the message right now, so nested radio messages sent meanwhile aren't counted.
    /// </summary>
    private EntityUid? _source;

    /// <summary>
    /// Wearers of headsets that received the message being sent right now, null outside of sending.
    /// </summary>
    private Filter? _listeners;

    /// <summary>
    /// Sends the message like <see cref="RadioSystem.SendRadioMessage"/> and plays the sound to everyone who got it, except <paramref name="exclude"/>.
    /// </summary>
    /// <returns>Wearers of headsets that received the message.</returns>
    public Filter SendRadioMessage(EntityUid speaker,
        string message,
        ProtoId<RadioChannelPrototype> channel,
        EntityUid radioSource,
        SoundSpecifier sound,
        EntityUid? exclude = null)
    {
        var (prevSource, prevListeners) = (_source, _listeners);
        var listeners = Filter.Empty();
        (_source, _listeners) = (radioSource, listeners);

        _radio.SendRadioMessage(speaker, message, _proto.Index(channel), radioSource);

        (_source, _listeners) = (prevSource, prevListeners);
        if (exclude is { } excluded)
            listeners.RemovePlayerByAttachedEntity(excluded);

        _audio.PlayGlobal(sound, listeners, true);
        return listeners;
    }

    /// <summary>
    /// Whether the mob wears a working headset that receives <paramref name="channel"/>.
    /// </summary>
    public bool WearsHeadset(EntityUid mob, ProtoId<RadioChannelPrototype> channel)
    {
        return _inventory.TryGetSlotEntity(mob, EarsSlot, out var ears)
            && IsWornHeadset(ears.Value, channel);
    }

    /// <summary>
    /// Whether the headset is worn, enabled and receives <paramref name="channel"/>.
    /// </summary>
    public bool IsWornHeadset(EntityUid headset, ProtoId<RadioChannelPrototype> channel)
    {
        return TryComp<HeadsetComponent>(headset, out var comp)
            && comp is { Enabled: true, IsEquipped: true }
            && TryComp<EncryptionKeyHolderComponent>(headset, out var keys)
            && keys.Channels.Contains(channel);
    }

    [SubscribeLocalEvent]
    private void OnHeadsetReceive(Entity<ActorComponent> ent, ref HeadsetRadioReceiveRelayEvent args)
    {
        if (_listeners == null
            || args.RelayedEvent.RadioSource != _source)
            return;

        _listeners.AddPlayer(ent.Comp.PlayerSession);
    }
}
