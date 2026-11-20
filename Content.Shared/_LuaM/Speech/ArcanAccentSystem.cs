// Ported from Monolith-DS (LuaM) by Nebula.

using Content.Shared.Random.Helpers;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._LuaM.Speech;

public sealed partial class ArcanAccentSystem : RelayAccentSystem<ArcanAccentComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private static readonly string[] Prefixes =
    [
        "luam-accent-arcan-prefix-1",
        "luam-accent-arcan-prefix-2",
        "luam-accent-arcan-prefix-3",
        "luam-accent-arcan-prefix-4",
    ];

    public override string Accentuate(string message, Entity<ArcanAccentComponent>? ent = null)
    {
        var msg = _replacement.ApplyReplacements(message, "luamArcan", ent?.Owner);

        var random = ent.HasValue
            ? SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Value))
            : _random;
        var chance = ent?.Comp.PrefixChance ?? 0.1f;

        if (msg.Length > 0 && random.Prob(chance))
        {
            // No char in the concatenation: Roslyn turns char + string (even char.ToString() + string)
            // into a ReadOnlySpan<char>(ref char) the client sandbox forbids.
            var first = KeepsCapital(msg) ? msg : msg[..1].ToLower() + msg[1..];
            msg = Loc.GetString(random.Pick(Prefixes)) + " " + first;
        }

        return msg;
    }

    /// <summary>
    /// The polite "Вы" forms and all-caps words keep their capital letter after a prefix.
    /// </summary>
    private static bool KeepsCapital(string message)
    {
        var end = 0;
        while (end < message.Length && char.IsLetter(message[end]))
            end++;

        var word = message[..end];
        return word is "Вы" or "Вас" or "Вам" or "Вами" || end > 1 && word.ToUpperInvariant() == word;
    }
}
