// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.Lobby;

/// <summary>
/// An extra lobby track added to the lobby playlist, ported from Monolith (LuaM).
/// Gives the track a readable title and artist, and can make it the first track of every round.
/// </summary>
[Prototype]
public sealed partial class LobbyTrackPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ResPath Path;

    [DataField]
    public string? Title;

    [DataField]
    public string? Artist;

    /// <summary>
    /// Plays this track first instead of a random one.
    /// </summary>
    [DataField]
    public bool Default;
}
