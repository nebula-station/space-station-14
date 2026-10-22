using Robust.Shared.Serialization;

namespace Content.Shared.Inventory;

/// <summary>
///     Defines what slot types an item can fit into.
/// </summary>
[Serializable, NetSerializable]
[Flags]
public enum SlotFlags
{
    NONE = 0,
    PREVENTEQUIP = 1 << 0,
    HEAD = 1 << 1,
    EYES = 1 << 2,
    EARS = 1 << 3,
    MASK = 1 << 4,
    OUTERCLOTHING = 1 << 5,
    INNERCLOTHING = 1 << 6,
    NECK = 1 << 7,
    BACK = 1 << 8,
    BELT = 1 << 9,
    GLOVES = 1 << 10,
    IDCARD = 1 << 11,
    POCKET = 1 << 12,
    LEGS = 1 << 13,
    FEET = 1 << 14,
    SUITSTORAGE = 1 << 15,
    // Lua start
    WALLET = 1 << 13,
    UNDERWEART = 1 << 16,
    UNDERWEARB = 1 << 17,
    SOCKS = 1 << 18,
    // BALACLAVA = 1 << 19, Nebula
    ARMBANDRIGHT = 1 << 20,
    ARMBANDLEFT = 1 << 21,
    // HELMETCOVER = 1 << 22, Nebula
    // HELMETATTACHMENT = 1 << 23, Nebula
    FINGER = 1 << 24,
    // EARRING = 1 << 25, Nebula
    // HAIRPIN = 1 << 26, Nebula
    NECKLACE = 1 << 27,
    // Lua end
    All = ~NONE,

    WITHOUT_POCKET = All & ~POCKET
}
