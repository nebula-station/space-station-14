// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from https://github.com/Goob-Station/Goob-Station/blob/707e36693d1d9289a4f04921c5e070b1e4195589/Content.Shared/_ES/Camera/ESScreenshakeSystem.cs

using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Noise;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Camera;

/// <summary>
///     Handles sending rotational or translational screenshake to an entity, managing the screenshake commands
///     of every entity currently screenshaking, and setting offset/rotation when updated
/// </summary>
public sealed partial class ESScreenshakeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private INetManager _net = default!;

    private static readonly Vector2 MaxOffset = new(0.15f, 0.15f);
    private const float MaxAngleDegrees = 20f;

    private readonly FastNoiseLite _noiseX = new(67);
    private readonly FastNoiseLite _noiseY = new(68);
    private readonly FastNoiseLite _noiseRotation = new(67 + 420); // Epic bacon

    private bool _reducedMotion;
    private float _intensity = 1f;

    private TimeSpan _now;
    private Predicate<ESScreenshakeCommand> _isExpired = default!;

    #region Internal

    public override void Initialize()
    {
        base.Initialize();

        _noiseX.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _noiseY.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _noiseRotation.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _isExpired = command => _now >= command.CalculatedEnd;

        if (!_net.IsClient)
            return;

        Subs.CVar(_cfg, CCVars.ReducedMotion, v => _reducedMotion = v, true);
        Subs.CVar(_cfg, CCVars.ScreenShakeIntensity, v => _intensity = v, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _now = _timing.CurTime;
        var shakers = EntityQueryEnumerator<ESScreenshakeComponent>();
        while (shakers.MoveNext(out var ent, out var shake))
        {
            if (shake.Commands.Count == 0)
            {
                RemCompDeferred<ESScreenshakeComponent>(ent);
                continue;
            }

            if (shake.Commands.RemoveWhere(_isExpired) > 0)
                Dirty(ent, shake);
        }
    }

    [SubscribeLocalEvent]
    private void OnGetEyeOffset(Entity<ESScreenshakeComponent> ent, ref GetEyeOffsetEvent args)
    {
        if (_reducedMotion || _intensity <= 0f)
            return;

        var time = (float) _timing.RealTime.TotalMilliseconds;
        var accumulatedOffset = Vector2.Zero;
        foreach (var command in ent.Comp.Commands)
        {
            if (command.Translational == null)
                continue;

            var trauma = CalculateTraumaValueForCurrentTime(command.Translational, command.Start);
            if (trauma <= 0)
                continue;

            _noiseX.SetFrequency(command.Translational.Frequency);
            _noiseY.SetFrequency(command.Translational.Frequency);

            var start = (float) command.Start.TotalMilliseconds;
            var offsetX = MaxOffset.X * trauma * _noiseX.GetNoise(time, start);
            var offsetY = MaxOffset.Y * trauma * _noiseY.GetNoise(time, start);
            accumulatedOffset += new Vector2(offsetX, offsetY);
        }

        args.Offset += accumulatedOffset * _intensity;
    }

    [SubscribeLocalEvent]
    private void OnGetEyeRotation(Entity<ESScreenshakeComponent> ent, ref ESGetEyeRotationEvent args)
    {
        if (_reducedMotion || _intensity <= 0f)
            return;

        var time = (float) _timing.RealTime.TotalMilliseconds;
        var accumulatedAngle = Angle.Zero;
        foreach (var command in ent.Comp.Commands)
        {
            if (command.Rotational == null)
                continue;

            var trauma = CalculateTraumaValueForCurrentTime(command.Rotational, command.Start);
            if (trauma <= 0)
                continue;

            _noiseRotation.SetFrequency(command.Rotational.Frequency);

            var angle = MaxAngleDegrees * trauma * _noiseRotation.GetNoise(time, (float) command.Start.TotalMilliseconds);
            accumulatedAngle += Angle.FromDegrees(angle);
        }

        args.Rotation += accumulatedAngle * _intensity;
    }

    [SubscribeLocalEvent]
    private void OnEntityUnpaused(Entity<ESScreenshakeComponent> ent, ref EntityUnpausedEvent args)
    {
        // rebuild screenshake commands but with offset times
        HashSet<ESScreenshakeCommand> newSet = [];
        foreach (var command in ent.Comp.Commands)
        {
            newSet.Add(command with
            {
                CalculatedEnd = command.CalculatedEnd + args.PausedTime,
                Start = command.Start + args.PausedTime,
            });
        }

        ent.Comp.Commands = newSet;
        Dirty(ent);
    }

    /// <summary>
    ///     Calculates when both traumas will be at least = 0 given the decay rate and start time.
    /// </summary>
    private static TimeSpan CalculateEndTimeForCommand(ESScreenshakeParameters? translation, ESScreenshakeParameters? rotation, TimeSpan start)
    {
        // https://www.desmos.com/calculator/optip8eucx
        var secsUntilRotationalEnd = rotation != null ? MathF.Sqrt(rotation.Trauma / rotation.DecayRate) : 0f;
        var secsUntilTranslationalEnd = translation != null ? MathF.Sqrt(translation.Trauma / translation.DecayRate) : 0f;
        return start + TimeSpan.FromSeconds(MathF.Max(secsUntilTranslationalEnd, secsUntilRotationalEnd));
    }

    /// <summary>
    ///     Gets the trauma value for the current time, given the decay rate and start time.
    /// </summary>
    private float CalculateTraumaValueForCurrentTime(ESScreenshakeParameters parameters, TimeSpan start)
    {
        var timeDiff = _timing.CurTime - start;
        if (timeDiff < TimeSpan.Zero)
            return 0f;

        // trauma decays quadratically with seconds passed
        // https://www.desmos.com/calculator/optip8eucx
        var totalSecsSquared = (float) (timeDiff.TotalSeconds * timeDiff.TotalSeconds);
        return -totalSecsSquared * parameters.DecayRate + parameters.Trauma;
    }

    #endregion

    #region Public API

    public void Screenshake(EntityUid uid, ESScreenshakeParameters? translation, ESScreenshakeParameters? rotation)
    {
        if (!HasComp<EyeComponent>(uid))
            return;

        var comp = EnsureComp<ESScreenshakeComponent>(uid);
        var time = _timing.CurTime;
        var end = CalculateEndTimeForCommand(translation, rotation, time);

        comp.Commands.Add(new ESScreenshakeCommand(translation, rotation, time, end));
        Dirty(uid, comp);
    }

    public void Screenshake(Filter filter, ESScreenshakeParameters? translation, ESScreenshakeParameters? rotation)
    {
        foreach (var player in filter.Recipients)
        {
            if (player.AttachedEntity is { } ent)
                Screenshake(ent, translation, rotation);
        }
    }

    #endregion
}
