// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.Arcane.Client/JoinQueue/IQueueMiniGameScoreSource.cs

namespace Content.Client._Arcane.JoinQueue;

public interface IQueueMiniGameScoreSource
{
    event Action<int>? ScoreChanged;
    event Action? BulletFired;
    event Action? EnemyKilled;
    event Action<bool>? GameEnded; // true = victory

    int Score { get; }
    int Lives { get; }
    int Wave { get; }
}
