// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.Arcane.Client/JoinQueue/QueueMiniGameControl.cs

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._Arcane.JoinQueue;

public abstract partial class QueueMiniGameControl : Control, IQueueMiniGameScoreSource
{
    [Dependency] private IInputManager _input = default!;

    private bool _wasSpace;
    private bool _started;
    private bool _reportedFinalScore;

    public event Action<int>? ScoreChanged;
    public event Action? BulletFired;
    public event Action? EnemyKilled;
    public event Action<bool>? GameEnded;

    public abstract int Score { get; }
    public abstract int Lives { get; }
    public abstract int Wave { get; }

    protected bool Started => _started;

    protected abstract bool IsGameFinished { get; }

    protected abstract void ResetGame();

    protected abstract void UpdateRunningGame(FrameEventArgs args, bool space);

    protected abstract void DrawGame(DrawingHandleScreen handle);

    protected sealed override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var previous = handle.GetTransform();
        handle.SetTransform(Matrix3x2.CreateScale(UIScale) * previous);
        try
        {
            DrawGame(handle);
        }
        finally
        {
            handle.SetTransform(previous);
        }
    }

    protected virtual void UpdateIdleGame(FrameEventArgs args)
    {
    }

    protected virtual void UpdatePostGame(FrameEventArgs args)
    {
    }

    protected virtual void UpdateSharedGame(FrameEventArgs args)
    {
    }

    protected bool IsKeyDown(Keyboard.Key key)
        => HasGameKeyboardFocus() && _input.IsKeyDown(key);

    private bool HasGameKeyboardFocus()
    {
        var focused = UserInterfaceManager.KeyboardFocused;
        if (focused == null)
            return true;

        for (var control = focused; control != null; control = control.Parent)
        {
            if (control == this)
                return true;
        }

        return false;
    }

    protected void RaiseBulletFired()
        => BulletFired?.Invoke();

    protected void RaiseEnemyKilled()
        => EnemyKilled?.Invoke();

    protected void RaiseGameEnded(bool victory)
        => GameEnded?.Invoke(victory);

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var heldSpace = _input.IsKeyDown(Keyboard.Key.Space);
        if (!VisibleInTree)
        {
            _wasSpace = heldSpace;
            return;
        }

        var space = HasGameKeyboardFocus() && heldSpace;
        var pressedSpace = space && !_wasSpace;

        if (!_started)
        {
            UpdateIdleGame(args);
            if (pressedSpace)
                StartOrResetGame();
        }
        else if (IsGameFinished)
        {
            UpdatePostGame(args);
            if (pressedSpace)
                StartOrResetGame();
        }
        else
        {
            UpdateRunningGame(args, space);
        }

        UpdateSharedGame(args);
        ReportFinalScore();
        _wasSpace = heldSpace;
    }

    private void StartOrResetGame()
    {
        _started = true;
        _reportedFinalScore = false;
        ResetGame();
    }

    private void ReportFinalScore()
    {
        if (_reportedFinalScore || !IsGameFinished)
            return;

        _reportedFinalScore = true;
        ScoreChanged?.Invoke(Score);
    }
}
