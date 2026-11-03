// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/93847a07a4b0b5890003c074e4768c5ba828543a/Content.Client/Audio/ContentAudioSystem.cs

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem
{
    /// <summary>
    /// Additional sound occlusion caused by the listener's hearing.
    /// </summary>
    public float ListenerOcclusion;
}
