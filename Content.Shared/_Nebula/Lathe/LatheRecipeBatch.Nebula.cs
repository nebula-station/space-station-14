// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Lathe;

public sealed partial class LatheRecipeBatch
{
    /// <summary>
    /// Who ordered each item of the batch, by item index. Items queued without a player hold <see cref="EntityUid.Invalid"/>.
    /// Server only, never sent to clients.
    /// </summary>
    [NonSerialized]
    public List<EntityUid>? Actors;
}
