// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Nebula.ErpPanel;
using Content.Shared._Nebula.ERP;
using Content.Shared.Dataset;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.ERP;

public sealed partial class OrgasmSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ErpPanelSystem _erpPanel = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TransformSystem _transform = default!;

    private static readonly EntProtoId HeartsProto = "EffectHearts";
    private static readonly ProtoId<LocalizedDatasetPrototype> OrgasmMessagesDataset = "OrgasmMessages";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArousalComponent, ArousalOrgasmEvent>(OnOrgasm);
    }

    private void OnOrgasm(Entity<ArousalComponent> ent, ref ArousalOrgasmEvent args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent, out var profile))
            return;

        DoOrgasmEffects(ent, args.PrivateFilter, profile);
    }

    public void DoOrgasmEffects(EntityUid uid, Filter? privateFilter = null, HumanoidProfileComponent? profile = null)
    {
        Resolve(uid, ref profile, false);

        Spawn(HeartsProto, _transform.GetMapCoordinates(uid));
        PlayOrgasmSound(uid, profile?.Gender ?? Gender.Male);

        if (_prototype.TryIndex(OrgasmMessagesDataset, out var dataset))
            _erpPanel.SendEmote(uid, Loc.GetString(_random.Pick(dataset.Values)), privateFilter, false);

        _popup.PopupEntity(Loc.GetString("orgasm-popup-self"), uid, uid, PopupType.MediumCaution);

        var weakness = EnsureComp<OrgasmWeaknessComponent>(uid);
        weakness.ExpiresAt = _timing.CurTime + weakness.WeaknessDuration;
        Dirty(uid, weakness);
    }

    private void PlayOrgasmSound(EntityUid uid, Gender gender)
    {
        var collection = ErpAudio.GetOrgasmSounds(gender);

        var audioParams = new AudioParams
        {
            Variation = 0.1f,
            // linear fade, silent past 2 tiles
            MaxDistance = 2f,
            ReferenceDistance = 0.5f,
            Volume = 3f,
        };

        _audio.PlayPvs(new SoundCollectionSpecifier(collection), uid, audioParams);
    }
}
