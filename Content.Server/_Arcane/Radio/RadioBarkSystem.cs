// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/Radio/RadioBarkSystem.cs

using Content.Shared._Arcane.Radio;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server._Arcane.Radio;

/// <summary>
/// Sends the channel's bark to players whose headset received a radio message (Orion #452, #456).
/// </summary>
public sealed partial class RadioBarkSystem : EntitySystem
{
    private static readonly SoundPathSpecifier DefaultBark = new("/Audio/_Arcane/Radio/basic.ogg");

    [SubscribeLocalEvent]
    private void OnHeadsetReceive(Entity<ActorComponent> ent, ref HeadsetRadioReceiveRelayEvent args)
    {
        var sound = args.RelayedEvent.Channel.OnSendSound ?? DefaultBark;
        RaiseNetworkEvent(new RadioBarkEvent(sound.Path, sound.Params), ent.Comp.PlayerSession.Channel);
    }
}
