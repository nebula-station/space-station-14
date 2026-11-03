// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs;

namespace Content.Client._Nebula.Hud;

/// <summary>
/// Kinds of the security module's HUD tasks, top to bottom.
/// </summary>
public enum SecurityHudTaskKind : byte
{
    Implant,
    Loss,
    Timer,
    Mark,
    Order,
    Personal,
    Wanted,
}

/// <summary>
/// Builds the HUD lines of the security module: its tasks and the squad section.
/// </summary>
public static class SecurityHud
{
    public const string Source = "security";

    /// <summary>
    /// The squad of a member, or the summary of all squads for the head of security.
    /// </summary>
    public const string SquadSection = "squad";

    /// <summary>
    /// Losses and timers come and go on their own, apart from the tasks sent by the server.
    /// </summary>
    public const string AlertSource = "security-alerts";

    /// <summary>
    /// Crits reported by security tracking implants, with their markers.
    /// </summary>
    public const string ImplantSource = "security-implant";

    public static SkewedHudLine Member(string name, MobState state)
    {
        return new SkewedHudLine
        {
            Label = Loc.GetString("skewed-hud-member"),
            Value = name,
            Color = SkewedHudSystem.StateColor(state),
            Lags = true,
        };
    }

    public static SkewedHudLine Summary(string name, Color color, int alive, int total, bool losses)
    {
        return new SkewedHudLine
        {
            Label = Loc.GetString("skewed-hud-squad"),
            Value = Loc.GetString("skewed-hud-squad-summary", ("name", name), ("alive", alive), ("total", total)),
            Color = losses ? SkewedHudSystem.StateColor(MobState.Critical) : color,
            LabelColor = color.WithAlpha(0.65f),
            Lags = true,
        };
    }

    public static SkewedHudTask Task(SecurityHudTaskKind kind, string text)
    {
        return new SkewedHudTask
        {
            Source = kind switch
            {
                SecurityHudTaskKind.Implant => ImplantSource,
                SecurityHudTaskKind.Loss or SecurityHudTaskKind.Timer => AlertSource,
                _ => Source,
            },
            Label = Loc.GetString(kind switch
            {
                SecurityHudTaskKind.Implant => "skewed-hud-task-implant",
                SecurityHudTaskKind.Loss => "skewed-hud-task-loss",
                SecurityHudTaskKind.Timer => "skewed-hud-task-timer",
                SecurityHudTaskKind.Mark => "skewed-hud-task-mark",
                SecurityHudTaskKind.Order => "skewed-hud-task-order",
                SecurityHudTaskKind.Personal => "skewed-hud-task-personal",
                _ => "skewed-hud-task-wanted",
            }),
            Text = text,
            Order = (int) kind,
            Urgent = kind is SecurityHudTaskKind.Implant
                or SecurityHudTaskKind.Loss
                or SecurityHudTaskKind.Timer
                or SecurityHudTaskKind.Mark,
        };
    }
}
