using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Movement.Systems;

/// <summary>
/// Controls the switching of motion and standing still animation.
/// </summary>
public sealed partial class ClientSpriteMovementSystem : SharedSpriteMovementSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;

    protected override void OnSpriteMoveInput(Entity<SpriteMovementComponent> ent, ref SpriteMoveEvent args)
    {
        base.OnSpriteMoveInput(ent, ref args);
        UpdateSprite(ent, args.IsMoving);
    }

    [SubscribeLocalEvent]
    private void OnAfterAutoHandleState(Entity<SpriteMovementComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent, ent.Comp.IsMoving);
    }

    private void UpdateSprite(Entity<SpriteMovementComponent> ent, bool isMoving)
    {
        if (!_spriteQuery.TryGetComponent(ent, out var sprite) || !ent.Comp.IsEnabled) // Nebula edit
            return;

        if (isMoving)
        {
            foreach (var (layer, state) in ent.Comp.MovementLayers)
            {
                if (HasState((ent.Owner, sprite), layer, state)) // Nebula
                    continue;

                _sprite.LayerSetData((ent.Owner, sprite), layer, state);
            }
        }
        else
        {
            foreach (var (layer, state) in ent.Comp.NoMovementLayers)
            {
                if (HasState((ent.Owner, sprite), layer, state)) // Nebula
                    continue;

                _sprite.LayerSetData((ent.Owner, sprite), layer, state);
            }
        }

        // Nebula edit
        var ev = new AfterSpriteMovementUpdateEvent(isMoving);
        RaiseLocalEvent(ent, ref ev);
    }

    // Nebula start

    private bool HasState(Entity<SpriteComponent> sprite, string key, PrototypeLayerData data)
    {
        if (data.State == null || data.RsiPath != null || data.TexturePath != null)
            return false;

        return _sprite.TryGetLayer(sprite.AsNullable(), key, out var layer, false) && layer.State == data.State;
    }
    // Nebula end
}
