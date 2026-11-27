// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Shared/_Arcane/Body/Systems/BrainSystem.cs

using Content.Shared.Body.Components;

namespace Content.Shared.Body.Systems;

public sealed partial class BrainSystem
{
    [Dependency] private MetaDataSystem _metaData = default!;

    /// <summary>
    /// Names the brain after the body it came from.
    /// </summary>
    private void TryRenameBrain(Entity<BrainComponent> ent, EntityUid body)
    {
        if (ent.Comp.Renamed || _timing.ApplyingState || TerminatingOrDeleted(ent) || TerminatingOrDeleted(body))
            return;

        _metaData.SetEntityName(ent, Loc.GetString("comp-brain-name", ("name", Name(ent)), ("player", Name(body))));
        ent.Comp.Renamed = true;
        Dirty(ent);
    }
}
