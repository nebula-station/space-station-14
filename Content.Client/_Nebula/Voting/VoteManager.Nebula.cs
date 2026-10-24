// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client.Voting;

public sealed partial class VoteManager
{
    /// <summary>
    /// Votes currently running. The instances are updated in place as vote data arrives.
    /// </summary>
    public IReadOnlyCollection<ActiveVote> ActiveVotes => _votes.Values;

    /// <summary>
    /// Raised when a vote starts or ends, after its data is filled in.
    /// </summary>
    public event Action? ActiveVotesChanged;
}
