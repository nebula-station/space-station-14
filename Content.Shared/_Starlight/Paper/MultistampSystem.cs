// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/TetrisZz/frontier-Radiant/blob/743dfcac03/Content.Shared/_Starlight/Paper/MultistampSystem.cs

using Content.Shared.Interaction.Events;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._Starlight.Paper;

public sealed partial class MultistampSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnInserted(Entity<MultistampComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (!ent.Comp.Stamps.Contains(args.Entity))
            ent.Comp.Stamps.Add(args.Entity);

        Dirty(ent);
        SetMultistamp(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<MultistampComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        ent.Comp.Stamps.Remove(args.Entity);
        ent.Comp.CurrentEntry = ent.Comp.Stamps.Count == 0
            ? 0
            : Math.Clamp(ent.Comp.CurrentEntry, 0, ent.Comp.Stamps.Count - 1);
        Dirty(ent);
        SetMultistamp(ent);
    }

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<MultistampComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || ent.Comp.Stamps.Count == 0)
            return;

        ent.Comp.CurrentEntry = (ent.Comp.CurrentEntry + 1) % ent.Comp.Stamps.Count;
        Dirty(ent);
        args.Handled = true;

        if (!SetMultistamp(ent))
            return;

        _audio.PlayPredicted(ent.Comp.ChangeSound, ent.Owner, args.User);
        var name = Name(ent.Comp.Stamps[ent.Comp.CurrentEntry]);
        _popup.PopupClient(Loc.GetString("multistamp-selected", ("stamp", name)), ent.Owner, args.User);
    }

    private bool SetMultistamp(Entity<MultistampComponent> ent)
    {
        if (ent.Comp.CurrentEntry >= ent.Comp.Stamps.Count
            || !TryComp<StampComponent>(ent, out var stamp)
            || !TryComp<StampComponent>(ent.Comp.Stamps[ent.Comp.CurrentEntry], out var current))
            return false;

        stamp.StampedName = current.StampedName;
        stamp.StampedColor = current.StampedColor;
        stamp.StampState = current.StampState;
        stamp.Sound = current.Sound;
        return true;
    }
}
