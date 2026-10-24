// SPDX-License-Identifier: MIT

// Ported from materialyoucolor: https://github.com/T-Dynamos/materialyoucolor-python

using static Content.Client._Nebula.Stylesheets.Material.MaterialMath;

namespace Content.Client._Nebula.Stylesheets.Material;

/// <summary>
/// Color in the HCT space: CAM16 hue and chroma with L* tone.
/// </summary>
/// <remarks>
/// Compared by reference on purpose, <see cref="TemperatureCache"/> keys its temperatures by instance like the Python port.
/// </remarks>
public sealed class Hct
{
    public double Hue { get; }
    public double Chroma { get; }
    public double Tone { get; }
    public int Argb { get; }

    private Hct(int argb)
    {
        var (hue, chroma) = Cam16.HueAndChroma(argb);
        Hue = hue;
        Chroma = chroma;
        Tone = LstarFromArgb(argb);
        Argb = argb;
    }

    public static Hct FromInt(int argb)
    {
        return new Hct(argb);
    }

    public static Hct From(double hue, double chroma, double tone)
    {
        return new Hct(HctSolver.SolveToInt(hue, chroma, tone));
    }
}

/// <summary>
/// CAM16 viewing conditions for the default sRGB environment.
/// </summary>
public sealed class ViewingConditions
{
    public static readonly ViewingConditions Default = new();

    public readonly double N;
    public readonly double Aw;
    public readonly double Nbb;
    public readonly double Ncb;
    public readonly double C;
    public readonly double Nc;
    public readonly double[] RgbD;
    public readonly double Fl;
    public readonly double FlRoot;
    public readonly double Z;

    private ViewingConditions()
    {
        var whitePoint = WhitePointD65;
        var adaptingLuminance = 200.0 / Math.PI * YFromLstar(50.0) / 100.0;
        const double backgroundLstar = 50.0;
        const double surround = 2.0;

        var xyz = whitePoint;
        var rW = xyz[0] * 0.401288 + xyz[1] * 0.650173 + xyz[2] * -0.051461;
        var gW = xyz[0] * -0.250268 + xyz[1] * 1.204414 + xyz[2] * 0.045854;
        var bW = xyz[0] * -0.002079 + xyz[1] * 0.048952 + xyz[2] * 0.953127;
        var f = 0.8 + surround / 10.0;
        var c = f >= 0.9
            ? Lerp(0.59, 0.69, (f - 0.9) * 10.0)
            : Lerp(0.525, 0.59, (f - 0.8) * 10.0);
        var d = f * (1.0 - 1.0 / 3.6 * Math.Exp((-adaptingLuminance - 42.0) / 92.0));
        d = d > 1.0 ? 1.0 : d < 0.0 ? 0.0 : d;

        double[] rgbD =
        [
            d * (100.0 / rW) + 1.0 - d,
            d * (100.0 / gW) + 1.0 - d,
            d * (100.0 / bW) + 1.0 - d,
        ];

        var k = 1.0 / (5.0 * adaptingLuminance + 1.0);
        var k4 = Math.Pow(k, 4);
        var k4F = 1.0 - k4;
        var fl = k4 * adaptingLuminance + 0.1 * k4F * k4F * Math.Pow(5.0 * adaptingLuminance, 1.0 / 3.0);
        var n = YFromLstar(backgroundLstar) / whitePoint[1];
        var nbb = 0.725 / Math.Pow(n, 0.2);

        double[] rgbAFactors =
        [
            Math.Pow(fl * rgbD[0] * rW / 100.0, 0.42),
            Math.Pow(fl * rgbD[1] * gW / 100.0, 0.42),
            Math.Pow(fl * rgbD[2] * bW / 100.0, 0.42),
        ];

        double[] rgbA =
        [
            400.0 * rgbAFactors[0] / (rgbAFactors[0] + 27.13),
            400.0 * rgbAFactors[1] / (rgbAFactors[1] + 27.13),
            400.0 * rgbAFactors[2] / (rgbAFactors[2] + 27.13),
        ];

        N = n;
        Aw = (2.0 * rgbA[0] + rgbA[1] + 0.05 * rgbA[2]) * nbb;
        Nbb = nbb;
        Ncb = nbb;
        C = c;
        Nc = f;
        RgbD = rgbD;
        Fl = fl;
        FlRoot = Math.Pow(fl, 0.25);
        Z = 1.48 + Math.Sqrt(n);
    }
}

/// <summary>
/// The part of the CAM16 color appearance model that HCT needs.
/// </summary>
public static class Cam16
{
    public static (double Hue, double Chroma) HueAndChroma(int argb)
    {
        var vc = ViewingConditions.Default;

        var redL = Linearized(RedFromArgb(argb));
        var greenL = Linearized(GreenFromArgb(argb));
        var blueL = Linearized(BlueFromArgb(argb));

        var x = 0.41233895 * redL + 0.35762064 * greenL + 0.18051042 * blueL;
        var y = 0.2126 * redL + 0.7152 * greenL + 0.0722 * blueL;
        var z = 0.01932141 * redL + 0.11916382 * greenL + 0.95034478 * blueL;

        var rC = 0.401288 * x + 0.650173 * y - 0.051461 * z;
        var gC = -0.250268 * x + 1.204414 * y + 0.045854 * z;
        var bC = -0.002079 * x + 0.048952 * y + 0.953127 * z;

        var rD = vc.RgbD[0] * rC;
        var gD = vc.RgbD[1] * gC;
        var bD = vc.RgbD[2] * bC;

        var rAf = Math.Pow(vc.Fl * Math.Abs(rD) / 100.0, 0.42);
        var gAf = Math.Pow(vc.Fl * Math.Abs(gD) / 100.0, 0.42);
        var bAf = Math.Pow(vc.Fl * Math.Abs(bD) / 100.0, 0.42);

        var rA = Signum(rD) * 400.0 * rAf / (rAf + 27.13);
        var gA = Signum(gD) * 400.0 * gAf / (gAf + 27.13);
        var bA = Signum(bD) * 400.0 * bAf / (bAf + 27.13);

        var a = (11.0 * rA + -12.0 * gA + bA) / 11.0;
        var b = (rA + gA - 2.0 * bA) / 9.0;
        var u = (20.0 * rA + 20.0 * gA + 21.0 * bA) / 20.0;
        var p2 = (40.0 * rA + 20.0 * gA + bA) / 20.0;
        var atanDegrees = Math.Atan2(b, a) * 180.0 / Math.PI;
        var hue = atanDegrees < 0 ? atanDegrees + 360.0 : atanDegrees;

        var ac = p2 * vc.Nbb;
        var j = 100.0 * Math.Pow(ac / vc.Aw, vc.C * vc.Z);
        var huePrime = hue < 20.14 ? hue + 360 : hue;
        var eHue = 0.25 * (Math.Cos(huePrime * Math.PI / 180.0 + 2.0) + 3.8);
        var p1 = 50000.0 / 13.0 * eHue * vc.Nc * vc.Ncb;
        var t = p1 * Math.Sqrt(a * a + b * b) / (u + 0.305);
        var alpha = Math.Pow(t, 0.9) * Math.Pow(1.64 - Math.Pow(0.29, vc.N), 0.73);
        var chroma = alpha * Math.Sqrt(j / 100.0);

        return (hue, chroma);
    }
}
