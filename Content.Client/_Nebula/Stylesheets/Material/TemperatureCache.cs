// SPDX-License-Identifier: MIT

// Ported from materialyoucolor: https://github.com/T-Dynamos/materialyoucolor-python

using System.Linq;
using static Content.Client._Nebula.Stylesheets.Material.MaterialMath;

namespace Content.Client._Nebula.Stylesheets.Material;

/// <summary>
/// Finds analogous colors by walking the hue wheel in equal steps of perceived warmth.
/// </summary>
public sealed class TemperatureCache
{
    private readonly Hct _input;
    private List<Hct>? _hctsByHue;
    private List<Hct>? _hctsByTemp;
    private Dictionary<Hct, double>? _tempsByHct;

    public TemperatureCache(Hct input)
    {
        _input = input;
    }

    private List<Hct> HctsByHue
    {
        get
        {
            if (_hctsByHue != null)
                return _hctsByHue;

            _hctsByHue = new List<Hct>(361);

            for (var hue = 0; hue <= 360; hue++)
            {
                _hctsByHue.Add(Hct.From(hue, _input.Chroma, _input.Tone));
            }

            return _hctsByHue;
        }
    }

    private Dictionary<Hct, double> TempsByHct
    {
        get
        {
            if (_tempsByHct != null)
                return _tempsByHct;

            _tempsByHct = new Dictionary<Hct, double>();

            foreach (var hct in HctsByHue.Append(_input))
            {
                _tempsByHct[hct] = RawTemperature(hct);
            }

            return _tempsByHct;
        }
    }

    /// <summary>
    /// Sorted coldest to warmest. The sort has to be stable to match Python's.
    /// </summary>
    private List<Hct> HctsByTemp
    {
        get
        {
            if (_hctsByTemp != null)
                return _hctsByTemp;

            var temps = TempsByHct;
            _hctsByTemp = HctsByHue.Append(_input).OrderBy(hct => temps[hct]).ToList();
            return _hctsByTemp;
        }
    }

    private Hct Warmest => HctsByTemp[^1];

    private Hct Coldest => HctsByTemp[0];

    public List<Hct> Analogous(int count, int divisions)
    {
        var startHue = RoundToInt(_input.Hue);
        var startHct = HctsByHue[startHue];
        var lastTemp = RelativeTemperature(startHct);
        var allColors = new List<Hct> { startHct };

        var absoluteTotalTempDelta = 0.0;

        for (var i = 0; i < 360; i++)
        {
            var hue = (int) SanitizeDegrees(startHue + i);
            var temp = RelativeTemperature(HctsByHue[hue]);
            absoluteTotalTempDelta += Math.Abs(temp - lastTemp);
            lastTemp = temp;
        }

        var hueAddend = 1;
        var tempStep = absoluteTotalTempDelta / divisions;
        var totalTempDelta = 0.0;
        lastTemp = RelativeTemperature(startHct);

        while (allColors.Count < divisions)
        {
            var hue = (int) SanitizeDegrees(startHue + hueAddend);
            var hct = HctsByHue[hue];
            var temp = RelativeTemperature(hct);
            totalTempDelta += Math.Abs(temp - lastTemp);

            var desiredTotalTempDeltaForIndex = allColors.Count * tempStep;
            var indexSatisfied = totalTempDelta >= desiredTotalTempDeltaForIndex;
            var indexAddend = 1;

            while (indexSatisfied && allColors.Count < divisions)
            {
                allColors.Add(hct);
                desiredTotalTempDeltaForIndex = (allColors.Count + indexAddend) * tempStep;
                indexSatisfied = totalTempDelta >= desiredTotalTempDeltaForIndex;
                indexAddend++;
            }

            lastTemp = temp;
            hueAddend++;

            if (hueAddend > 360)
            {
                while (allColors.Count < divisions)
                {
                    allColors.Add(hct);
                }

                break;
            }
        }

        var answers = new List<Hct> { _input };
        var increaseHueCount = (int) ((count - 1) / 2.0);

        for (var i = 1; i <= increaseHueCount; i++)
        {
            answers.Insert(0, allColors[WrapIndex(-i, allColors.Count)]);
        }

        var decreaseHueCount = count - increaseHueCount - 1;

        for (var i = 1; i <= decreaseHueCount; i++)
        {
            answers.Add(allColors[WrapIndex(i, allColors.Count)]);
        }

        return answers;
    }

    private static int WrapIndex(int index, int count)
    {
        while (index < 0)
        {
            index += count;
        }

        return index >= count ? index % count : index;
    }

    private double RelativeTemperature(Hct hct)
    {
        var temps = TempsByHct;
        var range = temps[Warmest] - temps[Coldest];
        var differenceFromColdest = temps[hct] - temps[Coldest];

        if (range == 0.0)
            return 0.5;

        return differenceFromColdest / range;
    }

    private static double RawTemperature(Hct color)
    {
        var lab = LabFromArgb(color.Argb);
        var hue = SanitizeDegrees(Math.Atan2(lab[2], lab[1]) * 180.0 / Math.PI);
        var chroma = Math.Sqrt(lab[1] * lab[1] + lab[2] * lab[2]);
        return -0.5 + 0.02 * Math.Pow(chroma, 1.07) * Math.Cos(SanitizeDegrees(hue - 50.0) * Math.PI / 180.0);
    }
}
