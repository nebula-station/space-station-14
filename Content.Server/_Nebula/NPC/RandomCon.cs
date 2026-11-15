// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.NPC.Queries.Considerations;

namespace Content.Server._Nebula.NPC;

/// <summary>
/// Scores every target randomly, to pick any valid target instead of the best one.
/// </summary>
public sealed partial class RandomCon : UtilityConsideration;
