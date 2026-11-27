// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/045e1f70c57558b76595316d8f399599f85bc753/Content.Shared/_Orion/Ghost/SharedGhostReturnToRoundSystem.cs

using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Orion.Ghost;

public abstract partial class SharedGhostReturnToRoundSystem : EntitySystem
{
    [Dependency] protected IConfigurationManager Cfg = default!;
    [Dependency] protected IGameTiming GameTiming = default!;

    protected TimeSpan GhostRespawnTime = TimeSpan.FromMinutes(5);

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(Cfg, CCVars.GhostRespawnTime, value => GhostRespawnTime = TimeSpan.FromSeconds(value), true);
    }
}

[Serializable, NetSerializable]
public sealed class GhostReturnToRoundRequest : EntityEventArgs;
