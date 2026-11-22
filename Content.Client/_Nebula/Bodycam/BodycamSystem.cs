// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Clothing;
using Content.Shared._Nebula.Bodycam;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Client._Nebula.Bodycam;

public sealed partial class BodycamSystem : SharedBodycamSystem
{
    [Dependency] private SharedItemSystem _item = default!;

    private const string MountedLayer = "nebula-bodycam-mounted";
    private const string MountedState = "equipped-NECK";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StorageComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals, after: [typeof(ClientClothingSystem)]);
        SubscribeLocalEvent<BodycamComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<BodycamComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnInserted(Entity<BodycamComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        _item.VisualsChanged(args.Container.Owner);
    }

    private void OnRemoved(Entity<BodycamComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        _item.VisualsChanged(args.Container.Owner);
    }

    private void OnGetEquipmentVisuals(Entity<StorageComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (args.Slot != "belt" || ent.Comp.Container == null)
            return;

        foreach (var contained in ent.Comp.Container.ContainedEntities)
        {
            if (!HasComp<BodycamComponent>(contained)
                || !TryComp<ClothingComponent>(contained, out var clothing)
                || clothing.RsiPath == null)
            {
                continue;
            }

            args.Layers.Add((MountedLayer, new PrototypeLayerData
            {
                RsiPath = clothing.RsiPath,
                State = MountedState,
            }));
            return;
        }
    }
}
