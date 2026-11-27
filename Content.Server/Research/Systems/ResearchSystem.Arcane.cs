// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Server/Research/Systems/ResearchSystem.Arcane.cs

using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

/// <summary>
/// Arcane additions: spoken console unlock messages, numbered server names and server examine.
/// </summary>
public sealed partial class ResearchSystem
{
    [Dependency] private ChatSystem _chat = default!;

    private void SayTechnologyUnlocked(EntityUid console, TechnologyPrototype technology, string cost, string approver)
    {
        var message = Loc.GetString("research-console-unlock-technology-ic",
            ("technology", Loc.GetString(technology.Name)),
            ("amount", cost),
            ("approver", approver));

        _chat.TrySendInGameICMessage(console, message, InGameICChatType.Speak, false);
    }
}
