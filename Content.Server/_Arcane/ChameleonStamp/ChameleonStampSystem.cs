// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Server/ChameleonStamp/ChameleonStampSystem.cs

using Content.Shared._Arcane.ChameleonStamp;
using Content.Shared.Paper;

namespace Content.Server._Arcane.ChameleonStamp;

public sealed partial class ChameleonStampSystem : SharedChameleonStampSystem
{
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatePresets();
    }

    [SubscribeLocalEvent]
    private void OnApplySettings(Entity<ChameleonStampComponent> entity, ref ChameleonStampApplySettingsMessage args)
    {
        if (!TryComp<StampComponent>(entity, out var stamp))
            return;

        if (!ValidatePreset(args.SelectedStampColorPrototype, out _, out var stampColorComponent)
            || !ValidatePreset(args.SelectedStampStatePrototype, out _, out var stampStateComponent)
            || !ValidatePreset(args.SelectedStampSpritePrototype, out var stampSpritePrototype, out var stampSpriteComponent))
        {
            // Nothing was applied: the resent state makes an open editor drop the rejected draft.
            Dirty(entity);
            return;
        }

        // Color
        entity.Comp.SelectedStampColorPrototype = args.SelectedStampColorPrototype;
        entity.Comp.CustomStampColor = args.CustomStampColor;
        stamp.StampedColor = stampColorComponent.StampedColor == args.CustomStampColor ? stampColorComponent.StampedColor : args.CustomStampColor;

        // StampedName
        stamp.StampedName = args.StampedName is { } stampedName
            ? Truncate(stampedName, MaxNameLength)
            : Loc.GetString(stampSpriteComponent.StampedName);
        // The editor reads the custom name back from here; keep the corrected value, null for the preset.
        entity.Comp.StampedName = args.StampedName != null ? stamp.StampedName : null;

        // Metadata
        if (args.CustomName == null)
            ApplyName(entity, stampSpritePrototype.Name);
        else
            ApplyName(entity, Truncate(args.CustomName, MaxNameLength), true);

        if (args.CustomDescription == null)
            ApplyDescription(entity, stampSpritePrototype.Description);
        else
            ApplyDescription(entity, Truncate(args.CustomDescription, MaxDescriptionLength), true);

        // StampState
        entity.Comp.SelectedStampStatePrototype = args.SelectedStampStatePrototype;
        stamp.StampState = stampStateComponent.StampState;

        // StampSprite
        entity.Comp.SelectedStampSpritePrototype = args.SelectedStampSpritePrototype;

        Dirty(entity);
        Dirty(entity, stamp);

        void ApplyDescription(Entity<ChameleonStampComponent> stampEntity, string value, bool applyToComponent = false)
        {
            _metaData.SetEntityDescription(stampEntity, value);
            stampEntity.Comp.CustomDescription = applyToComponent ? value : null;
        }

        void ApplyName(Entity<ChameleonStampComponent> stampEntity, string value, bool applyToComponent = false)
        {
            _metaData.SetEntityName(stampEntity, value);
            stampEntity.Comp.CustomName = applyToComponent ? value : null;
        }
    }

    private static string Truncate(string text, int length)
    {
        return text.Length > length ? text[..length] : text;
    }
}
