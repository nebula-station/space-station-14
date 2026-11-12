// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.ERP;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.ERP;

/// <summary>
/// Draws <see cref="CumOverlayComponent"/> as an extra sprite layer on top of the mob.
/// </summary>
public sealed partial class CumOverlayVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly SpriteSpecifier.Rsi Normal = new(CumOverlayComponent.OverlayRsi, CumOverlayComponent.NormalState);
    private static readonly SpriteSpecifier.Rsi Large = new(CumOverlayComponent.OverlayRsi, CumOverlayComponent.LargeState);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CumOverlayComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CumOverlayComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<CumOverlayComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<CumOverlayComponent> ent, ref ComponentStartup args)
    {
        UpdateLayer(ent);
    }

    private void OnState(Entity<CumOverlayComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateLayer(ent);
    }

    private void OnShutdown(Entity<CumOverlayComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent) || !TryComp<SpriteComponent>(ent, out var spriteComp))
            return;

        Entity<SpriteComponent?> sprite = (ent.Owner, spriteComp);
        if (_sprite.LayerMapTryGet(sprite, CumOverlayLayer.Base, out var layer, false))
            _sprite.LayerSetVisible(sprite, layer, false);
    }

    private void UpdateLayer(Entity<CumOverlayComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var spriteComp))
            return;

        Entity<SpriteComponent?> sprite = (ent.Owner, spriteComp);
        var spec = ent.Comp.Count >= 2 ? Large : Normal;

        // the layer is kept and reused, removing it would shift indices of layers mapped after it
        if (!_sprite.LayerMapTryGet(sprite, CumOverlayLayer.Base, out var layer, false))
        {
            layer = _sprite.AddLayer(sprite, spec);
            _sprite.LayerMapSet(sprite, CumOverlayLayer.Base, layer);
        }
        else
        {
            _sprite.LayerSetSprite(sprite, layer, spec);
        }

        _sprite.LayerSetVisible(sprite, layer, ent.Comp.Count > 0);
    }
}
