// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Shared._Nebula.Examine;
using Robust.Shared.Configuration;

namespace Content.Client._Nebula.Examine;

public sealed partial class CharacterExamineSystem : SharedCharacterExamineSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;

    protected override bool IsDetailedExamine(EntityUid examiner)
    {
        return _cfg.GetCVar(GoobCVars.DetailedExamine);
    }
}
