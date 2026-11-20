// Ported from Monolith-DS (LuaM) by Nebula.

using Content.Shared.Speech.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._LuaM.Speech;

/// <summary>
/// Languid, flirty Arcan speech: a capitalised "You", drawn-out "mm-h.." and an occasional giggly prefix.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ArcanAccentSystem))]
public sealed partial class ArcanAccentComponent : BaseAccentComponent
{
    /// <summary>
    /// Chance to start the message with one of the prefixes ("Ah..", "Mmm..").
    /// </summary>
    [DataField]
    public float PrefixChance = 0.1f;
}
