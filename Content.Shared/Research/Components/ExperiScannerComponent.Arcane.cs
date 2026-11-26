// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Shared/Research/Components/ExperiScannerComponent.Arcane.cs

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Research.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExperiScannerComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ScanRange = 2f;

    [DataField]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    [DataField]
    public SoundSpecifier FailureSound = new SoundPathSpecifier("/Audio/Machines/scanbuzz.ogg");

    [DataField]
    public string LastResult = string.Empty;
}
