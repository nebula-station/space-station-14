// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Client/ChameleonStamp/ChameleonStampSystem.cs

using Content.Shared._Arcane.ChameleonStamp;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Client._Arcane.ChameleonStamp;

public sealed partial class ChameleonStampSystem : SharedChameleonStampSystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatePresets();
    }

    [SubscribeLocalEvent]
    private void HandleState(Entity<ChameleonStampComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(entity);

        if (_ui.TryGetOpenUi<ChameleonStampUserInterface>(entity.Owner, ChameleonStampUiKey.Key, out var bui))
            bui.Update();
    }

    public void UpdateVisuals(Entity<ChameleonStampComponent> entity)
    {
        if (!TryComp<SpriteComponent>(entity, out var sprite)
            || !Proto.TryIndex<EntityPrototype>(entity.Comp.SelectedStampSpritePrototype, out var proto)
            || !proto.TryGetComponent<SpriteComponent>(out var presetSprite, Factory))
            return;

        sprite.CopyFrom(presetSprite);
    }
}
