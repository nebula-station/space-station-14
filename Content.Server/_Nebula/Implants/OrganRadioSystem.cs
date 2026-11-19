// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared._Nebula.Implants;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;

namespace Content.Server._Nebula.Implants;

/// <summary>
/// Gives the body intrinsic radio from the keys of an enabled <see cref="OrganRadioComponent"/> organ.
/// Only channels and components the organ added are removed when it is disabled.
/// </summary>
public sealed partial class OrganRadioSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnEnabled(Entity<OrganRadioComponent> ent, ref OrganEnabledEvent args)
    {
        if (!TryComp<EncryptionKeyHolderComponent>(ent, out var holder))
            return;

        ClearChannels(ent, args.Body);

        var receiver = EnsureComp<ActiveRadioComponent>(args.Body);
        ent.Comp.AddedReceiver = !HasComp<IntrinsicRadioReceiverComponent>(args.Body);
        EnsureComp<IntrinsicRadioReceiverComponent>(args.Body);

        if (!TryComp<IntrinsicRadioTransmitterComponent>(args.Body, out var transmitter))
        {
            transmitter = AddComp<IntrinsicRadioTransmitterComponent>(args.Body);
            transmitter.Channels.Clear();
        }

        foreach (var channel in holder.Channels)
        {
            if (receiver.Channels.Add(channel))
                ent.Comp.AddedReceiverChannels.Add(channel);
            if (transmitter.Channels.Add(channel))
                ent.Comp.AddedTransmitterChannels.Add(channel);
        }

        EnsureComp<OrganRadioUserComponent>(args.Body).Organ = ent.Owner;
    }

    [SubscribeLocalEvent]
    private void OnDisabled(Entity<OrganRadioComponent> ent, ref OrganDisabledEvent args)
    {
        ClearChannels(ent, args.Body);

        if (TryComp<OrganRadioUserComponent>(args.Body, out var user) && user.Organ == ent.Owner)
            RemComp(args.Body, user);
    }

    [SubscribeLocalEvent]
    private void OnGetDefaultChannel(Entity<OrganRadioUserComponent> ent, ref GetDefaultRadioChannelEvent args)
    {
        if (TryComp<EncryptionKeyHolderComponent>(ent.Comp.Organ, out var holder))
            args.Channel ??= holder.DefaultChannel;
    }

    private void ClearChannels(Entity<OrganRadioComponent> ent, EntityUid body)
    {
        if (TryComp<ActiveRadioComponent>(body, out var receiver))
        {
            receiver.Channels.ExceptWith(ent.Comp.AddedReceiverChannels);
            if (receiver.Channels.Count == 0)
                RemComp(body, receiver);
        }

        if (TryComp<IntrinsicRadioTransmitterComponent>(body, out var transmitter))
        {
            transmitter.Channels.ExceptWith(ent.Comp.AddedTransmitterChannels);
            if (transmitter.Channels.Count == 0)
                RemComp(body, transmitter);
        }

        if (ent.Comp.AddedReceiver)
            RemComp<IntrinsicRadioReceiverComponent>(body);

        ent.Comp.AddedReceiverChannels.Clear();
        ent.Comp.AddedTransmitterChannels.Clear();
        ent.Comp.AddedReceiver = false;
    }
}
