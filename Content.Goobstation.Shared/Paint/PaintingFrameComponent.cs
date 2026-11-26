// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Goobstation.Shared/Paint/PaintingFrameComponent.cs

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Paint;

[RegisterComponent, NetworkedComponent]
public sealed partial class PaintingFrameComponent : Component
{
    [DataField(required: true)]
    public string Material = string.Empty;
}
