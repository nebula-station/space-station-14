// SPDX-FileCopyrightText: 2026 Janet Blackquill <uhhadd@gmail.com>
//
// SPDX-License-Identifier: LicenseRef-Wallening

// Ported from https://github.com/Goob-Station/Goob-Station/blob/707e36693d1d9289a4f04921c5e070b1e4195589/Content.Shared/_ST/Interaction/InteractionParticleEvent.cs

using Robust.Shared.Serialization;

namespace Content.Shared._ST.Interaction;

/// <summary>
/// Data for interaction particles
/// </summary>
[Serializable, NetSerializable]
public sealed class StellarInteractionParticleEvent(NetEntity performer, NetEntity? used, NetEntity target, bool isClientEvent, StellarInteractionParticleType type) : EntityEventArgs
{
    public NetEntity Performer = performer;

    public NetEntity? Used = used;

    public NetEntity Target = target;

    /// <summary>
    /// Workaround for event subscription not working w/ the session overload
    /// </summary>
    public bool IsClientEvent = isClientEvent;

    public StellarInteractionParticleType Type = type;
}

[Serializable, NetSerializable]
public enum StellarInteractionParticleType
{
    Use,
    Pull,
    InHand,
}
