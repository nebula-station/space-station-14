// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Arcane.Server/Economy/CreditHolochipSystem.cs

using Content.Shared._Arcane.Economy;
using Content.Shared.Emp;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Random;

namespace Content.Server._Arcane.Economy;

/// <summary>
///     Colours holochips by their amount and wipes them on EMP.
/// </summary>
public sealed partial class CreditHolochipSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<CreditHolochipComponent> ent, ref ComponentStartup args)
    {
        UpdateVisuals(ent);
    }

    [SubscribeLocalEvent]
    private void OnStackChanged(Entity<CreditHolochipComponent> ent, ref StackCountChangedEvent args)
    {
        UpdateVisuals(ent);
    }

    [SubscribeLocalEvent]
    private void OnEmpPulse(Entity<CreditHolochipComponent> ent, ref EmpPulseEvent args)
    {
        var chance = Math.Clamp(0.6f / MathF.Max(0.1f, args.EnergyConsumption), 0f, 1f);
        if (!_random.Prob(chance))
            return;

        _popup.PopupEntity(Loc.GetString("credit-holochip-emp-destroyed"), ent, PopupType.LargeCaution);
        QueueDel(ent);
        args.Affected = true;
    }

    private void UpdateVisuals(Entity<CreditHolochipComponent> ent)
    {
        if (!TryComp<StackComponent>(ent, out var stack))
            return;

        var (suffix, shown) = stack.Count switch
        {
            >= 1_000_000_000 => ("_giga", stack.Count / 1_000_000_000f),
            >= 1_000_000 => ("_mega", stack.Count / 1_000_000f),
            >= 1_000 => ("_kilo", stack.Count / 1_000f),
            _ => (string.Empty, stack.Count),
        };

        _appearance.SetData(ent, CreditHolochipVisuals.BaseState, $"holochip{suffix}-color");
        _appearance.SetData(ent, CreditHolochipVisuals.OverlayState, $"holochip{suffix}");
        _appearance.SetData(ent, CreditHolochipVisuals.BaseColor, GetColor((int) MathF.Round(shown)));
    }

    private static Color GetColor(int amount)
    {
        return amount switch
        {
            <= 4 => Color.FromHex("#8E2E38"),
            <= 9 => Color.FromHex("#914792"),
            <= 19 => Color.FromHex("#BF5E0A"),
            <= 49 => Color.FromHex("#358F34"),
            <= 99 => Color.FromHex("#8A8A8A"),
            <= 199 => Color.FromHex("#009D9B"),
            <= 499 => Color.FromHex("#0153C1"),
            _ => Color.FromHex("#2C2C2C"),
        };
    }
}
