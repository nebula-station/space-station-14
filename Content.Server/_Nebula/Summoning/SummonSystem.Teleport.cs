// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Nebula.InfinityDorms.Components;
using Content.Shared._Nebula.PointTether;
using Content.Shared._Nebula.Summoning;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Buckle.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Spawners.Components;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Map;

namespace Content.Server._Nebula.Summoning;

public sealed partial class SummonSystem
{
    private static readonly TimeSpan LeashCheckInterval = TimeSpan.FromSeconds(1);
    private static readonly string[] DroppedSlots = ["back", "belt"];

    #region Summoned events

    [SubscribeLocalEvent]
    private void OnSummonedMobStateChanged(Entity<SummonedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            Return(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnSummonedTerminating(Entity<SummonedComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!TryComp<SummonRuneComponent>(ent.Comp.Rune, out var rune) || rune.Summoned != ent.Owner)
            return;

        rune.Summoned = null;
        QueueDel(ent.Comp.Rune);
    }

    [SubscribeLocalEvent]
    private void OnSummonedTetherExceeded(Entity<SummonedComponent> ent, ref PointTetherExceededEvent args)
    {
        Teleport(ent.Owner, Transform(ent.Comp.Rune).Coordinates);
    }

    #endregion

    #region Summon and return

    private void Summon(Entity<SummonRuneComponent> rune, EntityUid summoner, EntityUid target)
    {
        var xform = Transform(target);
        var summoned = EnsureComp<SummonedComponent>(target);
        summoned.Rune = rune.Owner;
        summoned.ReturnCoordinates = xform.Coordinates;
        summoned.ReturnStation = _station.GetOwningStation(target, xform);
        _tether.SetTether(target, rune.Owner, rune.Comp.LeashRange, LeashCheckInterval);
        rune.Comp.Summoned = target;

        foreach (var slot in DroppedSlots)
        {
            _inventory.TryUnequip(target, slot, silent: true, force: true);
        }

        foreach (var item in _inventory.GetHandOrInventoryEntities(target))
        {
            MarkItem(item, target);
        }

        Teleport(target, Transform(rune).Coordinates);

        if (!TerminatingOrDeleted(summoner))
        {
            var chant = Loc.GetString($"summon-chant-{_random.Next(1, ChantCount + 1)}");
            _chat.TrySendInGameICMessage(summoner, chant, InGameICChatType.Speak, false, ignoreActionBlocker: true);
        }

        _popup.PopupEntity(Loc.GetString("summon-popup-summoned"), target, target, PopupType.Medium);
        _adminLog.Add(LogType.Teleport, LogImpact.Medium,
            $"{ToPrettyString(summoner):user} summoned {ToPrettyString(target):target} via {ToPrettyString(rune):rune}");
    }

    /// <summary>
    /// Sends a summoned entity back to where it was summoned from.
    /// Items it did not bring with it are left on the rune.
    /// </summary>
    /// <param name="deletedRune">Rune that is being deleted and should not be deleted again.</param>
    private void Return(EntityUid target, EntityUid? deletedRune = null)
    {
        if (!TryComp<SummonedComponent>(target, out var summoned))
            return;

        var rune = summoned.Rune;
        if (TryComp<SummonRuneComponent>(rune, out var runeComp) && runeComp.Summoned == target)
            runeComp.Summoned = null;

        var runeCoords = Exists(rune) ? Transform(rune).Coordinates : Transform(target).Coordinates;
        foreach (var item in _inventory.GetHandOrInventoryEntities(target).ToList())
        {
            ReturnItem(item, target, runeCoords);
        }

        var marked = EntityQueryEnumerator<SummonedItemComponent>();
        while (marked.MoveNext(out var item, out var mark))
        {
            if (mark.Summoned == target)
                RemCompDeferred(item, mark);
        }

        if (TryGetReturnCoordinates(summoned, out var coords))
            Teleport(target, coords);

        RemComp<SummonedComponent>(target);
        RemComp<PointTetherComponent>(target);

        _adminLog.Add(LogType.Teleport, LogImpact.Low, $"{ToPrettyString(target):target} returned from summoning");

        var runeGrid = Exists(rune) ? Transform(rune).GridUid : null;
        if (rune != deletedRune && !TerminatingOrDeleted(rune))
            QueueDel(rune);

        if (runeGrid != null && HasComp<InfinityDormComponent>(runeGrid))
            _dorm.QueueDeletionCheck(runeGrid.Value);
    }

    private void Teleport(EntityUid uid, EntityCoordinates coords)
    {
        var from = Transform(uid).Coordinates;

        _container.TryRemoveFromContainer(uid, true);
        _pulling.StopAllPulls(uid);
        if (TryComp<BuckleComponent>(uid, out var buckle))
            _buckle.Unbuckle((uid, buckle), null);

        _transform.SetCoordinates(uid, coords);
        _transform.AttachToGridOrMap(uid);

        _sparks.DoSparks(from, playSound: false);
        _sparks.DoSparks(coords, playSound: false);
        _audio.PlayPvs(SummonSound, from);
        _audio.PlayPvs(SummonSound, coords);
    }

    private bool TryGetReturnCoordinates(SummonedComponent summoned, out EntityCoordinates coords)
    {
        coords = summoned.ReturnCoordinates;
        if (!TerminatingOrDeleted(coords.EntityId) && !TerminatingOrDeleted(_transform.GetMap(coords)))
            return true;

        List<EntityCoordinates> fallback = [];
        List<EntityCoordinates> any = [];
        var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spawn, out var xform))
        {
            if (spawn.SpawnType != SpawnPointType.LateJoin || TerminatingOrDeleted(xform.MapUid))
                continue;

            var station = _station.GetOwningStation(uid, xform);
            if (station == null)
                continue;

            any.Add(xform.Coordinates);
            if (station == summoned.ReturnStation)
                fallback.Add(xform.Coordinates);
        }

        if (fallback.Count == 0)
            fallback = any;

        if (fallback.Count == 0)
            return false;

        coords = _random.Pick(fallback);
        return true;
    }

    #endregion

    #region Helpers

    private void MarkItem(EntityUid item, EntityUid summoned)
    {
        EnsureComp<SummonedItemComponent>(item).Summoned = summoned;

        if (!TryComp<ContainerManagerComponent>(item, out var manager))
            return;

        foreach (var container in _container.GetAllContainers(item, manager))
        {
            foreach (var contained in container.ContainedEntities)
            {
                MarkItem(contained, summoned);
            }
        }
    }

    private void ReturnItem(EntityUid item, EntityUid summoned, EntityCoordinates runeCoords)
    {
        if (!TryComp<SummonedItemComponent>(item, out var mark) || mark.Summoned != summoned)
        {
            _container.TryRemoveFromContainer(item, true);
            _transform.SetCoordinates(item, runeCoords);
            _transform.AttachToGridOrMap(item);
            return;
        }

        if (!TryComp<ContainerManagerComponent>(item, out var manager))
            return;

        foreach (var container in _container.GetAllContainers(item, manager))
        {
            foreach (var contained in container.ContainedEntities.ToList())
            {
                ReturnItem(contained, summoned, runeCoords);
            }
        }
    }

    #endregion
}
