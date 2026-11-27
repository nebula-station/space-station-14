// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/0045faba82b100c782c32fb14427c1428578628d/Content.Arcane.Shared/Explosion/DecalGrenadeComponent.cs

using Content.Shared.Decals;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.Explosion;

/// <summary>
/// Scatters random decals around the entity when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DecalGrenadeComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The decals to pick from.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<DecalPrototype>> DecalPrototypes = [];

    /// <summary>
    /// How many decals to spawn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DecalCount = 25;

    /// <summary>
    /// Radius around the entity in which decals are spawned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DecalRadius = 3f;
}
