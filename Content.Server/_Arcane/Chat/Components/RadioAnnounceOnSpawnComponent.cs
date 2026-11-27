// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/Chat/Components/RadioAnnounceOnSpawnComponent.cs

using Content.Server._Arcane.Chat.Systems;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.Chat.Components;

/// <summary>
/// Dispatches a radio announcement when the entity is mapinit'd.
/// </summary>
[RegisterComponent, Access(typeof(RadioAnnounceOnSpawnSystem))]
public sealed partial class RadioAnnounceOnSpawnComponent : Component
{
    /// <summary>
    ///     Radio channels to announce on.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<RadioChannelPrototype>[] AnnouncementChannels = default!;

    /// <summary>
    ///     Locale id of the announcement message.
    /// </summary>
    [DataField(required: true)]
    public LocId Message;

    /// <summary>
    ///     Locale id of the announcement's sender name.
    /// </summary>
    [DataField]
    public LocId Sender = "automatic-notification-system-sender";
}
