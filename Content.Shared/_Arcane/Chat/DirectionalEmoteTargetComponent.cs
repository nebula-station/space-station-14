// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/862852d48934ecca32ab2d9b44eeb3ecf73874a0/Content.Arcane.Shared/Chat/DirectionalEmoteTargetComponent.cs

using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.Chat;

/// <summary>
/// Can send and receive directional emotes.
/// </summary>
[RegisterComponent]
public sealed partial class DirectionalEmoteTargetComponent : Component
{
    [ViewVariables]
    public TimeSpan LastSend = TimeSpan.Zero;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1);
}

[Serializable, NetSerializable]
public sealed class SendDirectionalEmoteEvent(NetEntity target, string text) : EntityEventArgs
{
    public readonly NetEntity Target = target;
    public readonly string Text = text;
}
