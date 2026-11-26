// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Arcane.Server/Economy/BankSystem.cs

using Content.Shared._Arcane.CCVar;
using Content.Shared._Arcane.Economy;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Stack;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Station.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.Economy;

/// <summary>
///     Personal bank accounts linked to ID cards, paydays and credit holochips.
/// </summary>
public sealed partial class BankSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private CargoSystem _cargo = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    private static readonly ProtoId<StackPrototype> HolochipStack = "CreditHolochip";
    private static readonly ProtoId<StackPrototype> CreditStack = "Credit";
    private static readonly SoundSpecifier PaydaySound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    private TimeSpan? _nextPayday;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_nextPayday is not { } next || _timing.CurTime < next)
            return;

        _nextPayday = _timing.CurTime + TimeSpan.FromMinutes(_cfg.GetCVar(ArcaneCVars.PaydayInterval));
        ProcessPayday();
    }

    [SubscribeLocalEvent]
    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _nextPayday = null;
    }

    [SubscribeLocalEvent]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        _nextPayday ??= _timing.CurTime + TimeSpan.FromMinutes(_cfg.GetCVar(ArcaneCVars.PaydayInterval));

        if (!_mind.TryGetMind(args.Mob, out var mindId, out var mind))
            return;

        var account = EnsureAccount((mindId, mind));
        account.Comp.Station = args.Station;

        if (args.JobId != null && account.Comp.JobId == null && _proto.TryIndex<JobSalaryPrototype>(args.JobId, out var salary))
        {
            account.Comp.JobId = args.JobId;
            ChangeBalance(account, salary.Salary, "starting payroll");
        }

        if (_idCard.TryFindIdCard(args.Mob, out var idCard) && TryComp<BankCardComponent>(idCard, out var card))
            LinkCard((idCard, card), account);
    }

    /// <summary>
    ///     Gets the account of a mind, opening one when it has none.
    /// </summary>
    public Entity<BankAccountComponent> EnsureAccount(Entity<MindComponent> mind)
    {
        var account = EnsureComp<BankAccountComponent>(mind);
        if (account.AccountId == string.Empty)
            account.AccountId = GenerateAccountId();

        if (!string.IsNullOrWhiteSpace(mind.Comp.CharacterName))
            account.OwnerName = mind.Comp.CharacterName;

        return (mind, account);
    }

    public void LinkCard(Entity<BankCardComponent> card, Entity<BankAccountComponent> account)
    {
        card.Comp.AccountId = account.Comp.AccountId;
        card.Comp.Balance = account.Comp.Balance;
        card.Comp.Department = GetDepartment(account);
        Dirty(card);
    }

    public bool TryFindAccount(string? accountId, out Entity<BankAccountComponent> account)
    {
        account = default;
        if (string.IsNullOrEmpty(accountId))
            return false;

        var query = EntityQueryEnumerator<BankAccountComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AccountId != accountId)
                continue;

            account = (uid, comp);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Finds the account linked to the ID card of a user.
    /// </summary>
    public bool TryGetUserAccount(EntityUid user, out Entity<BankAccountComponent> account)
    {
        account = default;
        return _idCard.TryFindIdCard(user, out var idCard)
            && TryComp<BankCardComponent>(idCard, out var card)
            && TryFindAccount(card.AccountId, out account);
    }

    /// <summary>
    ///     Adds or, with a negative amount, withdraws credits. Fails when the account would go below zero.
    /// </summary>
    public bool ChangeBalance(Entity<BankAccountComponent> account, int amount, string reason)
    {
        if (amount == 0)
            return true;

        long balance = (long) account.Comp.Balance + amount;
        if (balance < 0 || balance > int.MaxValue)
            return false;

        account.Comp.Balance = (int) balance;
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"Bank account {account.Comp.AccountId} ({account.Comp.OwnerName}) changed by {amount} for {reason}, balance {account.Comp.Balance}");

        SyncCards(account);
        return true;
    }

    private void SyncCards(Entity<BankAccountComponent> account)
    {
        var query = EntityQueryEnumerator<BankCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (card.AccountId != account.Comp.AccountId)
                continue;

            card.Balance = account.Comp.Balance;
            Dirty(uid, card);
        }
    }

    private ProtoId<CargoAccountPrototype>? GetDepartment(Entity<BankAccountComponent> account)
    {
        return account.Comp.JobId is { } job && _proto.TryIndex<JobSalaryPrototype>(job, out var salary)
            ? salary.Department
            : null;
    }

    private string GenerateAccountId()
    {
        while (true)
        {
            var id = _random.Next(100_000, 1_000_000).ToString() + _random.Next(100_000, 1_000_000).ToString();
            if (!TryFindAccount(id, out _))
                return id;
        }
    }

    /// <summary>
    ///     Pays every employee with a salary. Department salaries come from the station department account,
    ///     as much as it can still pay.
    /// </summary>
    private void ProcessPayday()
    {
        var query = EntityQueryEnumerator<BankAccountComponent, MindComponent>();
        while (query.MoveNext(out var uid, out var account, out var mind))
        {
            if (mind.OwnedEntity is not { } owned || mind.UserId == null || account.JobId is not { } job
                || !_proto.TryIndex<JobSalaryPrototype>(job, out var salary) || salary.Salary <= 0)
                continue;

            var paid = salary.Salary;
            if (salary.Department is { } department)
            {
                var station = _station.GetOwningStation(owned) ?? account.Station;
                if (!TryComp<StationBankAccountComponent>(station, out var bank))
                    continue;

                paid = Math.Min(paid, _cargo.GetBalanceFromAccount((station.Value, bank), department));
                if (paid <= 0)
                    continue;

                _cargo.UpdateBankAccount((station.Value, bank), -paid, department);
            }

            if (!ChangeBalance((uid, account), paid, "payday"))
                continue;

            _popup.PopupEntity(Loc.GetString("bank-payday-received", ("amount", paid)), owned, owned);
            _audio.PlayEntity(PaydaySound, owned, owned, AudioParams.Default.WithVolume(-2f));
        }
    }

    [SubscribeLocalEvent]
    private void OnAccountLookup(ref BankAccountLookupEvent args)
    {
        if (!TryGetUserAccount(args.User, out var account))
            return;

        args.AccountId = account.Comp.AccountId;
        args.OwnerName = account.Comp.OwnerName;
    }

    [SubscribeLocalEvent]
    private void OnAccountChange(ref BankAccountChangeEvent args)
    {
        if (args.Handled || !TryFindAccount(args.AccountId, out var account))
            return;

        args.Handled = ChangeBalance(account, args.Amount, args.Reason);
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<BankCardComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<StackComponent>(args.Used, out var stack) || stack.Count <= 0
            || stack.StackTypeId != HolochipStack && stack.StackTypeId != CreditStack)
            return;

        args.Handled = true;
        if (!TryFindAccount(ent.Comp.AccountId, out var account))
        {
            _popup.PopupEntity(Loc.GetString("bank-card-no-account"), ent, args.User);
            return;
        }

        if (!ChangeBalance(account, stack.Count, "card deposit"))
            return;

        _popup.PopupEntity(Loc.GetString("bank-card-deposited", ("amount", stack.Count)), ent, args.User);
        _stack.SetCount((args.Used, stack), 0);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnUiOpened(Entity<BankCardComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnWithdraw(Entity<BankCardComponent> ent, ref BankCardWithdrawMessage args)
    {
        if (args.Amount <= 0 || !TryFindAccount(ent.Comp.AccountId, out var account))
            return;

        // Only whoever owns the account can take money out of it.
        if (!_mind.TryGetMind(args.Actor, out var mindId, out _) || mindId != account.Owner)
        {
            _popup.PopupEntity(Loc.GetString("bank-card-not-owner"), ent, args.Actor);
            return;
        }

        if (!ChangeBalance(account, -args.Amount, "card withdrawal"))
        {
            _popup.PopupEntity(Loc.GetString("bank-card-insufficient-funds"), ent, args.Actor);
            return;
        }

        foreach (var chip in _stack.SpawnMultipleAtPosition(HolochipStack, args.Amount, Transform(args.Actor).Coordinates))
        {
            _hands.PickupOrDrop(args.Actor, chip);
        }

        UpdateUi(ent);
    }

    private void UpdateUi(Entity<BankCardComponent> ent)
    {
        _ui.SetUiState(ent.Owner, BankCardUiKey.Key, new BankCardBoundUiState(ent.Comp.AccountId, ent.Comp.Balance));
    }
}
