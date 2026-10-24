// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.ReadyManifest;

/// <summary>
/// Asks the server to open the manifest of players ready for the next round.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestReadyManifestEvent : EntityEventArgs;

/// <summary>
/// One ready player wanting one job: their selected character and their username.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct ReadyManifestEntry(ProtoId<JobPrototype> Job, string CharacterName, string UserName);

[Serializable, NetSerializable]
public sealed class ReadyManifestEuiState(List<ReadyManifestEntry> entries, int readyCount) : EuiStateBase
{
    public readonly List<ReadyManifestEntry> Entries = entries;

    /// <summary>
    /// Ready players, including those whose wanted jobs are not listed.
    /// </summary>
    public readonly int ReadyCount = readyCount;
}
