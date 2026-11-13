// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server._Nebula.ErpPanel;
using Content.Shared._Nebula.ERP;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Forensics.Systems;
using Content.Shared.Interaction;
using Content.Trauma.Common.Effects;
using Content.Shared.Dataset;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Map;
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
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private CumOverlaySystem _overlay = default!;
    [Dependency] private ForensicsSystem _forensics = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private CommonSparksSystem _sparks = default!;

    private static readonly EntProtoId HeartsProto = "EffectHearts";
    private static readonly ProtoId<LocalizedDatasetPrototype> OrgasmMessagesDataset = "OrgasmMessages";

    // Nebula - ported from Arcane: fluids and stains
    private static readonly EntProtoId StainWallProto = "EffectCumWall";
    private static readonly EntProtoId SemenPuddleProto = "PuddleSemen";
    private static readonly EntProtoId FemCumPuddleProto = "PuddleFemCum";
    private const string PuddleSolution = "puddle";
    private const float EffectDistance = 0.6f;
    private const float TargetDistance = 1.4f;
    private const float BlockedDistance = 0.1f;
    private const float WallCheckExtraRange = 0.1f;
    private const float ForwardDot = 0.6f;

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

        if (profile != null)
            SpawnFluids(uid, profile.Sex);

        var weakness = EnsureComp<OrgasmWeaknessComponent>(uid);
        weakness.ExpiresAt = _timing.CurTime + weakness.WeaknessDuration;
        Dirty(uid, weakness);
    }

    private void SpawnFluids(EntityUid uid, Sex sex)
    {
        if (sex == Sex.Unsexed)
            return;

        if (HasComp<SparkEjaculationComponent>(uid))
        {
            _sparks.DoSparks(uid, playSound: true);
            return;
        }

        var xform = Transform(uid);
        if (xform.MapID == MapId.Nullspace)
            return;

        var (sourcePos, sourceRot) = _transform.GetWorldPositionRotation(xform);
        var forward = sourceRot.ToWorldVec();

        SpawnPuddle(uid, sourcePos, xform.MapID, forward, sex);

        foreach (var target in _lookup.GetEntitiesInRange<HumanoidProfileComponent>(new MapCoordinates(sourcePos, xform.MapID), TargetDistance))
        {
            if (target.Owner == uid)
                continue;

            // stains are an ERP effect, never put them on someone who opted out
            if (!TryComp<ERPManagerComponent>(target, out var erp) || erp.Status == ERPStatus.Never)
                continue;

            var toTarget = _transform.GetWorldPosition(target.Owner) - sourcePos;
            if (toTarget.LengthSquared() < 0.0001f || Vector2.Dot(Vector2.Normalize(toTarget), forward) < ForwardDot)
                continue;

            if (!_interaction.InRangeUnobstructed(uid, target.Owner, TargetDistance))
                continue;

            _overlay.AddOverlay(target.Owner);
        }
    }

    private void SpawnPuddle(EntityUid uid, Vector2 sourcePos, MapId map, Vector2 forward, Sex sex)
    {
        var forwardMap = new MapCoordinates(sourcePos + forward * EffectDistance, map);
        var blocked = !_interaction.InRangeUnobstructed(uid, forwardMap, EffectDistance + WallCheckExtraRange);
        var coords = blocked
            ? new MapCoordinates(sourcePos + forward * BlockedDistance, map)
            : forwardMap;

        if (blocked)
            Spawn(StainWallProto, forwardMap);

        var puddle = Spawn(sex == Sex.Female ? FemCumPuddleProto : SemenPuddleProto, coords);
        _forensics.TransferDna(puddle, uid, false);

        var dna = _bloodstream.GetEntityBloodData(uid);
        if (dna.Count == 0 || !_solution.TryGetSolution(puddle, PuddleSolution, out var solutionEnt, out var solution))
            return;

        // Arcane added DNA to a copy of the reagent and lost it, the data has to be written back
        solution.SetReagentData(dna);
        _solution.UpdateChemicals(solutionEnt.Value);
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
