// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/TetrisZz/frontier-Radiant/blob/743dfcac03/Content.Shared/_Starlight/Paper/MultistampComponent.cs

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Paper;

/// <summary>
/// Stamp that holds other stamps in its item slots and copies the selected one.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultistampComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> Stamps = [];

    [ViewVariables, AutoNetworkedField]
    public int CurrentEntry;

    [DataField]
    public SoundSpecifier? ChangeSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
}
