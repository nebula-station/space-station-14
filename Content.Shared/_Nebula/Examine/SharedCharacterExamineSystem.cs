// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Trauma.Common.Heretic;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.Examine;

/// <summary>
/// Adds worn clothing to the examine text of a character.
/// Runs on the client too, so the tooltip does not grow when the server response arrives.
/// </summary>
public abstract partial class SharedCharacterExamineSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;

    private const int SlotsPriority = 13;

    private static readonly (string Slot, string Loc)[] Slots =
    [
        ("head", "head-examine"),
        ("eyes", "eyes-examine"),
        ("mask", "mask-examine"),
        ("neck", "neck-examine"),
        ("ears", "ears-examine"),
        ("jumpsuit", "jumpsuit-examine"),
        ("outerClothing", "outer-examine"),
        ("back", "back-examine"),
        ("gloves", "gloves-examine"),
        ("belt", "belt-examine"),
        ("id", "id-examine"),
        ("shoes", "shoes-examine"),
        ("suitstorage", "suitstorage-examine"),
    ];

    [SubscribeLocalEvent]
    private void OnExamined(Entity<ExaminableCharacterComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var selfaware = args.Examiner == args.Examined;
        var suffix = selfaware ? "-selfaware" : string.Empty;
        var detailed = IsDetailedExamine(args.Examiner);
        List<WornItem> worn = [];
        var priority = SlotsPriority;

        foreach (var (slot, loc) in Slots)
        {
            if (!_inventory.TryGetSlotEntity(ent.Owner, slot, out var item)
                || HasComp<StripMenuInvisibleComponent>(item))
                continue;

            var wornItem = new WornItem(loc + suffix, FormattedMessage.EscapeText(Name(item.Value)), item.Value);
            worn.Add(wornItem);

            if (detailed)
            {
                var line = Loc.GetString(wornItem.Loc, ("item", wornItem.Name), ("ent", ent.Owner), ("id", "empty"));
                args.PushMarkup($"[font size=10]{line}[/font]", priority);
            }

            priority--;
        }

        var cansee = Loc.GetString("examine-can-see" + suffix, ("ent", ent.Owner));
        if (detailed && worn.Count > 0)
            args.PushMarkup($"[font size=10]{cansee}[/font]", SlotsPriority + 1);

        OnCharacterExamined(ent, args, selfaware, cansee, worn);
    }

    /// <summary>
    /// Whether the examiner has detailed examine enabled in their settings.
    /// </summary>
    protected abstract bool IsDetailedExamine(EntityUid examiner);

    /// <summary>
    /// Called after the worn clothing was added to the examine text.
    /// </summary>
    protected virtual void OnCharacterExamined(Entity<ExaminableCharacterComponent> ent,
        ExaminedEvent args,
        bool selfaware,
        string cansee,
        List<WornItem> worn)
    {
    }
}

/// <summary>
/// A visible clothing item with its examine locale key and escaped name.
/// </summary>
public readonly record struct WornItem(string Loc, string Name, EntityUid Item);
