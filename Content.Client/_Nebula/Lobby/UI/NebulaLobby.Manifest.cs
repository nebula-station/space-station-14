// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.ReadyManifest;
using Content.Client._Nebula.UserInterface;
using Content.Client.CrewManifest;
using Content.Shared._Nebula.CCVar;
using Content.Shared.CCVar;

namespace Content.Client._Nebula.Lobby.UI;

public sealed partial class NebulaLobby
{
    private void InitManifest()
    {
        ManifestButton.Icon = NebulaIcons.Get("assignment");
        ManifestButton.OnPressed += _ => OpenManifest();
    }

    private void OnManifestCVarChanged(bool _)
    {
        UpdateManifestButton();
    }

    private void UpdateManifestButton()
    {
        var crew = _gameTicker?.IsGameStarted ?? false;

        ManifestButton.Visible = crew
            ? _cfg.GetCVar(CCVars.CrewManifestWithoutEntity)
            : _cfg.GetCVar(NCCVars.LobbyReadyManifest);
        ManifestButton.ToolTip = Loc.GetString(crew ? "crew-manifest-window-title" : "nebula-ready-manifest-title");
    }

    private void OpenManifest()
    {
        if (_gameTicker == null)
            return;

        if (!_gameTicker.IsGameStarted)
        {
            _entitySystems.GetEntitySystem<ReadyManifestSystem>().RequestReadyManifest();
            return;
        }

        foreach (var station in _gameTicker.StationNames.Keys)
        {
            _entitySystems.GetEntitySystem<CrewManifestSystem>().RequestCrewManifest(station);
            return;
        }
    }
}
