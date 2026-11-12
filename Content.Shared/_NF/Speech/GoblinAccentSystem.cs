// Ported from Frontier (sector-frontier-14) by Nebula, moved to the shared relay accent system.

using System.Text.RegularExpressions;
using Content.Shared.Speech.EntitySystems;

namespace Content.Shared._NF.Speech;

/// <summary>
/// Goblin speech: "the" becomes "da", dropped g's and h's and so on. Based on the southern accent.
/// </summary>
public sealed partial class GoblinAccentSystem : RelayAccentSystem<GoblinAccentComponent>
{
    private static readonly Regex RegexIng = new(@"(in)g\b", RegexOptions.IgnoreCase);
    private static readonly Regex RegexAnd = new(@"\b(an)d\b", RegexOptions.IgnoreCase);
    private static readonly Regex RegexEr = new(@"([^\WpPfF])er\b"); // keeps "er", "per" and "fer"
    private static readonly Regex RegexErUpper = new(@"([^\WpPfF])ER\b");
    private static readonly Regex RegexTwoLetterEr = new(@"(\w\w)er\b"); // "super" -> "supah"
    private static readonly Regex RegexTwoLetterErUpper = new(@"(\w\w)ER\b");
    private static readonly Regex RegexErs = new(@"(\w)ers\b"); // "fixers" -> "fixas"
    private static readonly Regex RegexErsUpper = new(@"(\w)ERS\b");
    private static readonly Regex RegexTt = new(@"([aeiouy])tt", RegexOptions.IgnoreCase);
    private static readonly Regex RegexOf = new(@"\b(o)f\b", RegexOptions.IgnoreCase);
    private static readonly Regex RegexThe = new(@"\bthe\b");
    private static readonly Regex RegexTheUpper = new(@"\bTHE\b");
    private static readonly Regex RegexH = new(@"\bh", RegexOptions.IgnoreCase);
    private static readonly Regex RegexSelf = new(@"self\b");
    private static readonly Regex RegexSelfUpper = new(@"SELF\b");

    [Dependency] private ReplacementAccentSystem _replacement = default!;

    public override string Accentuate(string message, Entity<GoblinAccentComponent>? ent = null)
    {
        message = _replacement.ApplyReplacements(message, "goblin_accent", ent?.Owner);

        message = RegexIng.Replace(message, "$1'");
        message = RegexAnd.Replace(message, "$1'");
        message = RegexEr.Replace(message, "$1ah");
        message = RegexErUpper.Replace(message, "$1AH");
        message = RegexTwoLetterEr.Replace(message, "$1ah");
        message = RegexTwoLetterErUpper.Replace(message, "$1AH");
        message = RegexErs.Replace(message, "$1as");
        message = RegexErsUpper.Replace(message, "$1AS");
        message = RegexTt.Replace(message, "$1'");
        message = RegexH.Replace(message, "'");
        message = RegexSelf.Replace(message, "sewf");
        message = RegexSelfUpper.Replace(message, "SEWF");
        message = RegexOf.Replace(message, "$1'");
        message = RegexThe.Replace(message, "da");
        message = RegexTheUpper.Replace(message, "DA");
        return message;
    }
}
