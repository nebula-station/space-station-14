// Ported from Monolith-DS (LuaM) by Nebula.

using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._LuaM.Body;

/// <summary>
/// Drives <see cref="BloodlossOverlay"/> from the local player's blood level.
/// Nebula: Monolith sends the intensity from the server, here the bloodstream and its solution are
/// networked, so the client reads the blood level itself.
/// </summary>
public sealed partial class BloodlossOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    private const float LerpRate = 2f;

    private static readonly Color DefaultBloodColor = Color.FromHex("#800000");

    private BloodlossOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new BloodlossOverlay();
        _overlayManager.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayManager.RemoveOverlay(_overlay);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var target = 0f;
        if (_player.LocalEntity is { } local && TryComp<BloodstreamComponent>(local, out var bloodstream))
        {
            target = GetIntensity((local, bloodstream));
            _overlay.BloodColor = GetBloodColor(bloodstream);
        }

        var current = _overlay.CurrentIntensity;
        current += (target - current) * Math.Clamp(LerpRate * frameTime, 0f, 1f);
        _overlay.CurrentIntensity = current < 0.005f ? 0f : current;
    }

    /// <summary>
    /// 0 at the bloodloss threshold and above, 1 with no blood left.
    /// </summary>
    private float GetIntensity(Entity<BloodstreamComponent> ent)
    {
        if (_mobState.IsDead(ent) || ent.Comp.BloodlossThreshold <= 0f)
            return 0f;

        // GetBloodLevel reads 0 while the blood solution hasn't reached the client yet, that is not an empty bloodstream.
        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, logMissing: false))
            return 0f;

        var level = _bloodstream.GetBloodLevel(ent.AsNullable());
        return Math.Clamp(1f - level / ent.Comp.BloodlossThreshold, 0f, 1f);
    }

    private Color GetBloodColor(BloodstreamComponent bloodstream)
    {
        foreach (var (reagent, _) in bloodstream.BloodReferenceSolution.Contents)
        {
            if (_prototype.TryIndex<ReagentPrototype>(reagent.Prototype, out var proto))
                return proto.SubstanceColor;
        }

        return DefaultBloodColor;
    }
}
