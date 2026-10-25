// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/dead-space-server/EridaGoob/blob/312c4de3ed33142c2a488771f4d1f58bbc499974/Content.Shared/_Orion/ServerProtection/Chat/ChatProtectionListPrototype.cs

using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.ServerProtection.Chat;

/// <summary>
/// List of words that are blocked in chat by the chat protection system.
/// </summary>
[Prototype]
public sealed partial class ChatProtectionListPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<string> Words { get; private set; } = [];
}
