// SPDX-License-Identifier: AGPL-3.0-only
// Ported from https://github.com/AdventureTimeSS14/space_station_ADT/blob/296c8b93290412fb8b446c0f42ce107851eefedf/Content.Client/ADT/Posing/PosingSystem.cs

using Content.Shared._ADT.Posing;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._ADT.Posing;

public sealed partial class PosingSystem : SharedPosingSystem
{
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const string PosingContext = "posing";

    private readonly HashSet<EntityUid> _posing = [];

    public override void Initialize()
    {
        base.Initialize();

        var posing = _input.Contexts.New(PosingContext, "common");
        posing.AddFunction(ContentKeyFunctions.TogglePosing);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetUp);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetDown);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetLeft);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetRight);
        posing.AddFunction(ContentKeyFunctions.PosingRotatePositive);
        posing.AddFunction(ContentKeyFunctions.PosingRotateNegative);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _input.Contexts.Remove(PosingContext);
    }

    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<PosingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.Posing)
        {
            _posing.Add(ent);
            return;
        }

        if (_posing.Remove(ent))
            ResetSprite(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<PosingComponent> ent, ref ComponentShutdown args)
    {
        if (!_posing.Remove(ent))
            return;

        if (!TerminatingOrDeleted(ent))
            ResetSprite(ent);

        if (_playerManager.LocalEntity == ent.Owner)
            _input.Contexts.SetActiveContext(ent.Comp.DefaultInputContext);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<PosingComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        if (ent.Comp.Posing && _input.Contexts.ActiveContext.Name == PosingContext)
            _input.Contexts.SetActiveContext(ent.Comp.DefaultInputContext);
    }

    protected override void ClientTogglePosing(EntityUid uid, PosingComponent posing)
    {
        base.ClientTogglePosing(uid, posing);

        if (posing.Posing)
            _posing.Add(uid);
        else if (_posing.Remove(uid))
            ResetSprite((uid, posing));

        if (_playerManager.LocalEntity == uid)
            _input.Contexts.SetActiveContext(posing.Posing ? PosingContext : posing.DefaultInputContext);
    }

    private void ResetSprite(Entity<PosingComponent> ent)
    {
        _sprite.SetOffset(ent.Owner, ent.Comp.DefaultOffset);
        _sprite.SetRotation(ent.Owner, Angle.FromDegrees(ent.Comp.DefaultAngle));
    }

    // Other animations overwrite the sprite offset and rotation, so the pose is reapplied every frame.
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        foreach (var uid in _posing)
        {
            if (!TryComp<PosingComponent>(uid, out var posing))
                continue;

            _sprite.SetOffset(uid, posing.DefaultOffset + posing.CurrentOffset);
            _sprite.SetRotation(uid, Angle.FromDegrees(posing.DefaultAngle) + posing.CurrentAngle);
        }
    }
}
