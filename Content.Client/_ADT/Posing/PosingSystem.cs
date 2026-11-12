// SPDX-License-Identifier: AGPL-3.0-only AND GPL-3.0-only
// Ported from https://github.com/AdventureTimeSS14/space_station_ADT/blob/296c8b93290412fb8b446c0f42ce107851eefedf/Content.Client/ADT/Posing/PosingSystem.cs
// Ported from https://github.com/wega-team/ss14-wega/blob/fb7f83ef99fb7bb977976d0649a20dc8c4bba058/Content.Client/_Wega/Posing/PosingSystem.cs

using System.Numerics;
using Content.Shared._ADT.Posing;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._ADT.Posing;

public sealed partial class PosingSystem : SharedPosingSystem
{
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const string PosingContext = "posing";
    private const float LerpDuration = 0.15f;
    private const float ReturnDuration = 0.2f;

    private readonly Dictionary<EntityUid, (
        Vector2 Offset,
        Angle Angle,
        Vector2 TargetOffset,
        Angle TargetAngle,
        float LerpTime,
        bool IsReturning
    )> _interpolationState = new();

    public override void Initialize()
    {
        base.Initialize();

        var posing = _input.Contexts.New(PosingContext, "common");
        posing.AddFunction(ContentKeyFunctions.TogglePosing);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetUp);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetDown);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetLeft);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetRight);
        posing.AddFunction(ContentKeyFunctions.PosingRotatePositive);
        posing.AddFunction(ContentKeyFunctions.PosingRotateNegative);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _input.Contexts.Remove(PosingContext);
        _interpolationState.Clear();
    }

    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<PosingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_playerManager.LocalEntity == ent.Owner)
            return;

        var posing = ent.Comp;
        if (posing.Posing && _interpolationState.TryGetValue(ent, out var state))
        {
            state.TargetOffset = posing.CurrentOffset;
            state.TargetAngle = posing.CurrentAngle;
            state.LerpTime = 0f;
            state.IsReturning = false;
            _interpolationState[ent] = state;
        }
        else if (posing.Posing)
        {
            _interpolationState[ent] = (posing.CurrentOffset, posing.CurrentAngle, posing.CurrentOffset, posing.CurrentAngle, 0f, false);
        }
        else if (_interpolationState.TryGetValue(ent, out state) && !state.IsReturning)
        {
            state.IsReturning = true;
            state.LerpTime = 0f;
            state.TargetOffset = Vector2.Zero;
            state.TargetAngle = Angle.Zero;
            _interpolationState[ent] = state;
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<PosingComponent> ent, ref ComponentShutdown args)
    {
        if (!_interpolationState.Remove(ent))
            return;

        if (!TerminatingOrDeleted(ent))
            ResetSprite(ent);

        if (_playerManager.LocalEntity == ent.Owner)
            _input.Contexts.SetActiveContext(ent.Comp.DefaultInputContext);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<PosingComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        if (ent.Comp.Posing && _input.Contexts.ActiveContext.Name == PosingContext)
            _input.Contexts.SetActiveContext(ent.Comp.DefaultInputContext);
    }

    protected override void ClientTogglePosing(EntityUid uid, PosingComponent posing)
    {
        base.ClientTogglePosing(uid, posing);

        if (posing.Posing)
        {
            _interpolationState[uid] = (posing.CurrentOffset, posing.CurrentAngle, posing.CurrentOffset, posing.CurrentAngle, 0f, false);
            ApplySprite((uid, posing), posing.CurrentOffset, posing.CurrentAngle);
        }
        else if (_interpolationState.TryGetValue(uid, out var state))
        {
            state.IsReturning = true;
            state.LerpTime = 0f;
            state.TargetOffset = Vector2.Zero;
            state.TargetAngle = Angle.Zero;
            _interpolationState[uid] = state;
        }

        if (_playerManager.LocalEntity == uid)
            _input.Contexts.SetActiveContext(posing.Posing ? PosingContext : posing.DefaultInputContext);
    }

    private void ApplySprite(Entity<PosingComponent> ent, Vector2 offset, Angle angle)
    {
        _sprite.SetOffset(ent.Owner, ent.Comp.DefaultOffset + offset);
        _sprite.SetRotation(ent.Owner, Angle.FromDegrees(ent.Comp.DefaultAngle) + angle);
    }

    private void ResetSprite(Entity<PosingComponent> ent)
    {
        ApplySprite(ent, Vector2.Zero, Angle.Zero);
    }

    // Other animations overwrite the sprite offset and rotation, so the pose is reapplied every frame.
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<PosingComponent>();
        while (query.MoveNext(out var uid, out var posing))
        {
            if (posing.Posing)
                UpdatePosing((uid, posing), frameTime);
            else
                UpdateReturning((uid, posing), frameTime);
        }
    }

    private void UpdatePosing(Entity<PosingComponent> ent, float frameTime)
    {
        var posing = ent.Comp;
        if (!_interpolationState.TryGetValue(ent, out var state) || state.IsReturning)
            state = (posing.CurrentOffset, posing.CurrentAngle, posing.CurrentOffset, posing.CurrentAngle, 0f, false);

        if (state.TargetOffset != posing.CurrentOffset || state.TargetAngle != posing.CurrentAngle)
        {
            state.TargetOffset = posing.CurrentOffset;
            state.TargetAngle = posing.CurrentAngle;
            state.LerpTime = 0f;
        }

        state.LerpTime += frameTime;
        var t = Math.Min(1f, state.LerpTime / LerpDuration);

        if (t >= 1f)
        {
            state.Offset = state.TargetOffset;
            state.Angle = state.TargetAngle;
        }
        else
        {
            state.Offset = Vector2.Lerp(state.Offset, state.TargetOffset, t);
            state.Angle = Angle.FromDegrees(MathHelper.Lerp(state.Angle.Degrees, state.TargetAngle.Degrees, t));
        }

        ApplySprite(ent, state.Offset, state.Angle);
        _interpolationState[ent] = state;
    }

    private void UpdateReturning(Entity<PosingComponent> ent, float frameTime)
    {
        if (!_interpolationState.TryGetValue(ent, out var state))
            return;

        if (!state.IsReturning)
        {
            state.IsReturning = true;
            state.LerpTime = 0f;
            state.TargetOffset = Vector2.Zero;
            state.TargetAngle = Angle.Zero;
        }

        state.LerpTime += frameTime;
        var t = Math.Min(1f, state.LerpTime / ReturnDuration);

        if (t >= 1f)
        {
            _interpolationState.Remove(ent);
            ResetSprite(ent);
            return;
        }

        state.Offset = Vector2.Lerp(state.Offset, state.TargetOffset, t);
        state.Angle = Angle.FromDegrees(MathHelper.Lerp(state.Angle.Degrees, state.TargetAngle.Degrees, t));
        _interpolationState[ent] = state;
        ApplySprite(ent, state.Offset, state.Angle);
    }
}
