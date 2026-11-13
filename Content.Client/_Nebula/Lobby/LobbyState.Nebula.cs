// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Lobby.UI;
using Content.Client._Nebula.Lobby;
using Content.Shared._Nebula.CCVar;
using Content.Shared._Nebula.Lobby;
using Content.Shared.GameTicking.Prototypes;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby;

public sealed partial class LobbyState
{
    private void StartupNebula()
    {
        Lobby!.CharacterPreview.WardrobeButton.OnPressed += OnWardrobePressed;
        Lobby.PreviousSongButton.OnPressed += OnPreviousSongPressed;
        Lobby.NextSongButton.OnPressed += OnNextSongPressed;
        Lobby.PauseSongButton.OnPressed += OnPauseSongPressed;
        UpdatePauseSongButton();
        Lobby.PreviousBackgroundButton.OnPressed += OnPreviousBackgroundPressed;
        Lobby.NextBackgroundButton.OnPressed += OnNextBackgroundPressed;

        _cfg.OnValueChanged(NCCVars.LobbyBackgroundRandom, OnLobbyBackgroundOptionChanged);
        _cfg.OnValueChanged(NCCVars.LobbyBackground, OnLobbyBackgroundOptionChanged, true);
    }

    private void ShutdownNebula()
    {
        Lobby!.CharacterPreview.WardrobeButton.OnPressed -= OnWardrobePressed;
        Lobby.PreviousSongButton.OnPressed -= OnPreviousSongPressed;
        Lobby.NextSongButton.OnPressed -= OnNextSongPressed;
        Lobby.PauseSongButton.OnPressed -= OnPauseSongPressed;
        Lobby.PreviousBackgroundButton.OnPressed -= OnPreviousBackgroundPressed;
        Lobby.NextBackgroundButton.OnPressed -= OnNextBackgroundPressed;

        _cfg.UnsubValueChanged(NCCVars.LobbyBackgroundRandom, OnLobbyBackgroundOptionChanged);
        _cfg.UnsubValueChanged(NCCVars.LobbyBackground, OnLobbyBackgroundOptionChanged);
        _backgroundAnimation?.Stop();
    }

    private void OnLobbyBackgroundOptionChanged<T>(T value)
    {
        UpdateLobbyBackground();
    }

    private LobbyBackgroundAnimation? _backgroundAnimation;

    /// <summary>
    /// Shows a static or animated lobby background, animated ones are ported from Monolith.
    /// </summary>
    private void SetNebulaLobbyBackground(LobbyBackgroundPrototype? proto)
    {
        _backgroundAnimation ??= new LobbyBackgroundAnimation();
        _backgroundAnimation.Stop();

        if (Lobby == null)
            return;

        if (proto == null)
        {
            Lobby.Background.Texture = null;
            Lobby.Background.BlurTexture = null;
            return;
        }

        var firstFrame = _backgroundAnimation.Start(proto);
        Lobby.Background.Texture = firstFrame
            ?? (_resourceCache.TryGetResource<TextureResource>(proto.Background, out var texture) ? texture.Texture : null);
        Lobby.Background.BlurTexture = firstFrame;
    }

    private void UpdateNebulaLobbyBackground(float frameTime)
    {
        if (Lobby == null || _backgroundAnimation is not { Playing: true })
            return;

        if (_backgroundAnimation.Update(frameTime) is { } frame)
            Lobby.Background.Texture = frame;

        Lobby.Background.BlurTexture ??= _backgroundAnimation.FirstFrame;
    }

    private ProtoId<LobbyBackgroundPrototype>? GetNebulaLobbyBackground()
    {
        return NebulaLobbyBackgrounds.GetPicked(_cfg, _protoMan) ?? _gameTicker.LobbyBackground;
    }

    private void OnPreviousBackgroundPressed(BaseButton.ButtonEventArgs args) => CycleNebulaLobbyBackground(-1);

    private void OnNextBackgroundPressed(BaseButton.ButtonEventArgs args) => CycleNebulaLobbyBackground(1);

    /// <summary>
    /// Switches to the previous or next lobby background and remembers it, like the Monolith lobby does.
    /// Picking one by hand turns the random server background off, same as choosing it in the options.
    /// </summary>
    private void CycleNebulaLobbyBackground(int delta)
    {
        var available = NebulaLobbyBackgrounds.GetAvailable(_protoMan);
        if (available.Count == 0)
            return;

        var current = GetNebulaLobbyBackground();
        var index = available.FindIndex(proto => proto.ID == current);
        index = index < 0
            ? (delta > 0 ? 0 : available.Count - 1)
            : ((index + delta) % available.Count + available.Count) % available.Count;

        _cfg.SetCVar(NCCVars.LobbyBackgroundRandom, false);
        _cfg.SetCVar(NCCVars.LobbyBackground, available[index].ID);
        _cfg.SaveToFile();
    }

    private LobbyTrackPrototype? GetNebulaLobbyTrack(string filename)
    {
        var path = new Robust.Shared.Utility.ResPath(filename);
        foreach (var track in _protoMan.EnumeratePrototypes<LobbyTrackPrototype>())
        {
            if (track.Path == path)
                return track;
        }

        return null;
    }

    private void OnPreviousSongPressed(BaseButton.ButtonEventArgs args) => _contentAudioSystem.SkipLobbyTrack(-1);

    private void OnNextSongPressed(BaseButton.ButtonEventArgs args) => _contentAudioSystem.SkipLobbyTrack(1);

    private void OnPauseSongPressed(BaseButton.ButtonEventArgs args)
    {
        _contentAudioSystem.ToggleLobbyMusicPause();
        UpdatePauseSongButton();
    }

    /// <summary>
    /// Shows the pause or play icon on the lobby music pause button, ported from Monolith.
    /// </summary>
    private void UpdatePauseSongButton()
    {
        if (Lobby == null)
            return;

        var paused = _contentAudioSystem.LobbyMusicPaused;
        Lobby.PauseSongButton.Icon = _resourceCache.GetResource<TextureResource>(paused
            ? "/Textures/Interface/AdminActions/play.png"
            : "/Textures/Interface/AdminActions/pause.png").Texture;
        Lobby.PauseSongButton.ToolTip = Loc.GetString(paused ? "ui-lobby-song-resume" : "ui-lobby-song-pause");
    }

    private void OnWardrobePressed(BaseButton.ButtonEventArgs args)
    {
        OnSetupPressed(args);
        _userInterfaceManager.GetUIController<LobbyUIController>().SelectWardrobeTab();
    }

    /// <summary>
    /// Opens the wardrobe tab of the character setup, same as the wardrobe button of the character panel.
    /// </summary>
    public void OpenWardrobe()
    {
        SetReady(false);
        Lobby?.SwitchState(LobbyGui.LobbyGuiState.CharacterSetup);
        _userInterfaceManager.GetUIController<LobbyUIController>().SelectWardrobeTab();
    }
}
