// SPDX-License-Identifier: MIT

// Ported from https://github.com/funky-station/forky-station/blob/500074aca67df6fc7fe86b9977a16e35f9ba533f/Content.Shared/_Starfall/Offering/OfferedItemComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared._Starfall.Offering;

/// <summary>
/// Marks a held item as being offered by its holder to a specific recipient.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OfferedItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Offerer;

    [DataField, AutoNetworkedField]
    public EntityUid Recipient;

    /// <summary>
    /// Index of the offerer's insert do-after; the offer ends together with it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ushort DoAfterIndex;
}
