// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/Chat/Systems/RadioAnnounceOnSpawnSystem.cs

using Content.Server._Arcane.Chat.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Radio.Components;
using Robust.Shared.Map;

namespace Content.Server._Arcane.Chat.Systems;

public sealed partial class RadioAnnounceOnSpawnSystem : EntitySystem
{
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    [SubscribeLocalEvent]
    private void OnInit(EntityUid uid, RadioAnnounceOnSpawnComponent comp, MapInitEvent args)
    {
        var entityName = MetaData(uid).EntityName;
        var message = Loc.GetString(comp.Message, ("name", entityName));

        var componentName = Loc.GetString(comp.Sender);
        var sender = Spawn(null, MapCoordinates.Nullspace);

        _metaData.SetEntityName(sender, componentName);

        AddComp<RadioMicrophoneComponent>(sender);

        // The temporary sender only provides the shown name; the spawned entity is the radio source, so telecom
        // and map checks see where the announcement actually comes from (a station channel needs that).
        foreach (var channelId in comp.AnnouncementChannels)
        {
            _radio.SendRadioMessage(sender, message, ProtoMan.Index(channelId), uid, escapeMarkup: false);
        }

        QueueDel(sender);
    }
}
