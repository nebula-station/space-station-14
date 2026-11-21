// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/bluestarwing/monolith-ds/blob/169a1543ef/Content.Shared/_Goobstation/Autosurgeon/AutoSurgeonSystem.cs

using Content.Goobstation.Shared.Autosurgeon;
using Content.Medical.Shared.Body;
using Content.Shared._LuaM.Autosurgeon;
using Content.Shared._LuaM.Cyberware;
using Content.Shared.Body;
using Content.Shared.Implants;
using Robust.Shared.Prototypes;

namespace Content.Server._LuaM.Autosurgeon;

/// <summary>
/// Installs Colossus cyberware from an autosurgeon and tells the user how much humanity they have left.
/// Runs before <see cref="AutoSurgeonSystem"/>, which marks the autosurgeon used.
/// </summary>
public sealed partial class CyberAutoSurgeonSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private SharedSubdermalImplantSystem _implant = default!;

    [SubscribeLocalEvent(before: [typeof(AutoSurgeonSystem)])]
    private void OnDoAfter(Entity<CyberAutoSurgeonComponent> ent, ref AutoSurgeonDoAfterEvent args)
    {
        if (args.Cancelled
            || !TryComp<AutoSurgeonComponent>(ent, out var surgeon)
            || surgeon.Used
            || args.Target is not { } target
            || !HasComp<BodyComponent>(target))
            return;

        var coords = Transform(target).Coordinates;

        if (ent.Comp.NewOrgan is { } organProto
            && _body.GetOrgan(target, ent.Comp.TargetPart) is { } part)
            TryInstall(part, SpawnAtPosition(organProto, coords), replace: true);

        if (ent.Comp.OrganForEachPart is { } eachProto)
        {
            foreach (var each in _part.GetBodyParts(target, ent.Comp.PartType))
            {
                TryInstall(each, SpawnAtPosition(eachProto, coords), replace: false);
            }
        }

        if (ent.Comp.Implant is { } implant)
            _implant.AddImplant(target, implant);

        if (ent.Comp.HumanityCost > 0
            && _body.GetOrgan(target, ent.Comp.TargetPart) is { } upgraded)
        {
            var key = ent.Comp.HumanityKey ?? MetaData(ent).EntityPrototype?.ID ?? string.Empty;
            EnsureComp<CyberwareHumanityComponent>(upgraded).Upgrades[key] = ent.Comp.HumanityCost;
        }

        var ev = new CyberwareInstalledEvent();
        RaiseLocalEvent(target, ref ev);
    }

    /// <summary>
    /// Puts the organ into the part, opening a slot for it if the part has none.
    /// The organ is deleted if it doesn't fit.
    /// </summary>
    private void TryInstall(EntityUid part, EntityUid organ, bool replace)
    {
        if (_body.GetCategory(organ) is not { } category
            || !TryComp<BodyPartComponent>(part, out var partComp))
        {
            Del(organ);
            return;
        }

        var parent = (part, partComp);
        if (!_part.HasOrganSlot(parent, category))
            _part.TryAddSlot(parent, category);

        if (_part.GetOrgan(parent, category) is { } old)
        {
            if (!replace)
            {
                Del(organ);
                return;
            }

            _part.RemoveOrgan(parent, old);
        }

        if (!_part.InsertOrgan(parent, organ))
        {
            Log.Error($"{ToPrettyString(organ)} could not be installed into {ToPrettyString(part)}");
            Del(organ);
        }
    }
}
