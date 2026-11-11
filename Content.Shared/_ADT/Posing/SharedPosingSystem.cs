// SPDX-License-Identifier: AGPL-3.0-only
// Ported from https://github.com/AdventureTimeSS14/space_station_ADT/blob/296c8b93290412fb8b446c0f42ce107851eefedf/Content.Shared/ADT/Posing/SharedPosingSystem.cs

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

namespace Content.Shared._ADT.Posing;

public abstract partial class SharedPosingSystem : EntitySystem
{
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;

    private const float OffsetStep = 0.05f;
    private const float AngleStep = 5f;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.TogglePosing, InputCmdHandler.FromDelegate(OnTogglePosingKey, handle: false))
            .Bind(ContentKeyFunctions.PosingOffsetRight, OffsetHandler(new(OffsetStep, 0f)))
            .Bind(ContentKeyFunctions.PosingOffsetLeft, OffsetHandler(new(-OffsetStep, 0f)))
            .Bind(ContentKeyFunctions.PosingOffsetUp, OffsetHandler(new(0f, OffsetStep)))
            .Bind(ContentKeyFunctions.PosingOffsetDown, OffsetHandler(new(0f, -OffsetStep)))
            .Bind(ContentKeyFunctions.PosingRotatePositive, AngleHandler(-AngleStep))
            .Bind(ContentKeyFunctions.PosingRotateNegative, AngleHandler(AngleStep))
            .Register<SharedPosingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedPosingSystem>();
    }

    private InputCmdHandler OffsetHandler(Vector2 offset)
    {
        return InputCmdHandler.FromDelegate(session =>
            {
                if (session?.AttachedEntity is { } userUid)
                    TryAdjustPosingOffset(userUid, offset);
            },
            handle: false);
    }

    private InputCmdHandler AngleHandler(float angle)
    {
        return InputCmdHandler.FromDelegate(session =>
            {
                if (session?.AttachedEntity is { } userUid)
                    TryAdjustPosingAngle(userUid, angle);
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
