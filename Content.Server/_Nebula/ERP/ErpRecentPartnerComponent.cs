// SPDX-FileCopyrightText: 2026 DefinitelyNotMori
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Nebula.ERP;

/// <summary>
/// The last entity whose ERP panel interaction raised this entity's arousal.
/// </summary>
[RegisterComponent]
public sealed partial class ErpRecentPartnerComponent : Component
{
    [DataField]
    public EntityUid Partner;

    [DataField]
    public TimeSpan Time;
}
