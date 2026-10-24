// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Lobby;
using Content.Shared._Nebula.CCVar;
using Content.Shared.GameTicking.Prototypes;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby;

public sealed partial class LobbyState
{
    private void StartupNebula()
    {
        Lobby!.CharacterPreview.WardrobeButton.OnPressed += OnWardrobePressed;

        _cfg.OnValueChanged(NCCVars.LobbyBackgroundRandom, OnLobbyBackgroundOptionChanged);
        _cfg.OnValueChanged(NCCVars.LobbyBackground, OnLobbyBackgroundOptionChanged, true);
    }

    private void ShutdownNebula()
    {
        Lobby!.CharacterPreview.WardrobeButton.OnPressed -= OnWardrobePressed;

        _cfg.UnsubValueChanged(NCCVars.LobbyBackgroundRandom, OnLobbyBackgroundOptionChanged);
        _cfg.UnsubValueChanged(NCCVars.LobbyBackground, OnLobbyBackgroundOptionChanged);
    }

    private void OnLobbyBackgroundOptionChanged<T>(T value)
    {
        UpdateLobbyBackground();
    }

    private ProtoId<LobbyBackgroundPrototype>? GetNebulaLobbyBackground()
    {
        return NebulaLobbyBackgrounds.GetPicked(_cfg, _protoMan) ?? _gameTicker.LobbyBackground;
    }

    private void OnWardrobePressed(BaseButton.ButtonEventArgs args)
    {
        OnSetupPressed(args);
        _userInterfaceManager.GetUIController<LobbyUIController>().SelectProfileEditorTab(LobbyUIController.JobsTab);
    }
}
