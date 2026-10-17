// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Eui;
using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.DetailExaminable;

[Serializable, NetSerializable]
public sealed class DetailExaminableEuiState : EuiStateBase
{
    public NetEntity Target;
    public string Name = string.Empty;
    public string SpeciesName = string.Empty;
    public Sex Sex;
    public Gender Gender;
    public required DetailExaminableData Data;
}
