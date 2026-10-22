// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.AlertLevel;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.InfinityDorms.Components;

[RegisterComponent]
public sealed partial class InfinityDormTeleporterComponent : Component
{
    [DataField(readOnly: true)]
    public List<ProtoId<AlertLevelPrototype>> WhitelistedAlertLevel = [];

    [DataField(readOnly: true)]
    public float MaxInteractionDistance = 4f;

    [DataField(readOnly: true)]
    public int MaxValueForRoomId = 100000;

    [DataField(readOnly: true)]
    public int MinValueForRoomId = 1;

    [DataField(readOnly: true)]
    public float UsingRateLimit = 2f;

    [DataField(readOnly: true)]
    public Dictionary<int, EntityUid> ConnectedMaps = [];
}
