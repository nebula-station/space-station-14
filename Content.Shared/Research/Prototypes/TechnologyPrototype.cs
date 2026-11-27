using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
// Nebula start
using Robust.Shared.Serialization;
using Content.Shared.Radio;
// Nebula end

namespace Content.Shared.Research.Prototypes;

/// <summary>
/// This is a prototype for a technology that can be unlocked.
/// </summary>
[Prototype]
public sealed partial class TechnologyPrototype : IPrototype, ISerializationHooks // Nebula edit
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of the technology.
    /// Supports locale strings
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    // Nebula start
    /// <summary>
    /// Localized description of the technology.
    /// </summary>
    [DataField]
    public LocId? Description;
    // Nebula end

    /// <summary>
    /// An icon used to visually represent the technology in UI.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// What research discipline this technology belongs to.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TechDisciplinePrototype> Discipline;

    /// <summary>
    /// What tier research is this?
    /// The tier governs how much lower-tier technology
    /// needs to be unlocked before this one.
    /// </summary>
    [DataField(required: true)]
    public int Tier;

    /// <summary>
    /// Hidden tech is not ever available at the research console.
    /// </summary>
    [DataField]
    public bool Hidden;

    // Nebula start
    /// <summary>
    /// Should this technology start as researched in every compatible database.
    /// </summary>
    [DataField]
    public bool StartingTechnology;
    // Nebula end

    /// <summary>
    /// How much research is needed to unlock.
    /// </summary>
    [DataField]
    public int Cost = 10000;

    // Nebula start
    /// <summary>
    /// Multipoint type costs.
    /// </summary>
    [DataField]
    public List<ResearchPointAmount> PointCosts = [];
    // Nebula end

    /// <summary>
    /// A list of <see cref="TechnologyPrototype"/>s that need to be unlocked in order to unlock this technology.
    /// </summary>
    [DataField]
    public List<ProtoId<TechnologyPrototype>> TechnologyPrerequisites = new();

    // Nebula start
    /// <summary>
    /// The required experiments that must be completed before this technology can be researched.
    /// </summary>
    [DataField]
    public List<ProtoId<ResearchExperimentPrototype>> RequiredExperiments = [];

    /// <summary>
    /// Experiments that can reduce the research cost for this technology.
    /// </summary>
    [DataField]
    public List<ProtoId<ResearchExperimentPrototype>> DiscountExperiments = [];

    /// <summary>
    /// Per-technology discount values keyed by experiment ID.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ResearchExperimentPrototype>, int> DiscountExperimentCosts = [];

    /// <summary>
    /// Experiments unlocked when this technology is researched.
    /// </summary>
    [DataField]
    public List<ProtoId<ResearchExperimentPrototype>> UnlockedExperiments = [];

    /// <summary>
    /// Should this technology be announced to the station when unlocked.
    /// </summary>
    [DataField]
    public bool AnnounceOnUnlock = true;

    /// <summary>
    /// Radio channels used for unlock announcements when <see cref="AnnounceOnUnlock"/> is enabled.
    /// Falls back to the console announcement channel if empty.
    /// </summary>
    [DataField]
    public List<ProtoId<RadioChannelPrototype>> AnnounceChannels = [];

    /// <summary>
    /// Infrastructure categories unlocked by this technology.
    /// </summary>
    [DataField]
    public List<string> InfrastructureUnlocks = [];

    /// <summary>
    /// Additional reveal/discovery requirements for hidden technologies.
    /// </summary>
    [DataField]
    public List<TechnologyRevealRequirement> RevealRequirements = [];
    // Nebula end

    /// <summary>
    /// A list of <see cref="LatheRecipePrototype"/>s that are unlocked by this technology
    /// </summary>
    [DataField]
    public List<ProtoId<LatheRecipePrototype>> RecipeUnlocks = new();

    /// <summary>
    /// A list of non-standard effects that are done when this technology is unlocked.
    /// </summary>
    [DataField]
    public IReadOnlyList<GenericUnlock> GenericUnlocks = new List<GenericUnlock>();

    // Nebula start
    /// <summary>
    /// Item prototypes that advance this technology's discovery when inserted into a research machine.
    /// </summary>
    [DataField]
    public List<string> ItemUnlocks = [];

    /// <summary>
    /// Prototypes that advance this technology's discovery when deconstructed.
    /// </summary>
    [DataField]
    public List<string> DeconstructionUnlocks = [];

    /// <summary>
    /// Required item prototype paths to reveal or unlock this technology.
    /// </summary>
    [DataField]
    public List<string> RequiredItemsToUnlock = [];
    // Nebula end

    /// <summary>
    /// Goobstation RnD console rework field
    /// Position of this tech in console menu
    /// </summary>
    [DataField(required: true)]
    public Vector2i Position { get; private set; }

    // Nebula start
    public IEnumerable<ProtoId<TechnologyPrototype>> AllRequiredTechnologies => TechnologyPrerequisites;
    // Nebula end
}

// Nebula start
[Serializable, NetSerializable]
public enum TechnologyRevealRequirementKind : byte
{
    RevealedTechnology,
    ResearchedTechnology,
    CompletedExperiment,
    ScanEntity,
    MachineInsertion,
    DeconstructEntity,
    ServerTrigger,
}

[DataDefinition, Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial record TechnologyRevealRequirement
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField]
    public TechnologyRevealRequirementKind Kind;

    [DataField]
    public int Target = 1;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record RevealedTechnologyRevealRequirement : TechnologyRevealRequirement
{
    [DataField(required: true)]
    public ProtoId<TechnologyPrototype> Technology;

    public RevealedTechnologyRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.RevealedTechnology;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ResearchedTechnologyRevealRequirement : TechnologyRevealRequirement
{
    [DataField(required: true)]
    public ProtoId<TechnologyPrototype> Technology;

    public ResearchedTechnologyRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.ResearchedTechnology;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record CompletedExperimentRevealRequirement : TechnologyRevealRequirement
{
    [DataField(required: true)]
    public ProtoId<ResearchExperimentPrototype> Experiment;

    public CompletedExperimentRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.CompletedExperiment;
    }
}

[DataDefinition, Serializable, NetSerializable]
public partial record ScanEntityRevealRequirement : TechnologyRevealRequirement
{
    [DataField]
    public string? RequiredEntityPrototype;

    [DataField]
    public List<string> RequiredTags = [];

    public ScanEntityRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.ScanEntity;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record MachineInsertionRevealRequirement : ScanEntityRevealRequirement
{
    [DataField]
    public string? RequiredMachinePrototype;

    public MachineInsertionRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.MachineInsertion;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record DeconstructEntityRevealRequirement : TechnologyRevealRequirement
{
    [DataField]
    public string? RequiredEntityPrototype;

    [DataField]
    public List<string> RequiredTags = [];

    public DeconstructEntityRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.DeconstructEntity;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ServerTriggerRevealRequirement : TechnologyRevealRequirement
{
    [DataField(required: true)]
    public string TriggerId = string.Empty;

    public ServerTriggerRevealRequirement()
    {
        Kind = TechnologyRevealRequirementKind.ServerTrigger;
    }
}
// Nebula end

[DataDefinition]
public partial record struct GenericUnlock()
{
    /// <summary>
    /// What event is raised when this is unlocked?
    /// Used for doing non-standard logic.
    /// </summary>
    [DataField]
    public object? PurchaseEvent = null;

    /// <summary>
    /// A player facing tooltip for what the unlock does.
    /// Supports locale strings.
    /// </summary>
    [DataField]
    public string UnlockDescription = string.Empty;
}
