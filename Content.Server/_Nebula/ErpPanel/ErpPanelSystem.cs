// SPDX-FileCopyrightText: 2026 C_Quarian
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared._Nebula.CCVar;
using Content.Shared._Nebula.ERP;
using Content.Shared._Nebula.ErpPanel;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Nebula.ErpPanel;

public sealed partial class ErpPanelSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ArousalSystem _arousal = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private INetConfigurationManager _netCfg = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    private static readonly EntProtoId HeartsProto = "EffectHearts";

    private static readonly SpriteSpecifier VerbIcon =
        new SpriteSpecifier.Texture(new("/Textures/_Nebula/Interface/ErpPanel/heart_icon.png"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ErpPanelOwnerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ErpPanelOwnerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ErpPanelOwnerComponent, ErpPanelOpenEvent>(OnOpenPanel);
        SubscribeLocalEvent<ErpPanelOwnerComponent, BoundUIClosedEvent>(OnBoundUIClosed);
        SubscribeLocalEvent<ErpPanelOwnerComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        Subs.BuiEvents<ErpPanelOwnerComponent>(ErpPanelKey.Key, subs =>
        {
            subs.Event<ErpPanelSendMessage>(OnSendMessage);
            subs.Event<ErpPanelCustomEmoteMessage>(OnCustomEmote);
        });
    }

    private void OnMapInit(Entity<ErpPanelOwnerComponent> entity, ref MapInitEvent args)
    {
        var interfaceData = new InterfaceData(
            clientType: "Content.Client._Nebula.ErpPanel.ErpPanelWindowBUI"
        );

        _ui.SetUi(entity.Owner, ErpPanelKey.Key, interfaceData);
    }

    // arousal is only for player controlled bodies
    private void OnPlayerAttached(Entity<ErpPanelOwnerComponent> entity, ref PlayerAttachedEvent args)
    {
        EnsureComp<ArousalComponent>(entity);
    }

    private void OnOpenPanel(Entity<ErpPanelOwnerComponent> entity, ref ErpPanelOpenEvent args)
    {
        if (!_interaction.IsAccessible(entity.Owner, args.Target))
            return;

        if (!TryOpenPanel(entity.Owner, args.Target))
            return;

        entity.Comp.Target = args.Target;

        // arousal is only sent to its owner and the partner with the panel open
        if (TryComp<ArousalComponent>(args.Target, out var targetArousal))
            Dirty(args.Target, targetArousal);
    }

    private void OnBoundUIClosed(Entity<ErpPanelOwnerComponent> entity, ref BoundUIClosedEvent args)
    {
        if (args.UiKey is not ErpPanelKey.Key)
            return;

        entity.Comp.Target = null;
    }

    private void OnSendMessage(Entity<ErpPanelOwnerComponent> entity, ref ErpPanelSendMessage args)
    {
        var user = args.Actor;
        var target = entity.Comp.Target;
        if (target == null || user != entity.Owner)
            return;

        ProcessInteraction(user, target.Value, args.Interaction, args.PrivateMode);
    }

    private void OnCustomEmote(Entity<ErpPanelOwnerComponent> entity, ref ErpPanelCustomEmoteMessage args)
    {
        var target = entity.Comp.Target;
        if (target == null || args.Actor != entity.Owner)
            return;

        ProcessCustomInteraction(args.Actor, target.Value, args.Emote, args.PrivateMode);
    }

    private void OnGetVerbs(Entity<ErpPanelOwnerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!IsValidUi(args.User, ent.Owner))
            return;

        var user = args.User;

        Verb verb = new()
        {
            Act = () =>
            {
                var ev = new ErpPanelOpenEvent(ent.Owner);
                RaiseLocalEvent(user, ref ev);
            },
            Text = Loc.GetString("erp-panel-open-verb"),
            Icon = VerbIcon,
            Priority = 2,
        };

        args.Verbs.Add(verb);
    }

    public void ProcessInteraction(EntityUid user, EntityUid target, string interactionId, bool privateMode = false)
    {
        if (!_prototype.TryIndex<PanelInteractionPrototype>(interactionId, out var interaction))
            return;

        if (!CheckRequirements(user, target, interaction))
            return;

        var messagesId = user == target ? interaction.SelfMessages : interaction.Messages;
        if (!_prototype.TryIndex(messagesId, out var messages))
            return;

        var message = Loc.GetString(_random.Pick(messages.Values),
            ("target", Identity.Name(target, EntityManager, user)));

        RunInteraction(user, target, InteractionData.From(interaction), message, privateMode);
    }

    public void ProcessCustomInteraction(EntityUid user, EntityUid target, ErpCustomEmote emote, bool privateMode)
    {
        if (emote.Self != (user == target) || !Enum.IsDefined(emote.Kind) || !_prototype.HasIndex(emote.Category))
            return;

        var message = ErpCustomEmote.Sanitize(emote.Message, ErpCustomEmote.MaxMessageLength);
        if (message.Length == 0)
            return;

        SoundSpecifier? sound = null;
        if (emote.Sound is { } soundId)
        {
            if (!_prototype.TryIndex(soundId, out var soundProto))
                return;

            sound = soundProto.Sound;
        }

        var interaction = new InteractionData(emote.Kind, ErpCustomEmote.Range, sound,
            emote.Kind == PanelInteractionKind.Erotic ? 1 : 0, GetCustomArouse(emote.Kind));
        message = message.Replace(ErpCustomEmote.TargetPlaceholder, Identity.Name(target, EntityManager, user));

        RunInteraction(user, target, interaction, message, privateMode);
    }

    private readonly record struct InteractionData(
        PanelInteractionKind Kind,
        float Range,
        SoundSpecifier? Sound,
        int UserArouse,
        int TargetArouse)
    {
        public bool IsErotic => Kind is PanelInteractionKind.Erotic or PanelInteractionKind.HarmfulErotic;

        public static InteractionData From(PanelInteractionPrototype proto)
        {
            return new InteractionData(proto.Kind, proto.Range, proto.Sound, proto.UserArouse, proto.TargetArouse);
        }
    }

    private static int GetCustomArouse(PanelInteractionKind kind)
    {
        return kind switch
        {
            PanelInteractionKind.Erotic => 5,
            PanelInteractionKind.HarmfulErotic => -5,
            _ => 0,
        };
    }

    private void RunInteraction(EntityUid user, EntityUid target, InteractionData interaction, string message, bool privateMode)
    {
        if (!TryComp<ErpPanelOwnerComponent>(user, out var userPanel))
            return;

        if (!IsValidInteraction(user, target, interaction, userPanel))
            return;

        Filter? privateFilter = privateMode
            ? Filter.Entities(user, target)
            : null;

        if (interaction.TargetArouse != 0 && TryComp<ArousalComponent>(target, out var targetArousalComp))
        {
            var targetDelta = interaction.TargetArouse * GetMultiplier(target, NCCVars.ErpArousalMultiplier) / 100;
            var currentTargetArousal = _arousal.GetArousal(targetArousalComp);

            if (targetDelta < 0 && Math.Abs(targetDelta) > currentTargetArousal)
                targetDelta = -currentTargetArousal;

            if (targetDelta > 0)
                Spawn(HeartsProto, _transform.GetMapCoordinates(target));

            _arousal.AddArousal(target, targetDelta, privateFilter: privateFilter);
            ProcessMoan(target, GetMultiplier(target, NCCVars.ErpMoanMultiplier), privateFilter);
        }

        userPanel.LastInteraction = _timing.CurTime;

        SendEmote(user, message, privateFilter, false);
        ProcessSounds(user, interaction.Sound, privateFilter);
        ProcessLunge(user, target, privateFilter);

        if (user == target)
            return;

        if (interaction.Kind == PanelInteractionKind.Erotic)
        {
            ProcessContactDamage(user, target);
            ProcessContactDamage(target, user);
        }

        if (interaction.UserArouse != 0 && TryComp<ArousalComponent>(user, out var userArousalComp))
        {
            var userDelta = interaction.UserArouse * GetMultiplier(user, NCCVars.ErpArousalMultiplier) / 100;
            var currentUserArousal = _arousal.GetArousal(userArousalComp);

            if (userDelta < 0 && Math.Abs(userDelta) > currentUserArousal)
                userDelta = -currentUserArousal;

            _arousal.AddArousal(user, userDelta, privateFilter: privateFilter);
            ProcessMoan(user, GetMultiplier(user, NCCVars.ErpMoanMultiplier), privateFilter);
        }
    }

    private void ProcessContactDamage(EntityUid source, EntityUid victim)
    {
        if (!TryComp<ErpContactDamageComponent>(source, out var contact))
            return;

        if (TryComp<HumanoidProfileComponent>(source, out var sourceProfile) &&
            TryComp<HumanoidProfileComponent>(victim, out var victimProfile) &&
            sourceProfile.Species == victimProfile.Species)
            return;

        _damageable.TryChangeDamage(victim, contact.Damage, origin: source);

        if (_prototype.TryIndex(contact.Popups, out var popups))
            _popup.PopupEntity(Loc.GetString(_random.Pick(popups.Values)), victim, victim, PopupType.SmallCaution);
    }

    private void ProcessMoan(EntityUid uid, float moanMultiplier, Filter? privateFilter)
    {
        if (!TryComp<ArousalComponent>(uid, out var arousal) || arousal.MaxArousal <= 0)
            return;
    
        var arousalPercent = Math.Clamp(_arousal.GetArousal(arousal) / arousal.MaxArousal, 0f, 1f);
        if (arousalPercent <= 0f)
            return;
    
        if (!TryComp<HumanoidProfileComponent>(uid, out var profile))
            return;
    
        var moanChance = Math.Clamp(arousalPercent * moanMultiplier / 100f, 0f, 1f);
    
        if (_random.Prob(moanChance))
            MoanWithGender(uid, profile.Gender, arousalPercent, privateFilter);
    }

    private void MoanWithGender(EntityUid uid, Gender gender, float arousalPercent, Filter? privateFilter)
    {
        var collection = ErpAudio.GetMoanSounds(gender);

        if (!_prototype.TryIndex(collection, out var soundCollection))
            return;

        if (soundCollection.PickFiles.Count == 0)
            return;

        arousalPercent = Math.Clamp(arousalPercent, 0f, 1f);

        // stronger moans as arousal grows
        var index = (int) Math.Ceiling(arousalPercent * soundCollection.PickFiles.Count) - 1;
        index += _random.Next(-2, 1);
        index = Math.Clamp(index, 0, soundCollection.PickFiles.Count - 1);

        var audioParams = new AudioParams
        {
            Variation = 0.125f,
            MaxDistance = 4f,
        };

        var sound = new ResolvedCollectionSpecifier(collection, index);
        if (privateFilter != null)
            _audio.PlayEntity(sound, privateFilter, uid, false, audioParams);
        else
            _audio.PlayPvs(sound, uid, audioParams);

        SendEmote(uid, Loc.GetString("moan-message"), privateFilter, true);
    }

    /// <summary>
    /// Sends an emote, to the given recipients only when <paramref name="privateFilter"/> is set.
    /// </summary>
    public void SendEmote(EntityUid source, string message, Filter? privateFilter, bool hideChat)
    {
        if (privateFilter == null)
        {
            _chat.TrySendInGameICMessage(source, message, InGameICChatType.Emote, hideChat);
            return;
        }

        var ent = Identity.Entity(source, EntityManager);
        var wrapped = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", FormattedMessage.EscapeText(Name(ent))),
            ("entity", ent),
            ("message", FormattedMessage.RemoveMarkupOrThrow(message)));

        _chatManager.ChatMessageToManyFiltered(privateFilter, ChatChannel.Emotes, message, wrapped, source, hideChat, false, null);

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Private ERP emote from {ToPrettyString(source):user}: {message}");
    }

    private void ProcessLunge(EntityUid user, EntityUid target, Filter? privateFilter)
    {
        if (user == target)
            return;

        var userXform = Transform(user);

        var invMatrix = _transform.GetInvWorldMatrix(userXform);
        var localPos = Vector2.Transform(_transform.GetMapCoordinates(target).Position, invMatrix);

        if (localPos.LengthSquared() <= 0f)
            return;

        localPos = userXform.LocalRotation.RotateVec(localPos);

        RaiseNetworkEvent(
            new ErpLungeEvent(GetNetEntity(user), localPos),
            privateFilter ?? Filter.Pvs(user, entityManager: EntityManager));
    }

    private void ProcessSounds(EntityUid user, SoundSpecifier? sound, Filter? privateFilter)
    {
        if (privateFilter != null)
            _audio.PlayEntity(sound, privateFilter, user, false);
        else
            _audio.PlayPvs(sound, user);
    }

    private bool TryOpenPanel(EntityUid user, EntityUid target)
    {
        if (!IsValidUi(user, target))
            return false;

        _ui.TryOpenUi(user, ErpPanelKey.Key, user);

        var state = new ErpPanelBuiState(GetNetEntity(user), GetNetEntity(target));
        _ui.SetUiState(user, ErpPanelKey.Key, state);
        return true;
    }

    private bool IsValidUi(EntityUid user, EntityUid target)
    {
        if (!HasComp<ErpPanelOwnerComponent>(user))
            return false;

        if (!HasComp<ErpPanelOwnerComponent>(target))
            return false;

        if (!HasComp<ArousalComponent>(user) || !HasComp<ArousalComponent>(target))
            return false;

        if (!IsPanelEnabled(user) || !IsPanelEnabled(target))
            return false;

        if (_mobState.IsIncapacitated(user) || _mobState.IsIncapacitated(target))
            return false;

        return true;
    }

    private bool IsValidInteraction(EntityUid user, EntityUid target, InteractionData interaction, ErpPanelOwnerComponent userPanel)
    {
        if (userPanel.LastInteraction + ErpPanelOwnerComponent.Cooldown > _timing.CurTime)
            return false;

        if (!_interaction.InRangeAndAccessible(user, target, interaction.Range))
            return false;

        if (!IsValidUi(user, target))
            return false;

        if (interaction.IsErotic && !CanUseErp(user, target))
            return false;

        // cuffed, asleep or stunned - Soft only
        if (interaction.Kind != PanelInteractionKind.Soft && !CanActFreely(user, target))
            return false;

        return true;
    }

    private bool CanActFreely(EntityUid user, EntityUid target)
    {
        return _actionBlocker.CanInteract(user, target)
               && (user == target || _actionBlocker.CanInteract(target, null));
    }

    private bool IsPanelEnabled(EntityUid uid)
    {
        // SSD or abandoned body
        if (!TryComp<ActorComponent>(uid, out var actor))
            return false;

        return _netCfg.GetClientCVar(actor.PlayerSession.Channel, NCCVars.ErpPanelEnabled);
    }

    private float GetMultiplier(EntityUid uid, CVarDef<float> cvar)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return 100f;

        // NaN passes through Math.Clamp
        var value = _netCfg.GetClientCVar(actor.PlayerSession.Channel, cvar);
        return float.IsFinite(value) ? Math.Clamp(value, 0f, ErpPanelOwnerComponent.MaxMultiplier) : 100f;
    }

    private bool CanUseErp(EntityUid user, EntityUid target)
    {
        if (!TryComp<ERPManagerComponent>(user, out var userErp) || userErp.Status == ERPStatus.Never)
            return false;

        if (user == target)
            return true;

        return TryComp<ERPManagerComponent>(target, out var targetErp)
               && targetErp.Status != ERPStatus.Never;
    }

    private bool CheckRequirements(EntityUid user, EntityUid target, PanelInteractionPrototype interaction)
    {
        if (interaction.UserRequirements != null)
        {
            foreach (var requirement in interaction.UserRequirements)
            {
                if (!requirement.IsAvailable(user, EntityManager))
                    return false;
            }
        }

        if (interaction.TargetRequirements != null)
        {
            foreach (var requirement in interaction.TargetRequirements)
            {
                if (!requirement.IsAvailable(target, EntityManager))
                    return false;
            }
        }

        return true;
    }
}
