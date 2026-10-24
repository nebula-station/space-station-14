// SPDX-License-Identifier: MIT

// Ported from materialyoucolor: https://github.com/T-Dynamos/materialyoucolor-python

namespace Content.Client._Nebula.Stylesheets.Material;

/// <summary>
/// All tones of a single hue and chroma.
/// </summary>
public sealed class TonalPalette
{
    private readonly Dictionary<double, int> _cache = new();

    public double Hue { get; }
    public double Chroma { get; }
    public Hct KeyColor { get; }

    private TonalPalette(double hue, double chroma, Hct keyColor)
    {
        Hue = hue;
        Chroma = chroma;
        KeyColor = keyColor;
    }

    public static TonalPalette FromHct(Hct hct)
    {
        return new TonalPalette(hct.Hue, hct.Chroma, hct);
    }

    public static TonalPalette FromHueAndChroma(double hue, double chroma)
    {
        return new TonalPalette(hue, chroma, CreateKeyColor(hue, chroma));
    }

    public int Tone(double tone)
    {
        if (!_cache.TryGetValue(tone, out var argb))
        {
            argb = Hct.From(Hue, Chroma, tone).Argb;
            _cache[tone] = argb;
        }

        return argb;
    }

    public Hct GetHct(double tone)
    {
        return Hct.FromInt(Tone(tone));
    }

    /// <summary>
    /// Binary searches for the tone closest to 50 that still reaches <paramref name="requestedChroma"/>.
    /// </summary>
    private static Hct CreateKeyColor(double hue, double requestedChroma)
    {
        const int pivotTone = 50;
        const int toneStepSize = 1;
        const double epsilon = 0.01;

        var chromaCache = new Dictionary<int, double>();
        var lowerTone = 0;
        var upperTone = 100;

        double MaxChroma(int tone)
        {
            if (!chromaCache.TryGetValue(tone, out var chroma))
            {
                chroma = Hct.From(hue, 200.0, tone).Chroma;
                chromaCache[tone] = chroma;
            }

            return chroma;
        }

        while (lowerTone < upperTone)
        {
            var midTone = (lowerTone + upperTone) / 2;
            var isAscending = MaxChroma(midTone) < MaxChroma(midTone + toneStepSize);
            var sufficientChroma = MaxChroma(midTone) >= requestedChroma - epsilon;

            if (sufficientChroma)
            {
                if (Math.Abs(lowerTone - pivotTone) < Math.Abs(upperTone - pivotTone))
                {
                    upperTone = midTone;
                }
                else
                {
                    if (lowerTone == midTone)
                        return Hct.From(hue, requestedChroma, lowerTone);

                    lowerTone = midTone;
                }
            }
            else if (isAscending)
            {
                lowerTone = midTone + toneStepSize;
            }
            else
            {
                upperTone = midTone;
            }
        }

        return Hct.From(hue, requestedChroma, lowerTone);
    }
}
