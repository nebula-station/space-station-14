// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/5406a8fe0da141f24b2b1e842714a7704b37aba3/Content.Server/_Orion/Power/Systems/InducerSystem.cs

using Content.Shared._Orion.Power.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Verbs;
using Content.Trauma.Common.Effects;
using Robust.Shared.Utility;

namespace Content.Shared._Orion.Power.Systems;

public sealed partial class InducerSystem : EntitySystem
{
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private CommonSparksSystem _sparks = default!;

    private static readonly SpriteSpecifier RateIcon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png"));

    private EntityQuery<BatteryComponent> _batteryQuery;

    public override void Initialize()
    {
        base.Initialize();

        _batteryQuery = GetEntityQuery<BatteryComponent>();
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<InducerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("inducer-examine-rate", ("rate", ent.Comp.TransferRate)));
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<InducerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled
            || args.Target is not { } target
            || !args.CanReach)
            return;

        if (!_batteryQuery.TryComp(target, out var targetBattery))
        {
            _popup.PopupClient(Loc.GetString("inducer-no-battery"), ent, args.User);
            return;
        }

        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var source))
        {
            _popup.PopupClient(Loc.GetString("inducer-no-power-cell"), ent, args.User);
            return;
        }

        if (_battery.GetCharge(source.Value.Owner).Charge <= 0)
        {
            _popup.PopupClient(Loc.GetString("inducer-empty"), ent, args.User);
            return;
        }

        if (_battery.IsFull((target, targetBattery)))
        {
            _popup.PopupClient(Loc.GetString("inducer-target-full"), ent, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.TransferDelay, new InducerDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            RequireCanInteract = true,
            DistanceThreshold = ent.Comp.MaxDistance,
            CancelDuplicate = false,
        };

        _doAfter.TryStartDoAfter(doAfter);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<InducerComponent> ent, ref InducerDoAfterEvent args)
    {
        if (args.Cancelled
            || args.Handled
            || args.Target is not { } target
            || !_batteryQuery.TryComp(target, out var targetBattery)
            || !_powerCell.TryGetBatteryFromSlot(ent.Owner, out var source))
            return;

        var multiplier = Transform(target).Anchored
            ? ent.Comp.StructureTransferMultiplier
            : ent.Comp.TransferMultiplier;

        if (multiplier <= 0f)
            return;

        var sourceBattery = source.Value.AsNullable();
        Entity<BatteryComponent?> targetEnt = (target, targetBattery);
        var (targetCharge, targetMax) = _battery.GetCharge(target);
        var consume = Math.Min(ent.Comp.TransferRate * ent.Comp.TransferDelay, _battery.GetCharge(source.Value.Owner).Charge);
        var receive = Math.Min(consume * multiplier, targetMax - targetCharge);
        if (receive <= 0
            || !_battery.TryUseCharge(sourceBattery, receive / multiplier))
            return;

        _battery.ChangeCharge(targetEnt, receive);
        _sparks.DoSparks(Transform(target).Coordinates, args.User, source: ent);
        args.Handled = true;
        args.Repeat = !_battery.IsFull(targetEnt);
    }

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<InducerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var rates = ent.Comp.AvailableTransferRates;
        if (rates.Count == 0)
            return;

        var index = rates.IndexOf(ent.Comp.TransferRate);
        SetRate(ent, rates[(index + 1) % rates.Count], args.User);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<InducerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        var priority = 0;
        foreach (var rate in ent.Comp.AvailableTransferRates)
        {
            args.Verbs.Add(new Verb
            {
                Category = VerbCategory.SelectType,
                Text = Loc.GetString("inducer-set-transfer-rate", ("rate", rate)),
                Icon = RateIcon,
                Priority = priority--,
                Disabled = ent.Comp.TransferRate == rate,
                Act = () => SetRate(ent, rate, user),
            });
        }
    }

    private void SetRate(Entity<InducerComponent> ent, int rate, EntityUid user)
    {
        ent.Comp.TransferRate = rate;
        Dirty(ent);
        _popup.PopupClient(Loc.GetString("inducer-transfer-rate-set", ("rate", rate)), ent, user);
    }
}
