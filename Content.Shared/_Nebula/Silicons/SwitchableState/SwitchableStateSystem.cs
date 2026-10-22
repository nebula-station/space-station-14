// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.Silicons.SwitchableState;

public sealed partial class ChangeSpriteSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private SharedHandheldLightSystem _lightSystem = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    #region  Base events

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BorgSwitchableStateComponent> ent, ref MapInitEvent args)
    {
        if (!_action.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.ActionId))
            return;

        _action.SetEntityIcon((ent.Comp.ActionEntity.Value, null), ent.Owner);
        Dirty(ent);

        var userInterfaceComp = EnsureComp<UserInterfaceComponent>(ent);
        _ui.SetUi((ent, userInterfaceComp), SwitchStateUiKey.Key, new InterfaceData("SwitchableStateBoundUserInterface"));
    }


    [SubscribeLocalEvent]
    private void OnShutdown(Entity<BorgSwitchableStateComponent> ent, ref ComponentShutdown args)
    {
        _action.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
        _ui.CloseUi(ent.Owner, SwitchStateUiKey.Key);
    }

    #endregion
    #region Events

    [SubscribeLocalEvent]
    private void OnSwitchStateActionEvent(Entity<BorgSwitchableStateComponent> ent, ref SwitchStateActionEvent args)
    {
        if (args.Handled || !TryComp<UserInterfaceComponent>(ent, out var userInterfaceComp))
            return;

        args.Handled = true;

        if (!_ui.IsUiOpen((ent, userInterfaceComp), SwitchStateUiKey.Key, args.Performer))
            _ui.OpenUi((ent, userInterfaceComp), SwitchStateUiKey.Key, args.Performer);
        else
            _ui.CloseUi((ent, userInterfaceComp), SwitchStateUiKey.Key, args.Performer);
    }

    [SubscribeLocalEvent]
    private void OnSwitchStateMessage(Entity<BorgSwitchableStateComponent> ent, ref SwitchStateMessage args)
    {
        _ui.CloseUi(ent.Owner, SwitchStateUiKey.Key, args.Actor);

        if (!CanSwitch(ent, args.Type))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            ent.Owner,
            ent.Comp.DoAfterDuration,
            new SwitchStateDoAfterEvent(args.Type),
            ent.Owner)
        {
            BreakOnDamage = false,
            BlockDuplicate = true,
            BreakOnHandChange = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<BorgSwitchableStateComponent> ent, ref SwitchStateDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!CanSwitch(ent, args.Type))
            return;

        args.Handled = true;

        ChangeType(ent, args.Type);
    }

    #endregion
    #region Functions

    public void SetupStates(EntityUid uid,
        Dictionary<BorgStateType, Vector2> statesWhiteList,
        TimeSpan doAfterDuration,
        SpriteSpecifier baseSprite)
    {
        if (TryComp<BorgSwitchableStateComponent>(uid, out var existing) && existing.CurrentType != BorgStateType.Base)
            ChangeType((uid, existing), BorgStateType.Base);

        if (statesWhiteList.Count == 0)
        {
            RemComp<BorgSwitchableStateComponent>(uid);
            return;
        }

        var comp = EnsureComp<BorgSwitchableStateComponent>(uid);
        comp.DoAfterDuration = doAfterDuration;
        comp.StatesWhiteList = new Dictionary<BorgStateType, Vector2>(statesWhiteList);
        comp.StatesWhiteList.TryAdd(BorgStateType.Base, Vector2.One);
        comp.BaseSprite = baseSprite;

        Dirty(uid, comp);
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private bool CanSwitch(Entity<BorgSwitchableStateComponent> ent, BorgStateType type)
    {
        return type != ent.Comp.CurrentType
            && ent.Comp.StatesWhiteList.ContainsKey(type)
            && _mobState.IsAlive(ent);
    }

    private void ChangeType(Entity<BorgSwitchableStateComponent> ent, BorgStateType newType)
    {
        SetRest(ent, newType != BorgStateType.Base);

        ent.Comp.CurrentType = newType;

        _appearance.SetData(ent, SwitchStateVisuals.Key, newType);

        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(ent.Owner);
        Dirty(ent);
    }

    private void SetRest(Entity<BorgSwitchableStateComponent> ent, bool isActive)
    {
        if (TryComp<SpriteMovementComponent>(ent.Owner, out var spriteMovement))
        {
            spriteMovement.IsEnabled = !isActive;
            DirtyField(ent.Owner, spriteMovement, nameof(SpriteMovementComponent.IsEnabled));
        }

        if (isActive && TryComp<HandheldLightComponent>(ent.Owner, out var comp))
            _lightSystem.TurnOff((ent.Owner, comp));

        ent.Comp.IsRestActive = isActive;
    }

    [SubscribeLocalEvent]
    private void OnLightToggle(Entity<BorgSwitchableStateComponent> ent, ref LightToggleEvent args)
    {
        if (ent.Comp.CurrentType != BorgStateType.Base && TryComp<HandheldLightComponent>(ent.Owner, out var comp))
            _lightSystem.TurnOff((ent.Owner, comp), false);
    }

    [SubscribeLocalEvent]
    private void OnRefreshSpeed(Entity<BorgSwitchableStateComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.StatesWhiteList.TryGetValue(ent.Comp.CurrentType, out var vector))
            return;

        args.ModifySpeed(vector.X, vector.Y);
    }

    [SubscribeLocalEvent]
    private void OnMobstateChanged(Entity<BorgSwitchableStateComponent> ent, ref MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Critical:
            case MobState.Dead:
                if (ent.Comp.StatesWhiteList.ContainsKey(BorgStateType.Wreck))
                    ChangeType(ent, BorgStateType.Wreck);
                break;
            case MobState.Alive when ent.Comp.CurrentType == BorgStateType.Wreck:
                ChangeType(ent, BorgStateType.Base);
                break;
        }
    }

    #endregion
}
