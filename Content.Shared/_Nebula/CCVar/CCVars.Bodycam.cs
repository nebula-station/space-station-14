// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{
    public static readonly CVarDef<int> BodycamMaxClipMinutes =
        CVarDef.Create("bodycam.max_clip_minutes", 30, CVar.SERVERONLY);

    public static readonly CVarDef<int> BodycamAdminMaxClipMinutes =
        CVarDef.Create("bodycam.admin_max_clip_minutes", 120, CVar.SERVERONLY);

    public static readonly CVarDef<float> BodycamViewRange =
        CVarDef.Create("bodycam.view_range", 14f, CVar.SERVERONLY);

    public static readonly CVarDef<float> BodycamSampleInterval =
        CVarDef.Create("bodycam.sample_interval", 0.5f, CVar.SERVERONLY);

    public static readonly CVarDef<float> BodycamRequestCooldown =
        CVarDef.Create("bodycam.request_cooldown", 10f, CVar.SERVERONLY);
}
