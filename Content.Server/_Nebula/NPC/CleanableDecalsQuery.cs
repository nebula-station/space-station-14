// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.NPC.Queries.Queries;

namespace Content.Server._Nebula.NPC;

/// <summary>
/// Returns cleanable decals, such as footprints and dirt, in vision range on the NPC's grid.
/// </summary>
public sealed partial class CleanableDecalsQuery : UtilityQuery;
