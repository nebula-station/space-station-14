// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Rejuvenate;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Nebula.ERP;

/// <summary>
/// Adds, washes and dries <see cref="CumOverlayComponent"/>. All state changes happen on the server,
/// the client only draws the layer.
/// </summary>
public sealed partial class CumOverlaySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    private const int MaxCount = 2;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CumOverlayComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnRejuvenate(Entity<CumOverlayComponent> ent, ref RejuvenateEvent args)
    {
        Wash(ent.Owner);
    }

    public void AddOverlay(EntityUid uid)
    {
        if (_net.IsClient || TerminatingOrDeleted(uid))
            return;

        var overlay = EnsureComp<CumOverlayComponent>(uid);
        overlay.Count = Math.Min(overlay.Count + 1, MaxCount);
        overlay.DriesAt = _timing.CurTime + overlay.DryTime;
        Dirty(uid, overlay);
    }

    /// <summary>
    /// Removes the stain. Safe to call from reactions, events and on entities without the component.
    /// </summary>
    public void Wash(EntityUid uid)
    {
        if (_net.IsClient)
            return;

        RemCompDeferred<CumOverlayComponent>(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CumOverlayComponent>();
        while (query.MoveNext(out var uid, out var overlay))
        {
            if (overlay.DriesAt <= now)
                RemCompDeferred<CumOverlayComponent>(uid);
        }
    }
}

/// <summary>
/// Washes <see cref="CumOverlayComponent"/> off the entity. Used in touch reactions of water and space cleaner.
/// </summary>
public sealed partial class WashCumOverlayEntityEffectSystem : EntityEffectSystem<CumOverlayComponent, WashCumOverlay>
{
    [Dependency] private CumOverlaySystem _overlay = default!;

    protected override void Effect(Entity<CumOverlayComponent> entity, ref EntityEffectEvent<WashCumOverlay> args)
    {
        _overlay.Wash(entity.Owner);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class WashCumOverlay : EntityEffectBase<WashCumOverlay>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-wash-cum-overlay", ("chance", Probability));
}

/// <summary>
/// Deletes the entity it is applied to. Used to wash fluid stains off walls.
/// </summary>
public sealed partial class RemoveCumStainEntityEffectSystem : EntityEffectSystem<CumStainComponent, RemoveCumStain>
{
    [Dependency] private INetManager _net = default!;

    protected override void Effect(Entity<CumStainComponent> entity, ref EntityEffectEvent<RemoveCumStain> args)
    {
        if (_net.IsServer)
            QueueDel(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class RemoveCumStain : EntityEffectBase<RemoveCumStain>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-wash-cum-overlay", ("chance", Probability));
}

/// <summary>
/// Marks a stain on a wall, so <see cref="RemoveCumStain"/> can never delete anything else.
/// </summary>
[RegisterComponent]
public sealed partial class CumStainComponent : Component;

/// <summary>
/// Marks a species that cannot produce fluids, so an orgasm vents sparks instead.
/// </summary>
[RegisterComponent]
public sealed partial class SparkEjaculationComponent : Component;
