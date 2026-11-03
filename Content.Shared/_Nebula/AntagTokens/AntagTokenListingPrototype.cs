// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.AntagTokens;

/// <summary>
/// An antagonist a player can buy with an antag token.
/// The player count requirement comes from the GameRule component of <see cref="Rule"/>.
/// </summary>
[Prototype]
public sealed partial class AntagTokenListingPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Gives the shown name and description.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AntagPrototype> Antag;

    /// <summary>
    /// The game rule to create if none with <see cref="RuleComp"/> is running.
    /// It must not assign anyone else, so no lateJoinAdditional.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Rule;

    /// <summary>
    /// Name of the game rule's component, used to find a rule that is already running.
    /// </summary>
    [DataField(required: true)]
    public CompName RuleComp;
}
