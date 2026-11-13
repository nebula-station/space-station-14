using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Cloning.Events;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Toggleable;
using System.Diagnostics.CodeAnalysis; // Nebula
using Content.Shared.Humanoid; // Nebula
using Robust.Shared.Prototypes; // Nebula
using Robust.Shared.Network; // Nebula
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Shared.Wagging;

/// <summary>
/// Adds an action to toggle the wagging animation for tails markings that support it.
/// </summary>
public sealed partial class WaggingSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private INetManager _net = default!; // Nebula

    /// <summary>
    /// Copies the component and its values to the clone.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCloning(Entity<WaggingComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        // Make sure to set the datafields before adding the component so that the correct action gets spawned on map init.
        var cloneComp = Factory.GetComponent<WaggingComponent>();
        cloneComp.Action = ent.Comp.Action;
        cloneComp.Layer = ent.Comp.Layer;
        cloneComp.Organ = ent.Comp.Organ;
        cloneComp.Suffix = ent.Comp.Suffix;
        AddComp(args.CloneUid, cloneComp, true);
    }

    /// <summary>
    /// Adds the wagging action during initialization.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWaggingMapInit(Entity<WaggingComponent> ent, ref MapInitEvent args)
    {
        UpdateWaggingAction(ent); // Nebula - only tails with an animated variant get the action
    }

    // Nebula start
    /// <summary>
    /// Re-checks the wag action once new markings are applied, e.g. from the character profile.
    /// </summary>
    [SubscribeLocalEvent(after: [typeof(BodySystem)])]
    private void OnApplyMarkings(Entity<WaggingComponent> ent, ref ApplyOrganMarkingsEvent args)
    {
        UpdateWaggingAction(ent);
    }

    /// <summary>
    /// Gives the wag action only while the tail has an animated variant. The loadout lets any species
    /// pick any tail, and a tail that can't wag made the button do nothing.
    /// </summary>
    private void UpdateWaggingAction(Entity<WaggingComponent> ent)
    {
        // predicted action entities of networked mobs are spawned by the server
        if (_net.IsClient && !IsClientSide(ent))
            return;

        var canWag = CanWag(ent.Comp, ent.Owner);
        if (canWag == (ent.Comp.ActionEntity != null))
            return;

        if (canWag)
        {
            _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, ent);
        }
        else
        {
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
            ent.Comp.ActionEntity = null;
            SetWaggingFlag(ent, false);
        }

        DirtyField(ent.AsNullable(), nameof(WaggingComponent.ActionEntity));
    }

    /// <summary>
    /// Whether any marking on the wagging layer has an animated variant, or already is one.
    /// </summary>
    private bool CanWag(WaggingComponent comp, EntityUid uid)
    {
        if (!_visualBody.TryGatherMarkingsData(uid, [comp.Layer], out _, out _, out var applied))
            return false;

        foreach (var set in applied.Values)
        {
            if (!set.TryGetValue(comp.Layer, out var markings))
                continue;

            foreach (var marking in markings)
            {
                var id = marking.MarkingId.Id;
                if (IsWaggingVariant(id, comp.Suffix) || ProtoMan.HasIndex<MarkingPrototype>(id + comp.Suffix))
                    return true;
            }
        }

        return false;
    }
    // Nebula end

    /// <summary>
    /// Removes the wagging action during shutdown.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWaggingShutdown(Entity<WaggingComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    /// <summary>
    /// Attempts to toggle the wagging action.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWaggingToggle(Entity<WaggingComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryToggleWagging(ent.AsNullable()))
            return;

        args.Handled = true;
    }

    /// <summary>
    /// Tries to disable wagging when the mob state changes.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<WaggingComponent> ent, ref MobStateChangedEvent args)
    {
        TryToggleWagging(ent.AsNullable(), false);
    }

    /// <summary>
    /// If <see cref="desired"/> provided - attempts to set wagging to it, otherwise tries to toggle wagging.
    /// </summary>
    /// <param name="ent">The entity to toggle.</param>
    /// <param name="desired">The desired wagging state.</param>
    /// <returns>Whether wagging was toggled.</returns>
    [PublicAPI]
    public bool TryToggleWagging(Entity<WaggingComponent?> ent, bool? desired = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!_visualBody.TryGatherMarkingsData(ent.Owner,
                [ent.Comp.Layer],
                out _,
                out _,
                out var applied))
        {
            return false;
        }

        // Nebula start
        // Trauma moved tail markings from the torso to the Tail organ, but the component still points
        // at the torso by default, so wagging did nothing. Use whichever organ actually holds the layer.
        if (!TryGetWaggingOrgan(ent.Comp, applied, out var organ, out var markingsSet))
            return false;

        // The real state is what the tail shows. The flag goes stale when the tail organ is cut off,
        // reattached or regrown by surgery or rejuvenate, which left tails stuck wagging or doubled.
        var currentlyWagging = IsWagging(markingsSet, ent.Comp);
        var wagging = desired ?? !currentlyWagging;

        if (wagging == currentlyWagging)
        {
            SetWaggingFlag((ent.Owner, ent.Comp), currentlyWagging);
            return false;
        }

        var changed = false;

        markingsSet = markingsSet.ShallowClone();
        foreach (var (layers, _) in markingsSet)
        {
            markingsSet[layers] = markingsSet[layers].ShallowClone();
            var layerMarkings = markingsSet[layers];

            for (var i = 0; i < layerMarkings.Count; i++)
            {
                var currentMarkingId = layerMarkings[i].MarkingId.Id;
                var animated = IsWaggingVariant(currentMarkingId, ent.Comp.Suffix);
                string newMarkingId;

                if (wagging && !animated)
                    newMarkingId = $"{currentMarkingId}{ent.Comp.Suffix}";
                else if (!wagging && animated)
                    newMarkingId = currentMarkingId[..^ent.Comp.Suffix.Length];
                else
                    continue; // already in the wanted state, or a second marking without an animated variant

                if (!ProtoMan.HasIndex<MarkingPrototype>(newMarkingId))
                    continue;

                layerMarkings[i] = new Marking(newMarkingId, layerMarkings[i].MarkingColors) { Gradient = layerMarkings[i].Gradient }; // Nebula edit
                changed = true;
            }
        }

        // nothing can wag, keep the old state so the toggle doesn't desync
        if (!changed)
            return false;

        SetWaggingFlag((ent.Owner, ent.Comp), wagging);
        // Nebula end

        _visualBody.ApplyMarkings(ent, new()
        {
            [organ] = markingsSet
        });

        return true;
    }

    /// <summary>
    /// Finds the organ holding the wagging layer: the configured one if it has markings there, otherwise any organ that does.
    /// </summary>
    private static bool TryGetWaggingOrgan(WaggingComponent comp,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> applied,
        out ProtoId<OrganCategoryPrototype> organ,
        [NotNullWhen(true)] out Dictionary<HumanoidVisualLayers, List<Marking>>? markingsSet)
    {
        if (applied.TryGetValue(comp.Organ, out markingsSet) && HasLayerMarkings(markingsSet, comp.Layer))
        {
            organ = comp.Organ;
            return true;
        }

        foreach (var (category, set) in applied)
        {
            if (!HasLayerMarkings(set, comp.Layer))
                continue;

            organ = category;
            markingsSet = set;
            return true;
        }

        organ = comp.Organ;
        markingsSet = null;
        return false;
    }

    private void SetWaggingFlag(Entity<WaggingComponent> ent, bool wagging)
    {
        if (ent.Comp.Wagging == wagging)
            return;

        ent.Comp.Wagging = wagging;
        DirtyField(ent.AsNullable(), nameof(WaggingComponent.Wagging));
    }

    /// <summary>
    /// Whether the marking is an animated variant, i.e. has the suffix and its static marking exists.
    /// </summary>
    private bool IsWaggingVariant(string markingId, string suffix)
    {
        return markingId.Length > suffix.Length
               && markingId.EndsWith(suffix, StringComparison.Ordinal)
               && ProtoMan.HasIndex<MarkingPrototype>(markingId[..^suffix.Length]);
    }

    private bool IsWagging(Dictionary<HumanoidVisualLayers, List<Marking>> set, WaggingComponent comp)
    {
        if (!set.TryGetValue(comp.Layer, out var markings))
            return false;

        foreach (var marking in markings)
        {
            if (IsWaggingVariant(marking.MarkingId.Id, comp.Suffix))
                return true;
        }

        return false;
    }

    private static bool HasLayerMarkings(Dictionary<HumanoidVisualLayers, List<Marking>> set, HumanoidVisualLayers layer)
    {
        return set.TryGetValue(layer, out var markings) && markings.Count > 0;
    }
}
