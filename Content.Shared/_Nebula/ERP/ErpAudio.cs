// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.ERP;

/// <summary>
/// Female sounds only for <see cref="Gender.Female"/>, male for everyone else.
/// </summary>
public static class ErpAudio
{
    private static readonly ProtoId<SoundCollectionPrototype> MoansMale = "MoansMale";
    private static readonly ProtoId<SoundCollectionPrototype> MoansFemale = "MoansFemale";
    private static readonly ProtoId<SoundCollectionPrototype> OrgasmMale = "OrgasmMale";
    private static readonly ProtoId<SoundCollectionPrototype> OrgasmFemale = "OrgasmFemale";

    public static ProtoId<SoundCollectionPrototype> GetMoanSounds(Gender gender)
        => gender == Gender.Female ? MoansFemale : MoansMale;

    public static ProtoId<SoundCollectionPrototype> GetOrgasmSounds(Gender gender)
        => gender == Gender.Female ? OrgasmFemale : OrgasmMale;
}
