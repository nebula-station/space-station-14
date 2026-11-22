// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Bodycam;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBodycamSystem))]
public sealed partial class BodycamComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}

[ByRefEvent]
public readonly record struct BodycamToggledEvent(EntityUid? User, bool Enabled);
