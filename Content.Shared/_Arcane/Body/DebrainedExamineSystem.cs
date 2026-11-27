// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Shared/Body/DebrainedExamineSystem.cs

using Content.Medical.Common.Body;
using Content.Shared.Examine;

namespace Content.Shared._Arcane.Body;

/// <summary>
/// Shows on examine that a body has no brain (Orion #252).
/// </summary>
public sealed partial class DebrainedExamineSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnExamined(Entity<DebrainedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("comp-brain-examine-debrained", ("entity", ent.Owner)));
    }
}
