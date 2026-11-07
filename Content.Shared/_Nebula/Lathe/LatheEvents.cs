// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Lathe;

/// <summary>
/// Raised on a lathe after a player queued items into <see cref="Batch"/>, the last batch of its queue.
/// </summary>
[ByRefEvent]
public readonly record struct LatheRecipeQueuedEvent(EntityUid Actor, LatheRecipeBatch Batch, int Quantity);

/// <summary>
/// Raised on a lathe when it starts producing the next item of <see cref="Batch"/>.
/// </summary>
[ByRefEvent]
public readonly record struct LatheBatchItemStartedEvent(LatheRecipeBatch Batch);

/// <summary>
/// Raised on a lathe when an aborted item went back to the queue as the new one-item <see cref="Batch"/>.
/// </summary>
[ByRefEvent]
public readonly record struct LatheItemRequeuedEvent(LatheRecipeBatch Batch);

/// <summary>
/// Raised on a lathe after it spawned the result of a recipe.
/// </summary>
[ByRefEvent]
public readonly record struct LatheProducedEntityEvent(EntityUid Result, ProtoId<LatheRecipePrototype> Recipe);
