// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Shared._Nebula.Fax;

[RegisterComponent]
public sealed partial class FaxPagerComponent : Component
{
    [DataField]
    public int MaxFaxes = 2;

    [DataField]
    public bool AutoLink;

    [ViewVariables]
    public List<EntityUid> Faxes = new();

    [DataField]
    public SoundSpecifier BuzzSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");
}

[ByRefEvent]
public readonly record struct FaxReceivedEvent(string FromName);
