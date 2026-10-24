// SPDX-License-Identifier: MIT

// Ported from materialyoucolor: https://github.com/T-Dynamos/materialyoucolor-python

namespace Content.Client._Nebula.Stylesheets.Material;

/// <summary>
/// Number and color space helpers of Material Color Utilities, ported from the materialyoucolor 2.0.10 Python package
/// that end-4's dots-hyprland uses, so generated schemes match it exactly. Colors are ARGB packed into an int.
/// </summary>
public static class MaterialMath
{
    private static readonly double[][] SrgbToXyz =
    [
        [0.41233895, 0.35762064, 0.18051042],
        [0.2126, 0.7152, 0.0722],
        [0.01932141, 0.11916382, 0.95034478],
    ];

    private static readonly double[][] XyzToSrgb =
    [
        [3.2413774792388685, -1.5376652402851851, -0.49885366846268053],
        [-0.9691452513005321, 1.8758853451067872, 0.04156585616912061],
        [0.05562093689691305, -0.20395524564742123, 1.0571799111220335],
    ];

    public static readonly double[] WhitePointD65 = [95.047, 100.0, 108.883];

    public static double Signum(double num)
    {
        if (num < 0)
            return -1.0;

        return num == 0 ? 0.0 : 1.0;
    }

    public static double Lerp(double start, double stop, double amount)
    {
        return (1.0 - amount) * start + amount * stop;
    }

    public static double Clamp(double min, double max, double value)
    {
        if (value < min)
            return min;

        return value > max ? max : value;
    }

    public static double SanitizeDegrees(double degrees)
    {
        degrees %= 360.0;

        if (degrees < 0)
            degrees += 360.0;

        return degrees;
    }

    public static double RotationDirection(double from, double to)
    {
        return SanitizeDegrees(to - from) <= 180.0 ? 1.0 : -1.0;
    }

    public static double DifferenceDegrees(double a, double b)
    {
        return 180.0 - Math.Abs(Math.Abs(a - b) - 180.0);
    }

    public static double[] MatrixMultiply(double[] row, double[][] matrix)
    {
        return
        [
            row[0] * matrix[0][0] + row[1] * matrix[0][1] + row[2] * matrix[0][2],
            row[0] * matrix[1][0] + row[1] * matrix[1][1] + row[2] * matrix[1][2],
            row[0] * matrix[2][0] + row[1] * matrix[2][1] + row[2] * matrix[2][2],
        ];
    }

    /// <summary>
    /// Python's <c>round</c> rounds halves to even, same as the default <see cref="Math.Round(double)"/>.
    /// </summary>
    public static int RoundToInt(double value)
    {
        return (int) Math.Round(value);
    }

    public static int ArgbFromRgb(int red, int green, int blue)
    {
        return unchecked((int) 0xFF000000) | (red & 255) << 16 | (green & 255) << 8 | blue & 255;
    }

    public static int RedFromArgb(int argb)
    {
        return (argb >> 16) & 255;
    }

    public static int GreenFromArgb(int argb)
    {
        return (argb >> 8) & 255;
    }

    public static int BlueFromArgb(int argb)
    {
        return argb & 255;
    }

    public static int ArgbFromLinrgb(double[] linrgb)
    {
        return ArgbFromRgb(Delinearized(linrgb[0]), Delinearized(linrgb[1]), Delinearized(linrgb[2]));
    }

    public static int ArgbFromXyz(double x, double y, double z)
    {
        var m = XyzToSrgb;
        var r = m[0][0] * x + m[0][1] * y + m[0][2] * z;
        var g = m[1][0] * x + m[1][1] * y + m[1][2] * z;
        var b = m[2][0] * x + m[2][1] * y + m[2][2] * z;
        return ArgbFromRgb(Delinearized(r), Delinearized(g), Delinearized(b));
    }

    public static double[] XyzFromArgb(int argb)
    {
        double[] rgb =
        [
            Linearized(RedFromArgb(argb)),
            Linearized(GreenFromArgb(argb)),
            Linearized(BlueFromArgb(argb)),
        ];

        return MatrixMultiply(rgb, SrgbToXyz);
    }

    public static double[] LabFromArgb(int argb)
    {
        var xyz = XyzFromArgb(argb);
        var fx = LabF(xyz[0] / WhitePointD65[0]);
        var fy = LabF(xyz[1] / WhitePointD65[1]);
        var fz = LabF(xyz[2] / WhitePointD65[2]);
        return [116.0 * fy - 16.0, 500.0 * (fx - fy), 200.0 * (fy - fz)];
    }

    public static int ArgbFromLstar(double lstar)
    {
        var component = Delinearized(YFromLstar(lstar));
        return ArgbFromRgb(component, component, component);
    }

    public static double LstarFromArgb(int argb)
    {
        return 116.0 * LabF(XyzFromArgb(argb)[1] / 100.0) - 16.0;
    }

    public static double YFromLstar(double lstar)
    {
        return 100.0 * LabInvF((lstar + 16.0) / 116.0);
    }

    public static double LstarFromY(double y)
    {
        return LabF(y / 100.0) * 116.0 - 16.0;
    }

    public static double Linearized(int component)
    {
        var normalized = component / 255.0;

        if (normalized <= 0.040449936)
            return normalized / 12.92 * 100.0;

        return Math.Pow((normalized + 0.055) / 1.055, 2.4) * 100.0;
    }

    public static int Delinearized(double component)
    {
        var normalized = component / 100.0;
        var delinearized = normalized <= 0.0031308
            ? normalized * 12.92
            : 1.055 * Math.Pow(normalized, 1.0 / 2.4) - 0.055;

        return Math.Clamp(RoundToInt(delinearized * 255), 0, 255);
    }

    private static double LabF(double t)
    {
        const double e = 216.0 / 24389.0;
        const double kappa = 24389.0 / 27.0;

        if (t > e)
            return Math.Pow(t, 1.0 / 3.0);

        return (kappa * t + 16) / 116;
    }

    private static double LabInvF(double ft)
    {
        const double e = 216.0 / 24389.0;
        const double kappa = 24389.0 / 27.0;
        var ft3 = ft * ft * ft;

        if (ft3 > e)
            return ft3;

        return (116 * ft - 16) / kappa;
    }
}
