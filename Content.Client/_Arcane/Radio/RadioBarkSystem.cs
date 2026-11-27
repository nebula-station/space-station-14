// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Client/Radio/RadioBarkSystem.cs

using Content.Shared._Arcane.CCVar;
using Content.Shared._Arcane.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client._Arcane.Radio;

public sealed partial class RadioBarkSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    /// <summary>
    /// The options slider goes up to this gain.
    /// </summary>
    public const float VolumeMultiplier = 2f;

    [SubscribeNetworkEvent]
    private void OnRadioBark(RadioBarkEvent ev)
    {
        var gain = _cfg.GetCVar(ArcaneCVars.RadioBarkVolume) * VolumeMultiplier;
        if (gain <= 0f)
            return;

        var audioParams = ev.Params.WithVolume(ev.Params.Volume + SharedAudioSystem.GainToVolume(gain));
        _audio.PlayGlobal(new SoundPathSpecifier(ev.Path), Filter.Local(), false, audioParams);
    }
}
