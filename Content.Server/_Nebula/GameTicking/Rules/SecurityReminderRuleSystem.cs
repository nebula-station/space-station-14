// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Nebula.Radio;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radio.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Nebula.GameTicking.Rules;

public sealed partial class SecurityReminderRuleSystem : GameRuleSystem<SecurityReminderRuleComponent>
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private RadioSoundSystem _radioSound = default!;

    protected override void Started(Entity<SecurityReminderRuleComponent, GameRuleComponent> rule,
        ref GameRuleStartedEvent args)
    {
        base.Started(rule, ref args);

        ScheduleNext(rule.Comp1);
    }

    protected override void ActiveTick(EntityUid uid,
        SecurityReminderRuleComponent component,
        GameRuleComponent gameRule,
        float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (Timing.CurTime < component.NextReminder)
            return;

        ScheduleNext(component);

        List<EntityUid> wearers = [];
        var query = EntityQueryEnumerator<HeadsetComponent, TransformComponent>();
        while (query.MoveNext(out var headset, out _, out var xform))
        {
            var wearer = xform.ParentUid;
            if (!_radioSound.IsWornHeadset(headset, component.Channel)
                || !HasComp<ActorComponent>(wearer)
                || !_mobState.IsAlive(wearer))
                continue;

            wearers.Add(wearer);
        }

        if (wearers.Count == 0)
            return;

        _audio.PlayPvs(component.Sound,
            RobustRandom.Pick(wearers),
            AudioParams.Default.WithMaxDistance(component.Range));
    }

    private void ScheduleNext(SecurityReminderRuleComponent component)
    {
        component.NextReminder = Timing.CurTime + TimeSpan.FromSeconds(component.Delay.Next(RobustRandom));
    }
}
