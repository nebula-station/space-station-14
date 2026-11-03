// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.AntagTokens;

[Serializable, NetSerializable]
public enum AntagTokenError : byte
{
    None,
    Unavailable,
    NotLinked,
    NotInLobby,
    NotEnoughPlayers,
    InsufficientTokens,
    ActivePurchase,
    Busy,
}

[Serializable, NetSerializable]
public readonly record struct AntagTokenListingState(ProtoId<AntagTokenListingPrototype> Id, int MinPlayers);

[Serializable, NetSerializable]
public sealed class AntagTokenStateRequest : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class AntagTokenBuyRequest : EntityEventArgs
{
    public ProtoId<AntagTokenListingPrototype> Listing;
}

[Serializable, NetSerializable]
public sealed class AntagTokenCancelRequest : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class AntagTokenStateMessage : EntityEventArgs
{
    /// <summary>
    /// False when the bot cannot be reached or this server has no ID.
    /// </summary>
    public bool Available;

    public bool Linked;
    public int Balance;

    /// <summary>
    /// Tokens the current subscription gives per week, 0 without one.
    /// </summary>
    public int WeeklyTokens;

    /// <summary>
    /// Balance where weekly grants stop.
    /// </summary>
    public int WeeklyTokenCap;

    /// <summary>
    /// Time until the next weekly grant is allowed, null if there was none yet.
    /// </summary>
    public TimeSpan? NextWeeklyGrantIn;

    /// <summary>
    /// Purchases and cancellations are possible only in the lobby before the round.
    /// </summary>
    public bool InLobby;

    public int Online;
    public List<AntagTokenListingState> Listings = [];

    public ProtoId<AntagTokenListingPrototype>? ActiveListing;

    /// <summary>
    /// The active purchase belongs to another game server and cannot be cancelled here.
    /// </summary>
    public bool ActiveElsewhere;

    /// <summary>
    /// Result of the last request of this player.
    /// </summary>
    public AntagTokenError Error;
}

/// <summary>
/// Sent to everyone when the player count changes, so an open window can close listings without asking the bot.
/// </summary>
[Serializable, NetSerializable]
public sealed class AntagTokenOnlineMessage : EntityEventArgs
{
    public int Online;
}

/// <summary>
/// Sent to everyone when the round moves on, so an open window can ask for the balance and purchase again.
/// </summary>
[Serializable, NetSerializable]
public sealed class AntagTokenRunLevelMessage : EntityEventArgs
{
    public bool InLobby;
}

/// <summary>
/// Answer to a request rejected without asking the bot.
/// </summary>
[Serializable, NetSerializable]
public sealed class AntagTokenErrorMessage : EntityEventArgs
{
    public AntagTokenError Error;
}
