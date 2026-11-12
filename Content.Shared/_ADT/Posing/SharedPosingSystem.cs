// SPDX-License-Identifier: AGPL-3.0-only AND GPL-3.0-only
// Ported from https://github.com/AdventureTimeSS14/space_station_ADT/blob/296c8b93290412fb8b446c0f42ce107851eefedf/Content.Shared/ADT/Posing/SharedPosingSystem.cs
// Ported from https://github.com/wega-team/ss14-wega/blob/fb7f83ef99fb7bb977976d0649a20dc8c4bba058/Content.Shared/_Wega/Posing/SharedPosingSystem.cs

using System.Linq;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage.Components;
using Content.Shared.Input;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._ADT.Posing;

public abstract partial class SharedPosingSystem : EntitySystem
{
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const float OffsetSpeed = 0.2f;
    private const float AngleSpeed = 20f;

    private readonly Dictionary<(EntityUid Uid, BoundKeyFunction Function), (float Angle, Vector2 Offset, GameTick Start, GameTick End)> _continuousInput = new();

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.TogglePosing, InputCmdHandler.FromDelegate(OnTogglePosingKey, handle: false))
            .Bind(ContentKeyFunctions.PosingOffsetRight, ContinuousHandler(ContentKeyFunctions.PosingOffsetRight, offset: new Vector2(OffsetSpeed, 0f)))
            .Bind(ContentKeyFunctions.PosingOffsetLeft, ContinuousHandler(ContentKeyFunctions.PosingOffsetLeft, offset: new Vector2(-OffsetSpeed, 0f)))
            .Bind(ContentKeyFunctions.PosingOffsetUp, ContinuousHandler(ContentKeyFunctions.PosingOffsetUp, offset: new Vector2(0f, OffsetSpeed)))
            .Bind(ContentKeyFunctions.PosingOffsetDown, ContinuousHandler(ContentKeyFunctions.PosingOffsetDown, offset: new Vector2(0f, -OffsetSpeed)))
            .Bind(ContentKeyFunctions.PosingRotatePositive, ContinuousHandler(ContentKeyFunctions.PosingRotatePositive, angle: -AngleSpeed))
            .Bind(ContentKeyFunctions.PosingRotateNegative, ContinuousHandler(ContentKeyFunctions.PosingRotateNegative, angle: AngleSpeed))
            .Register<SharedPosingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedPosingSystem>();
        _continuousInput.Clear();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var tick = _timing.CurTick;
        var keepTicks = (uint) _timing.TickRate;
        foreach (var (key, input) in _continuousInput.ToArray())
        {
            if (!TryComp<PosingComponent>(key.Uid, out var posing) || !posing.Posing)
            {
                _continuousInput.Remove(key);
                continue;
            }

            if (tick < input.Start)
                continue;

            if (tick >= input.End)
            {
                if (tick.Value - input.End.Value > keepTicks)
                    _continuousInput.Remove(key);

                continue;
            }

            if (input.Angle != 0f)
                TryAdjustPosingAngle(key.Uid, input.Angle * frameTime, posing);

            if (input.Offset != Vector2.Zero)
                TryAdjustPosingOffset(key.Uid, input.Offset * frameTime, posing);
        }
    }

    private InputCmdHandler ContinuousHandler(BoundKeyFunction function, float angle = 0f, Vector2 offset = default)
    {
        return InputCmdHandler.FromDelegate(
            session =>
            {
                if (session?.AttachedEntity is { } userUid && HasComp<PosingComponent>(userUid))
                    _continuousInput[(userUid, function)] = (angle, offset, _timing.CurTick, GameTick.MaxValue);
            },
            session =>
            {
                if (session?.AttachedEntity is not { } userUid
                    || !_continuousInput.TryGetValue((userUid, function), out var input))
                    return;

                input.End = _timing.CurTick;
                _continuousInput[(userUid, function)] = input;
            },
            handle: false);
    }

    private void RemoveContinuousInput(EntityUid uid)
    {
        foreach (var key in _continuousInput.Keys.ToArray())
        {
            if (key.Uid == uid)
                _continuousInput.Remove(key);
        }
    }

    private void OnTogglePosingKey(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } userUid || !TryComp<PosingComponent>(userUid, out var posing))
            return;

        if (posing.Posing || CanStartPosing(userUid))
            TogglePosing(userUid, posing);
    }

    [SubscribeLocalEvent]
    private void OnUpdateCanMove(Entity<PosingComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.Posing)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnDowned(Entity<PosingComponent> ent, ref DownedEvent args)
    {
        if (ent.Comp.Posing)
            TogglePosing(ent, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<PosingComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Posing)
            TogglePosing(ent, ent.Comp);
    }

    private void TogglePosing(EntityUid uid, PosingComponent posingComp)
    {
        posingComp.Posing = !posingComp.Posing;
        _actionBlocker.UpdateCanMove(uid);

        posingComp.CurrentAngle = Angle.Zero;
        posingComp.CurrentOffset = Vector2.Zero;

        if (!posingComp.Posing)
            RemoveContinuousInput(uid);

        ClientTogglePosing(uid, posingComp);
        Dirty(uid, posingComp);
    }

    private void TryAdjustPosingOffset(EntityUid uid, Vector2 offset, PosingComponent? posingComp = null)
    {
        if (!Resolve(uid, ref posingComp, false) || !posingComp.Posing)
            return;

        var previousOffset = posingComp.CurrentOffset;

        posingComp.CurrentOffset += offset;
        posingComp.CurrentOffset = Vector2.Clamp(posingComp.CurrentOffset, -posingComp.OffsetLimits, posingComp.OffsetLimits);

        if (posingComp.CurrentOffset.Equals(previousOffset))
            return;

        Dirty(uid, posingComp);
    }

    private void TryAdjustPosingAngle(EntityUid uid, float angle, PosingComponent? posingComp = null)
    {
        if (!Resolve(uid, ref posingComp, false) || !posingComp.Posing)
            return;

        var previousAngle = posingComp.CurrentAngle;

        var newAngle = posingComp.CurrentAngle.Degrees + angle;
        posingComp.CurrentAngle = Angle.FromDegrees(Math.Clamp(newAngle, -posingComp.AngleLimits, posingComp.AngleLimits));

        if (posingComp.CurrentAngle.Equals(previousAngle))
            return;

        Dirty(uid, posingComp);
    }

    protected virtual void ClientTogglePosing(EntityUid uid, PosingComponent posing)
    {
    }

    private bool CanStartPosing(EntityUid uid)
    {
        if (!_actionBlocker.CanConsciouslyPerformAction(uid))
            return false;

        if (TryComp<StaminaComponent>(uid, out var stamina) && stamina.Critical)
            return false;

        if (HasComp<StunnedComponent>(uid))
            return false;

        return !_standing.IsDown(uid);
    }
}
