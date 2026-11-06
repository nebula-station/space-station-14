// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873de20e597282565f27b686331e65f677/Content.Server/_Orion/EnergyDome/Systems/EnergyDomeSystem.cs

using Content.Server._Orion.EnergyDome.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Timing.Components;
using Content.Shared.Timing.Systems;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server._Orion.EnergyDome.Systems;

public sealed partial class EnergyDomeSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private DeviceLinkSystem _signalSystem = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<EnergyDomeGeneratorComponent> generator, ref MapInitEvent args)
    {
        if (generator.Comp.CanDeviceNetworkUse)
            _signalSystem.EnsureSinkPorts(generator, generator.Comp.TogglePort, generator.Comp.OnPort, generator.Comp.OffPort);
    }

    #region Use Ways

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<EnergyDomeGeneratorComponent> generator, ref SignalReceivedEvent args)
    {
        if (!generator.Comp.CanDeviceNetworkUse)
            return;

        if (args.Port == generator.Comp.OnPort)
        {
            AttemptToggle(generator, true);
        }
        if (args.Port == generator.Comp.OffPort)
        {
            AttemptToggle(generator, false);
        }
        if (args.Port == generator.Comp.TogglePort)
        {
            AttemptToggle(generator, !generator.Comp.Enabled);
        }
    }

    [SubscribeLocalEvent]
    private void OnActivatedInWorld(Entity<EnergyDomeGeneratorComponent> generator, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !generator.Comp.CanInteractUse)
            return;

        AttemptToggle(generator, !generator.Comp.Enabled);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnExamine(Entity<EnergyDomeGeneratorComponent> generator, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(
            (generator.Comp.Enabled)
            ? "energy-dome-on-examine-is-on-message"
            : "energy-dome-on-examine-is-off-message"
            ));
    }

    [SubscribeLocalEvent]
    private void AddToggleDomeVerb(Entity<EnergyDomeGeneratorComponent> generator, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !generator.Comp.CanInteractUse)
            return;

        ActivationVerb verb = new()
        {
            Text = Loc.GetString("energy-dome-verb-toggle"),
            Act = () => AttemptToggle(generator, !generator.Comp.Enabled)
        };

        args.Verbs.Add(verb);
    }

    [SubscribeLocalEvent]
    private void OnGetActions(Entity<EnergyDomeGeneratorComponent> generator, ref GetItemActionsEvent args)
    {
        if (generator.Comp.CanInteractUse)
            args.AddAction(ref generator.Comp.ToggleActionEntity, generator.Comp.ToggleAction);
    }

    [SubscribeLocalEvent]
    private void OnToggleAction(Entity<EnergyDomeGeneratorComponent> generator, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        AttemptToggle(generator, !generator.Comp.Enabled);
        args.Handled = true;
    }

    #endregion

    #region Interactions

    [SubscribeLocalEvent]
    private void OnPowerCellSlotEmpty(Entity<EnergyDomeGeneratorComponent> generator, ref PowerCellSlotEmptyEvent args)
    {
        TurnOff(generator, true);
    }

    [SubscribeLocalEvent]
    private void OnPowerCellChanged(Entity<EnergyDomeGeneratorComponent> generator, ref PowerCellChangedEvent args)
    {
        if (args.Ejected || !_powerCell.HasDrawCharge(generator.Owner))
            TurnOff(generator, true);
    }

    [SubscribeLocalEvent]
    private void OnChargeChanged(Entity<EnergyDomeGeneratorComponent> generator, ref ChargeChangedEvent args)
    {
        if (args.CurrentCharge == 0)
            TurnOff(generator, true);
    }

    [SubscribeLocalEvent]
    private void OnDomeDamaged(Entity<EnergyDomeComponent> dome, ref DamageDealtEvent args)
    {
        if (dome.Comp.Generator == null)
            return;

        var generatorUid = dome.Comp.Generator.Value;
        if (!TryComp<EnergyDomeGeneratorComponent>(generatorUid, out var generatorComp))
            return;

        var totalDamage = args.Damage.GetTotal().Float();
        var energyLeak = totalDamage * generatorComp.DamageEnergyDraw;

        _audio.PlayPvs(generatorComp.ParrySound, dome);

        if (HasComp<PowerCellDrawComponent>(generatorUid))
        {
            if (_powerCell.TryGetBatteryFromSlot(generatorUid, out var cell))
            {
                _battery.UseCharge(cell.Value.AsNullable(), energyLeak);

                if (_battery.GetCharge(cell.Value.Owner).Charge <= 0)
                    TurnOff((generatorUid, generatorComp), true);
            }
        }

        // It seems to me it would not work well to hang both a powercell and an internal battery with wire charging on the object....
        if (!TryComp<BatteryComponent>(generatorUid, out var battery))
            return;

        _battery.UseCharge((generatorUid, battery), energyLeak);

        if (_battery.GetCharge(generatorUid).Charge <= 0)
            TurnOff((generatorUid, generatorComp), true);
    }

    [SubscribeLocalEvent]
    private void OnParentChanged(Entity<EnergyDomeGeneratorComponent> generator, ref EntParentChangedMessage args)
    {
        // TODO: taking the active barrier in hand for some reason does not manage to change the parent in this case,
        // and the barrier is not turned off.
        if (GetProtectedEntity(generator) != generator.Comp.DomeParentEntity)
            TurnOff(generator, false);
    }

    [SubscribeLocalEvent]
    private void OnComponentRemove(Entity<EnergyDomeGeneratorComponent> generator, ref ComponentRemove args)
    {
        TurnOff(generator, false);
    }

    #endregion

    #region Functional

    public bool AttemptToggle(Entity<EnergyDomeGeneratorComponent> generator, bool status)
    {
        if (_useDelay.IsDelayed(generator.Owner))
        {
            _audio.PlayPvs(generator.Comp.TurnOffSound, generator);
            _popup.PopupEntity(
                    Loc.GetString("energy-dome-recharging"),
                    generator);
            return false;
        }

        if (TryComp<PowerCellSlotComponent>(generator, out _))
        {
            if (!_powerCell.TryGetBatteryFromSlot(generator.Owner, out _) && !TryComp<BatteryComponent>(generator.Owner, out _))
            {
                _audio.PlayPvs(generator.Comp.TurnOffSound, generator);
                _popup.PopupEntity(
                    Loc.GetString("energy-dome-no-cell"),
                    generator);
                return false;
            }

            if (status && !_powerCell.HasCharge(generator.Owner, 1f))
            {
                _audio.PlayPvs(generator.Comp.TurnOffSound, generator);
                _popup.PopupEntity(
                    Loc.GetString("energy-dome-no-power"),
                    generator);
                return false;
            }
        }

        if (HasComp<BatteryComponent>(generator))
        {
            if (_battery.GetCharge(generator.Owner).Charge <= 0)
            {
                _audio.PlayPvs(generator.Comp.TurnOffSound, generator);
                _popup.PopupEntity(
                    Loc.GetString("energy-dome-no-power"),
                    generator);
                return false;
            }
        }

        Toggle(generator, status);
        return true;
    }

    private void Toggle(Entity<EnergyDomeGeneratorComponent> generator, bool status)
    {
        if (status)
            TurnOn(generator);
        else
            TurnOff(generator, false);
    }

    private void TurnOn(Entity<EnergyDomeGeneratorComponent> generator)
    {
        if (generator.Comp.Enabled)
            return;

        var protectedEntity = GetProtectedEntity(generator);

        var newDome = Spawn(generator.Comp.DomePrototype, Transform(protectedEntity).Coordinates);
        generator.Comp.DomeParentEntity = protectedEntity;
        _transform.SetParent(newDome, protectedEntity);

        if (TryComp<EnergyDomeComponent>(newDome, out var domeComp))
        {
            domeComp.Generator = generator;
        }

        if (TryComp<PowerCellDrawComponent>(generator.Owner, out _))
        {
            _powerCell.SetDrawEnabled(generator.Owner, true);
        }

        if (TryComp<BatterySelfRechargerComponent>(generator, out var recharger))
        {
            recharger.AutoRechargeRate = generator.Comp.InternalBatteryChargeRate;
            Dirty(generator.Owner, recharger);
            _battery.RefreshChargeRate(generator.Owner);
        }

        generator.Comp.SpawnedDome = newDome;
        _audio.PlayPvs(generator.Comp.TurnOnSound, generator);
        generator.Comp.Enabled = true;
    }

    private void TurnOff(Entity<EnergyDomeGeneratorComponent> generator, bool startReloading)
    {
        if (!generator.Comp.Enabled)
            return;

        generator.Comp.Enabled = false;
        QueueDel(generator.Comp.SpawnedDome);

        if (TryComp<PowerCellDrawComponent>(generator.Owner, out _))
        {
            _powerCell.SetDrawEnabled(generator.Owner, false);
        }
        if (TryComp<BatterySelfRechargerComponent>(generator, out var recharger))
        {
            recharger.AutoRechargeRate = 0f;
            Dirty(generator.Owner, recharger);
            _battery.RefreshChargeRate(generator.Owner);
        }

        _audio.PlayPvs(generator.Comp.TurnOffSound, generator);

        if (!startReloading)
            return;

        _audio.PlayPvs(generator.Comp.EnergyOutSound, generator);

        if (TryComp<UseDelayComponent>(generator, out var useDelay))
            _useDelay.TryResetDelay((generator.Owner, useDelay));
    }

    #endregion

    #region Util

    private EntityUid GetProtectedEntity(EntityUid entity)
    {
        return (_container.TryGetOuterContainer(entity, Transform(entity), out var container))
            ? container.Owner
            : entity;
    }

    #endregion
}
