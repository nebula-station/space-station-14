// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Shared/_Orion/DocumentPrinter/DocumentPrinterComponent.cs

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Orion.DocumentPrinter;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DocumentPrinterComponent : Component
{
    /// <summary>
    /// Who ordered the document being printed now, used to fill in their name and job.
    /// </summary>
    [ViewVariables]
    public EntityUid? CurrentActor;

    [DataField]
    public SoundSpecifier SwitchSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField, AutoNetworkedField]
    public bool IsOnAutocomplete = true;
}
