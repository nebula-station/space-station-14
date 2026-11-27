// SPDX-License-Identifier: AGPL-3.0-only
// Ported from https://github.com/AdventureTimeSS14/space_station_ADT/blob/921015157ff5a0e4d3156c1fccc0a1f3c34c0a96/Content.Server/ADT/Casino/SlotMachineCasinoSystem.cs

using Content.Server.Stack;
using Content.Shared._ADT.Casino;
using Content.Shared.Cargo.Components;
using Content.Shared.Chat;
using Content.Shared.Emag.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._ADT.Casino;

public sealed partial class SlotMachineCasinoSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private StackSystem _stack = default!; // Nebula edit
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public const string SymSeven = "seven";
    public const string SymWild = "wild";
    public const string SymScatter = "scatter";
    private int _globalPlays;
    private int _globalJackpots;
    private DateOnly _statsDay;
    private bool _statsInitialized;

    // Nebula start
    private const float UpdateInterval = 0.1f;
    private float _updateAccumulator;
    private static readonly ProtoId<StackPrototype> HolochipStack = "CreditHolochip";
    // Nebula end

    public int GlobalPlays => _globalPlays;
    public int GlobalJackpots => _globalJackpots;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SlotMachineCasinoComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;

        if (!_statsInitialized)
        {
            _globalJackpots = _random.Next(1, 5); // false hope
            _globalPlays = _random.Next(75, 201);
            _statsDay = DateOnly.FromDateTime(DateTime.UtcNow);
            _statsInitialized = true;
        }

        RollReels(ent.Comp);
        comp.Balance = 0;
        comp.BetAmount = comp.SpinPriceMin;
        comp.ActiveLines = GetActiveLines(comp);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnUiOpened(Entity<SlotMachineCasinoComponent> ent, ref BoundUIOpenedEvent args)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today != _statsDay)
        {
            _statsDay = today;
            _globalPlays = 0;
        }

        _globalPlays++;
        PushState(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Nebula start
        _updateAccumulator += frameTime;
        if (_updateAccumulator < UpdateInterval)
            return;

        frameTime = _updateAccumulator;
        _updateAccumulator = 0f;
        // Nebula end

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<SlotMachineCasinoComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_power.IsPowered(uid))
                continue;

            comp.MoneyRemainder += frameTime / 2f;
            if (comp.MoneyRemainder >= 1f)
            {
                var whole = (int)comp.MoneyRemainder;
                comp.MoneyRemainder -= whole;
                comp.Money += whole;
            }

            if (comp.ShowWinOverlay && now >= comp.WinBlinkEndTime)
            {
                comp.ShowWinOverlay = false;
                _appearance.SetData(uid, SlotMachineCasinoVisuals.WinBlink, false);
            }

            if (!comp.Working)
            {
                if (comp.NextAutoSpinTime > TimeSpan.Zero && now >= comp.NextAutoSpinTime)
                {
                    comp.NextAutoSpinTime = TimeSpan.Zero;
                    if (comp.FreeSpinsRemaining > 0 || comp.Balance >= comp.BetAmount)
                        TrySpin((uid, comp), null);
                }
                continue;
            }

            if (comp.NextReelToStop < comp.Reels.Length && now >= comp.NextReelStopTime)
            {
                comp.ReelSpinning[comp.NextReelToStop] = false;
                comp.StoppedReels++;
                _appearance.SetData(uid, SlotMachineCasinoVisuals.StoppedReels, comp.StoppedReels);
                comp.NextReelToStop++;
                comp.NextReelStopTime = now + TimeSpan.FromSeconds(comp.ReelStopDelay);

                if (comp.NextReelToStop >= comp.Reels.Length)
                    ResolveSpin((uid, comp), now);
                else
                    PushState((uid, comp));
            }
        }
    }

    // === Player actions ===

    [SubscribeLocalEvent]
    private void OnAction(Entity<SlotMachineCasinoComponent> ent, ref SlotMachineCasinoActionMessage msg)
    {
        if (!_power.IsPowered(ent.Owner))
            return;

        switch (msg.Action)
        {
            case SlotMachineCasinoAction.Spin:
                TrySpin(ent, msg.Actor);
                break;
            case SlotMachineCasinoAction.SetBet:
                if (msg.Amount >= ent.Comp.SpinPriceMin && msg.Amount <= ent.Comp.SpinPriceMax)
                {
                    ent.Comp.BetAmount = msg.Amount;
                    ent.Comp.ActiveLines = GetActiveLines(ent.Comp);
                    PushState(ent);
                }
                break;
            case SlotMachineCasinoAction.AutoSpin:
                if (msg.Amount > 0 && msg.Amount <= ent.Comp.AutoSpinMax && !ent.Comp.Working)
                {
                    ent.Comp.AutoSpin = true;
                    ent.Comp.AutoSpinRemaining = msg.Amount;
                    TrySpin(ent, msg.Actor);
                }
                break;
            case SlotMachineCasinoAction.StopAuto:
                StopAuto(ent.Comp);
                PushState(ent);
                break;
            case SlotMachineCasinoAction.CashOut:
                CashOut(ent, msg.Actor);
                break;
            case SlotMachineCasinoAction.Gamble:
                DoGamble(ent, msg.Choice);
                break;
            case SlotMachineCasinoAction.Collect:
                ent.Comp.GambleAvailable = false;
                ent.Comp.GambleAmount = 0;
                PushState(ent);
                break;
        }
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<SlotMachineCasinoComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<CashComponent>(args.Used, out _) || !TryComp<StackComponent>(args.Used, out var stack))
            return;

        args.Handled = true;
        var amount = _stack.GetCount(args.Used);
        ent.Comp.Balance += amount;
        PlayCasinoSound(ent, ent.Comp.InsertSound, -5f);
        if (args.User != default)
            _popup.PopupEntity(Loc.GetString("slotmachine-casino-inserted", ("amount", amount)), ent.Owner, args.User); // Nebula edit
        QueueDel(args.Used);
        PushState(ent);
    }

    [SubscribeLocalEvent]
    private void OnEmagged(Entity<SlotMachineCasinoComponent> ent, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }

    // === Money out ===

    private void CashOut(Entity<SlotMachineCasinoComponent> ent, EntityUid? user)
    {
        var comp = ent.Comp;
        if (comp.Balance <= 0)
            return;

        // Nebula start
        foreach (var chip in _stack.SpawnMultipleAtPosition(HolochipStack, comp.Balance, Transform(ent.Owner).Coordinates))
        {
            if (user != null)
                _hands.PickupOrDrop(user.Value, chip);
        }
        // Nebula end

        comp.Balance = 0;
        comp.SessionWinnings = 0;
        comp.WinStreak = 0;
        comp.LossStreak = 0;
        StopAuto(comp);
        comp.FreeSpinsRemaining = 0;
        PushState(ent);
    }

    private void DoGamble(Entity<SlotMachineCasinoComponent> ent, string choice)
    {
        var comp = ent.Comp;
        if (!comp.GambleAvailable || comp.GambleAmount <= 0 || comp.Working)
            return;

        if (choice != "red" && choice != "black")
            return;

        if (comp.Balance < comp.GambleAmount)
        {
            comp.ResultMessage = Loc.GetString("slotmachine-casino-gamble-nofunds");
            comp.ResultType = "lose";
            comp.GambleAvailable = false;
            comp.GambleAmount = 0;
            PushState(ent);
            return;
        }

        comp.Balance -= comp.GambleAmount;
        if (_random.Prob(comp.GambleWinChance))
        {
            comp.SessionWinnings += comp.GambleAmount;
            comp.GambleAmount *= 2;
            comp.Balance += comp.GambleAmount;
            comp.ResultMessage = Loc.GetString("slotmachine-casino-gamble-win", ("amount", comp.GambleAmount));
            comp.ResultType = "win";
            PlayCasinoSound(ent, comp.MoneySound, -3f);
        }
        else
        {
            comp.SessionWinnings -= comp.GambleAmount;
            comp.ResultMessage = Loc.GetString("slotmachine-casino-gamble-lose", ("amount", comp.GambleAmount));
            comp.ResultType = "lose";
            comp.GambleAmount = 0;
            comp.GambleAvailable = false;
            comp.WinStreak = 0;
        }

        PushState(ent);
    }

    // === Helpers ===

    public void StopAuto(SlotMachineCasinoComponent comp)
    {
        comp.AutoSpin = false;
        comp.AutoSpinRemaining = 0;
        comp.NextAutoSpinTime = TimeSpan.Zero;
    }

    private void PlayCasinoSound(Entity<SlotMachineCasinoComponent> ent, SoundSpecifier sound, float volume = 0f)
    {
        _audio.PlayPvs(sound, ent.Owner, AudioParams.Default.WithVolume(volume).WithPitchScale(_random.NextFloat(0.9f, 1.15f)));
    }

    private void PushState(Entity<SlotMachineCasinoComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, SlotMachineCasinoUiKey.Key))
            return;

        _ui.SetUiState(ent.Owner, SlotMachineCasinoUiKey.Key, BuildState(ent));
    }

    private SlotMachineCasinoBoundUserInterfaceState BuildState(Entity<SlotMachineCasinoComponent> ent)
    {
        var comp = ent.Comp;
        var reels = new string[5][];
        var stopped = new bool[5];
        for (var i = 0; i < 5; i++)
        {
            reels[i] = (string[])comp.Reels[i].Clone();
            stopped[i] = !comp.ReelSpinning[i];
        }

        return new SlotMachineCasinoBoundUserInterfaceState
        {
            Money = comp.Money,
            Balance = comp.Balance,
            BetAmount = comp.BetAmount,
            Working = comp.Working,
            ActiveLines = comp.ActiveLines,
            WinningLine = comp.WinningLine,
            Plays = _globalPlays,
            SpinCount = comp.SpinCount,
            Jackpots = _globalJackpots,
            ResultMessage = comp.ResultMessage,
            ResultType = comp.ResultType,
            BonusMultiplier = comp.BonusMultiplier,
            BonusActive = comp.BonusActive,
            WinStreak = comp.WinStreak,
            LossStreak = comp.LossStreak,
            SessionWinnings = comp.SessionWinnings,
            AutoSpin = comp.AutoSpin,
            AutoSpinRemaining = comp.AutoSpinRemaining,
            FreeSpinsRemaining = comp.FreeSpinsRemaining,
            GambleAvailable = comp.GambleAvailable,
            GambleAmount = comp.GambleAmount,
            NearMiss = comp.NearMiss,
            NearMissMessage = comp.NearMissMessage,
            ShowConfetti = comp.ShowConfetti,
            Reels = reels,
            ReelsStopped = stopped,
            History = [.. comp.SpinHistory],
        };
    }
}
