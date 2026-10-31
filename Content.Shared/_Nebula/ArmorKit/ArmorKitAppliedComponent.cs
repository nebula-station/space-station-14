// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.ArmorKit;

/// <summary>
/// Marks clothing that already had an <see cref="ArmorKitComponent"/> applied. Such clothing can't take another kit.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArmorKitAppliedComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId Prefix = "armor-kit-prefix-reinforced";

    [DataField, AutoNetworkedField]
    public Color PrefixColor = Color.FromHex("#DC143C");
}
