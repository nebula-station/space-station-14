using Content.Client._Arcane.Flash;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Flash;

public sealed partial class FlashSystem : SharedFlashSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private FlashAfterimageOverlay _overlay = default!; // Nebula edit

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new();
    }

    [SubscribeLocalEvent]
    private void OnApplied(Entity<FlashedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity == args.Target)
        {
            _overlay.Start(); // Nebula edit
            _overlayMan.AddOverlay(_overlay);
        }
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<FlashedStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        if (!_statusEffects.HasEffectComp<FlashedStatusEffectComponent>(args.Target))
        {
            _overlay.Reset(); // Nebula edit
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<FlashedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
    {
        _overlay.Start(captureFrame: false); // Nebula edit
        _overlayMan.AddOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<FlashedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
    {
        if (_player.LocalEntity is { } player && player != args.AppliedTo && _statusEffects.HasEffectComp<FlashedStatusEffectComponent>(player)) // Nebula edit
            return;

        _overlay.Reset(); // Nebula edit
        _overlayMan.RemoveOverlay(_overlay);
    }

    // Nebula start
    public override void Shutdown()
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.Dispose();
        base.Shutdown();
    }
    // Nebula end
}
