// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ERP;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ERPManagerComponent : Component
{
    [DataField, AutoNetworkedField]
    public ERPStatus Status = ERPStatus.Ask;
}

[Serializable, NetSerializable]
public enum ERPStatus : byte
{
    Full,
    Ask,
    Never
}
