// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Goobstation.Server/Paint/PhotocopierComponent.cs

using Content.Goobstation.Shared.Paint;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Server.Paint;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PhotocopierComponent : Component
{
    [DataField]
    public string FirstSlot = "canvas_a";

    [DataField]
    public string SecondSlot = "canvas_b";

    [DataField]
    public TimeSpan ScanTime = TimeSpan.FromSeconds(2.4);

    [DataField]
    public TimeSpan PrintTime = TimeSpan.FromSeconds(2.4);

    [DataField]
    public SoundSpecifier? ScanSound = new SoundPathSpecifier("/Audio/Machines/scanning.ogg");

    [DataField]
    public SoundSpecifier? PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    [ViewVariables]
    public PhotocopierState State = PhotocopierState.Idle;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextStage;

    [ViewVariables]
    public EntityUid? Original;

    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables]
    public EntityUid? User;
}
