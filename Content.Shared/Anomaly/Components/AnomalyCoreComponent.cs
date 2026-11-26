using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
// Nebula start
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
// Nebula end

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// This component exists for a limited time, and after it expires it modifies the entity, greatly reducing its value and changing its visuals
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAnomalyCoreSystem), typeof(SharedAnomalySystem))] // Nebula edit
[AutoGenerateComponentState]
public sealed partial class AnomalyCoreComponent : Component
{
    /// <summary>
    /// Amount of time required for the core to decompose into an inert core
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double TimeToDecay = 600;

    /// <summary>
    /// The moment of core decay. It is set during entity initialization.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan DecayMoment;

    /// <summary>
    /// The starting value of the entity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double StartPrice = 10000;

    /// <summary>
    /// The value of the object sought during decaying
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double EndPrice = 200;

    /// <summary>
    /// Has the core decayed?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool IsDecayed;

    /// <summary>
    /// The amount of GORILLA charges the core has.
    /// Not used when <see cref="IsDecayed"/> is false.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public int Charge = 5;

    // Nebula start
    /// <summary>
    /// Inert core this core turns into when it decays.
    /// </summary>
    [DataField]
    public EntProtoId? DecayPrototype;

    /// <summary>
    /// Active core this inert core turns back into when reactivated.
    /// </summary>
    [DataField]
    public EntProtoId? ReactivationPrototype;

    /// <summary>
    /// Anomaly that can spawn as a hazardous side effect of reactivation.
    /// </summary>
    [DataField]
    public EntProtoId? HazardousAnomalyPrototype;

    /// <summary>
    /// Reagents that safely reactivate an inert core.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> ReactivationReagents = [];

    /// <summary>
    /// Reagents that reactivate an inert core but may cause a hazardous side effect.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> HazardousReactivationReagents = [];

    /// <summary>
    /// Amount of reagent consumed per reactivation attempt.
    /// </summary>
    [DataField]
    public FixedPoint2 ReactivationReagentAmount = FixedPoint2.New(5);

    [DataField]
    public float HazardousFailureChance = 0.65f;
    // Nebula end
}
