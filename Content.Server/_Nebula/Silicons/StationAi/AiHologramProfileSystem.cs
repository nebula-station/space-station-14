// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Preferences.Managers;
using Content.Shared._Nebula.Silicons.StationAi;
using Content.Shared.Preferences;
using Content.Shared.Silicons.StationAi;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Silicons.StationAi;

public sealed partial class AiHologramProfileSystem : EntitySystem
{
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly ProtoId<StationAiCustomizationGroupPrototype> HologramGroup = "StationAiHolograms";

    public override void Initialize()
    {
        base.Initialize();

        _prefs.PreferencesChanged += OnPreferencesChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _prefs.PreferencesChanged -= OnPreferencesChanged;
    }

    private void OnPreferencesChanged(NetUserId userId)
    {
        if (!_players.TryGetSessionById(userId, out var session)
            || session.AttachedEntity is not { } attached
            || !TryComp<AiHologramProfileComponent>(attached, out var comp))
            return;

        UpdateProfile((attached, comp));
    }

    [SubscribeLocalEvent]
    private void OnCustomizationChanged(Entity<AiHologramProfileComponent> ent, ref AiHologramCustomizationChangedEvent args)
    {
        UpdateProfile(ent);
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<AiHologramProfileComponent> ent, ref PlayerAttachedEvent args)
    {
        UpdateProfile(ent);
    }

    private void UpdateProfile(Entity<AiHologramProfileComponent> ent)
    {
        HumanoidCharacterProfile? profile = null;

        if (TryComp<StationAiCustomizationComponent>(ent, out var customization)
            && customization.ProtoIds.TryGetValue(HologramGroup, out var protoId)
            && _proto.Resolve(protoId, out var proto)
            && proto.UseCharacterProfile)
        {
            // Without a player the hologram keeps the last character.
            if (!TryComp<ActorComponent>(ent, out var actor))
                return;

            if (_prefs.TryGetCachedPreferences(actor.PlayerSession.UserId, out var prefs)
                && prefs.SelectedCharacter is HumanoidCharacterProfile selected)
                profile = GetVisualProfile(selected);

            // TODO: apply the character's voice here once TTS is ported.
        }

        if (profile == ent.Comp.Profile
            || profile != null && ent.Comp.Profile != null && profile.MemberwiseEquals(ent.Comp.Profile))
            return;

        ent.Comp.Profile = profile;
        Dirty(ent);
    }

    /// <summary>
    /// Copy with only the appearance. The component reaches every client, unlike the player's other preferences.
    /// </summary>
    private static HumanoidCharacterProfile GetVisualProfile(HumanoidCharacterProfile profile)
    {
        return new HumanoidCharacterProfile()
            .WithSpecies(profile.Species)
            .WithSex(profile.Sex)
            .WithCharacterAppearance(profile.Appearance)
            .WithHeight(profile.Height)
            .WithWidth(profile.Width);
    }
}
