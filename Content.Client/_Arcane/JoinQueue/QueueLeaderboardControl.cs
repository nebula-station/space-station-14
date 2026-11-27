// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.Arcane.Client/JoinQueue/QueueLeaderboardControl.cs

using System.Linq;
using System.Numerics;
using Content.Shared._Arcane.JoinQueue;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Arcane.JoinQueue;

public sealed class QueueMiniGameLeaderboardControl : BoxContainer
{
    private const float ColumnWidth = 268f;
    private const int MaxRows = 5;

    private static readonly QueueMiniGameKind[] GameOrder =
    [
        QueueMiniGameKind.Gyruss,
        QueueMiniGameKind.GoGoShitcurity,
        QueueMiniGameKind.SpaceInvaders,
    ];

    private readonly Dictionary<QueueMiniGameKind, (Label Name, Label Score)[]> _gameRows = [];
    private readonly Dictionary<QueueMiniGameKind, List<QueueMiniGameLeaderboardEntry>> _byGame = [];

    public QueueMiniGameLeaderboardControl()
    {
        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;
        MinSize = new Vector2(ColumnWidth, 0f);

        AddChild(new Label
        {
            Text = Loc.GetString("queue-leaderboard-games-title"),
            StyleClasses = { "LabelSecondaryColor" },
            Align = Label.AlignMode.Center,
        });

        foreach (var game in GameOrder)
        {
            AddChild(new Label
            {
                Text = Loc.GetString(GetGameLocId(game)),
                StyleClasses = { "LabelKeyText" },
                Margin = new Thickness(0, 6, 0, 0),
            });

            var rows = new (Label Name, Label Score)[MaxRows];
            for (var i = 0; i < MaxRows; i++)
            {
                var row = QueueLeaderboardRows.MakeRow(
                    Loc.GetString("queue-leaderboard-empty"),
                    string.Empty,
                    out var scoreLabel,
                    out var nameLabel,
                    56f);
                rows[i] = (nameLabel, scoreLabel);
                AddChild(row);
            }
            _gameRows[game] = rows;
            _byGame[game] = [];
        }
    }

    public void UpdateGameLeaderboard(IReadOnlyList<QueueMiniGameLeaderboardEntry> leaderboard)
    {
        foreach (var list in _byGame.Values)
            list.Clear();

        foreach (var entry in leaderboard)
        {
            if (_byGame.TryGetValue(entry.Game, out var list))
                list.Add(entry);
        }

        foreach (var game in GameOrder)
        {
            if (!_gameRows.TryGetValue(game, out var rows))
                continue;

            var entries = _byGame[game];
            for (var i = 0; i < rows.Length; i++)
            {
                var (nameLabel, scoreLabel) = rows[i];
                if (i < entries.Count && entries[i].Score > 0 && !string.IsNullOrEmpty(entries[i].PlayerName))
                {
                    nameLabel.Text = $"{i + 1}. {entries[i].PlayerName}";
                    scoreLabel.Text = entries[i].Score.ToString();
                }
                else
                {
                    nameLabel.Text = Loc.GetString("queue-leaderboard-empty");
                    scoreLabel.Text = string.Empty;
                }
            }
        }
    }

    private static string GetGameLocId(QueueMiniGameKind game)
    {
        return game switch
        {
            QueueMiniGameKind.Gyruss => "queue-minigame-gyruss",
            QueueMiniGameKind.GoGoShitcurity => "queue-minigame-gogo-shitcurity",
            QueueMiniGameKind.SpaceInvaders => "queue-minigame-space-invaders",
            _ => throw new ArgumentOutOfRangeException(nameof(game), game, null),
        };
    }
}

public sealed class QueueTimeLeaderboardControl : BoxContainer
{
    private const int MaxQueueRows = 5;
    private const float ColumnWidth = 320f;
    private const float TimeColumnWidth = 92f;

    private readonly List<QueueWaitEntry> _queueWaitTimes = [];
    private readonly List<(Label Name, Label Time)> _queueRows = [];

    private int _yourIndex = -1;

    public QueueTimeLeaderboardControl()
    {
        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;
        MinSize = new Vector2(ColumnWidth, 0f);

        AddChild(new Label
        {
            Text = Loc.GetString("queue-leaderboard-time-title"),
            StyleClasses = { "LabelSecondaryColor" },
            Align = Label.AlignMode.Center,
        });

        for (var i = 0; i < MaxQueueRows; i++)
        {
            var row = QueueLeaderboardRows.MakeRow(Loc.GetString("queue-leaderboard-empty"),
                string.Empty,
                out var timeLabel,
                out var nameLabel,
                TimeColumnWidth);

            _queueRows.Add((nameLabel, timeLabel));
            AddChild(row);
        }
    }

    public void UpdateQueuePlayers(IReadOnlyList<string> playerNames, IReadOnlyList<float> playerWaitSeconds, int yourIndex)
    {
        _yourIndex = yourIndex;
        _queueWaitTimes.Clear();

        for (var i = 0; i < playerNames.Count; i++)
        {
            var name = playerNames[i];
            var waitSeconds = i < playerWaitSeconds.Count ? playerWaitSeconds[i] : 0f;
            if (!float.IsFinite(waitSeconds) || waitSeconds < 0f || (double) waitSeconds > int.MaxValue)
                continue;

            _queueWaitTimes.Add(new QueueWaitEntry(name, waitSeconds, i));
        }

        RefreshQueueRows();
    }

    private void RefreshQueueRows()
    {
        var rankedPlayers = _queueWaitTimes
            .OrderByDescending(entry => entry.WaitSeconds)
            .ThenBy(entry => entry.QueueIndex)
            .Take(MaxQueueRows)
            .ToArray();

        for (var i = 0; i < _queueRows.Count; i++)
        {
            var (nameLabel, timeLabel) = _queueRows[i];
            if (i >= rankedPlayers.Length)
            {
                nameLabel.Text = Loc.GetString("queue-leaderboard-empty");
                timeLabel.Text = string.Empty;
                continue;
            }

            var entry = rankedPlayers[i];
            var displayName = entry.QueueIndex == _yourIndex
                ? Loc.GetString("queue-leaderboard-you", ("name", entry.Name))
                : entry.Name;

            nameLabel.Text = $"{i + 1}. {displayName}";
            timeLabel.Text = FormatQueueTime(entry.WaitSeconds);
        }
    }

    private static string FormatQueueTime(float seconds)
    {
        var totalSeconds = Math.Max(0, (int) seconds);
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds / 60;
        var remainingSeconds = totalSeconds % 60;

        if (hours > 0)
        {
            return Loc.GetString("queue-leaderboard-time-hours",
                ("hours", hours),
                ("minutes", minutes % 60));
        }

        return minutes == 0
            ? Loc.GetString("queue-leaderboard-time-seconds", ("seconds", remainingSeconds))
            : Loc.GetString("queue-leaderboard-time-minutes",
                ("minutes", minutes),
                ("seconds", remainingSeconds));
    }

    private readonly record struct QueueWaitEntry(string Name, float WaitSeconds, int QueueIndex);
}

internal static class QueueLeaderboardRows
{
    public static BoxContainer MakeRow(string name, string value, out Label valueLabel, out Label nameLabel, float valueWidth = 56f)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            MinHeight = 18,
        };

        nameLabel = new Label
        {
            Text = name,
            ClipText = true,
            HorizontalExpand = true,
            StyleClasses = { "LabelKeyText" },
        };

        valueLabel = new Label
        {
            Text = value,
            Align = Label.AlignMode.Right,
            ClipText = true,
            SetWidth = valueWidth,
            StyleClasses = { "LabelSecondaryColor" },
        };

        row.AddChild(nameLabel);
        row.AddChild(valueLabel);
        return row;
    }
}
