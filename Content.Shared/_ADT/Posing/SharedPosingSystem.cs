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

    private readonly Dictionary<EntityUid, (float Angle, float OffsetX, float OffsetY, TimeSpan LastUpdate)> _continuousInput = new();

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.TogglePosing, InputCmdHandler.FromDelegate(OnTogglePosingKey, handle: false))
            .Bind(ContentKeyFunctions.PosingOffsetRight, ContinuousHandler(offsetX: OffsetSpeed))
            .Bind(ContentKeyFunctions.PosingOffsetLeft, ContinuousHandler(offsetX: -OffsetSpeed))
            .Bind(ContentKeyFunctions.PosingOffsetUp, ContinuousHandler(offsetY: OffsetSpeed))
            .Bind(ContentKeyFunctions.PosingOffsetDown, ContinuousHandler(offsetY: -OffsetSpeed))
            .Bind(ContentKeyFunctions.PosingRotatePositive, ContinuousHandler(angle: -AngleSpeed))
            .Bind(ContentKeyFunctions.PosingRotateNegative, ContinuousHandler(angle: AngleSpeed))
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

        var now = _timing.CurTime;
        foreach (var (uid, input) in _continuousInput.ToArray())
        {
            if (!TryComp<PosingComponent>(uid, out var posing) || !posing.Posing)
            {
                _continuousInput.Remove(uid);
                continue;
            }

            var timeSinceLastUpdate = now - input.LastUpdate;
            if (timeSinceLastUpdate.TotalSeconds < 0.016)
                continue;

            var delta = Math.Min(0.1f, (float) timeSinceLastUpdate.TotalSeconds);

            if (input.Angle != 0)
                TryAdjustPosingAngle(uid, input.Angle * delta, posing);

            if (input.OffsetX != 0 || input.OffsetY != 0)
                TryAdjustPosingOffset(uid, new Vector2(input.OffsetX, input.OffsetY) * delta, posing);

            _continuousInput[uid] = (input.Angle, input.OffsetX, input.OffsetY, now);
        }
    }

    private InputCmdHandler ContinuousHandler(float angle = 0f, float offsetX = 0f, float offsetY = 0f)
    {
        return InputCmdHandler.FromDelegate(
            session =>
            {
                if (session?.AttachedEntity is { } userUid && HasComp<PosingComponent>(userUid))
                    _continuousInput[userUid] = (angle, offsetX, offsetY, _timing.CurTime);
            },
            session =>
            {
                if (session?.AttachedEntity is { } userUid)
                    _continuousInput.Remove(userUid);
            },
            handle: false);
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
            _continuousInput.Remove(uid);

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
