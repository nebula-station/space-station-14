// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/81cd66873d/Content.Server/_DV/Feroxi/FeroxiDehydrateSystem.cs

using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Metabolism;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Server._DV.Feroxi;

public sealed partial class FeroxiDehydrateSystem : EntitySystem
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(2);

    [Dependency] private BodySystem _body = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SatiationSystem _satiation = default!;

    private TimeSpan _nextCheck;

    public override void Update(float frameTime)
    {
        if (_timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + CheckInterval;

        var query = EntityQueryEnumerator<FeroxiDehydrateComponent, SatiationComponent>();
        while (query.MoveNext(out var uid, out var comp, out var satiation))
        {
            if (_satiation.GetValueOrNull((uid, satiation), SatiationSystem.Thirst) is not { } thirst)
                continue;

            var dehydrated = thirst <= comp.DehydrationThreshold;
            if (comp.Dehydrated != dehydrated)
                UpdateDehydration((uid, comp), dehydrated);
        }
    }

    private void UpdateDehydration(Entity<FeroxiDehydrateComponent> ent, bool dehydrated)
    {
        ent.Comp.Dehydrated = dehydrated;

        var metabolizerType = dehydrated ? ent.Comp.DehydratedMetabolizer : ent.Comp.HydratedMetabolizer;
        foreach (var lung in _body.GetOrgans<LungComponent>(ent.Owner))
        {
            if (!TryComp<MetabolizerComponent>(lung, out var metabolizer) || metabolizer.MetabolizerTypes == null)
                continue;

            metabolizer.MetabolizerTypes.Clear();
            metabolizer.MetabolizerTypes.Add(metabolizerType);
            Dirty(lung, metabolizer);
        }
    }
}
