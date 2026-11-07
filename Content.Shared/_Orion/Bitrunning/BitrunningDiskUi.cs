// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Shared/_Orion/Bitrunning/BitrunningDiskUi.cs

using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

[Serializable, NetSerializable]
public enum BitrunningDiskUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class BitrunningDiskBoundUiState(List<string> options, string? selectedOption) : BoundUserInterfaceState
{
    public List<string> Options = options;
    public string? SelectedOption = selectedOption;
}

[Serializable, NetSerializable]
public sealed class BitrunningDiskSelectOptionMessage(string option) : BoundUserInterfaceMessage
{
    public string Option = option;
}
