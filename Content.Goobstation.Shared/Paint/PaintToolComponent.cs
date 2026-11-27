// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Goobstation.Shared/Paint/PaintToolComponent.cs

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Paint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaintToolComponent : Component
{
    [DataField, AutoNetworkedField]
    public PaintColorMode ColorMode = PaintColorMode.Fixed;

    [DataField, AutoNetworkedField]
    public Color Color = Color.Black;

    [DataField, AutoNetworkedField]
    public int MinBrushSize = 1;

    [DataField, AutoNetworkedField]
    public int MaxBrushSize = 3;

    [DataField, AutoNetworkedField]
    public bool CanErase;

    [DataField]
    public int PixelsPerCharge;

    [DataField]
    public string? Solution;

    [DataField]
    public int PixelsPerUnit = 128;

    [ViewVariables]
    public int PixelDebt;
}

[Serializable, NetSerializable]
public enum PaintColorMode : byte
{
    Fixed,
    Picker,
    Crayon,
    Solution,
}
