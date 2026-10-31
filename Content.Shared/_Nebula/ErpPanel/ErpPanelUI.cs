// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ErpPanel;

[Serializable, NetSerializable]
public enum ErpPanelKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ErpPanelBuiState(NetEntity user, NetEntity target) : BoundUserInterfaceState
{
    public NetEntity User { get; } = user;
    public NetEntity Target { get; } = target;
}

[Serializable, NetSerializable]
public sealed class ErpPanelSendMessage(string interaction, bool privateMode) : BoundUserInterfaceMessage
{
    public readonly string Interaction = interaction;
    public readonly bool PrivateMode = privateMode;
}

[Serializable, NetSerializable]
public sealed class ErpPanelCustomEmoteMessage(ErpCustomEmote emote, bool privateMode) : BoundUserInterfaceMessage
{
    public readonly ErpCustomEmote Emote = emote;
    public readonly bool PrivateMode = privateMode;
}

[ByRefEvent]
public readonly record struct ErpPanelOpenEvent(EntityUid Target);
