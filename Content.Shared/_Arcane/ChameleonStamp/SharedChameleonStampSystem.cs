// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Shared/ChameleonStamp/SharedChameleonStampSystem.cs

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Paper;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.ChameleonStamp;

public abstract partial class SharedChameleonStampSystem : EntitySystem
{
    public const int MaxNameLength = 64;
    public const int MaxDescriptionLength = 256;

    [Dependency] protected IPrototypeManager Proto = default!;

    private List<ProtoId<EntityPrototype>> _stampsPresets = [];

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            UpdatePresets();
    }

    protected void UpdatePresets()
    {
        _stampsPresets.Clear();

        var prototypes = Proto.EnumeratePrototypes<EntityPrototype>();

        foreach (var proto in prototypes)
        {
            if (proto.Abstract || proto.HideSpawnMenu || !proto.TryGetComponent<StampComponent>(out _, Factory))
                continue;

            _stampsPresets.Add(proto);
        }
    }

    public bool ValidatePreset(EntProtoId preset, [NotNullWhen(true)] out EntityPrototype? presetPrototype, [NotNullWhen(true)] out StampComponent? presetStampComponent)
    {
        presetPrototype = null;
        presetStampComponent = null;

        if (!_stampsPresets.Contains(preset.Id)
            || !Proto.TryIndex<EntityPrototype>(preset, out presetPrototype)
            || !presetPrototype.TryGetComponent(out presetStampComponent, Factory))
            return false;

        return true;
    }

    public IReadOnlyList<ProtoId<EntityPrototype>> GetAllPresets()
    {
        return _stampsPresets;
    }
}
