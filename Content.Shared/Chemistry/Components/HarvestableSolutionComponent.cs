using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Allows a solution on an entity to be transferred into a held refillable container.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(HarvestableSolutionSystem))]
[AutoGenerateComponentState] // Nebula edit
public sealed partial class HarvestableSolutionComponent : Component
{
    // Nebula edit start
    /// <summary>
    /// The name of the solution to harvest.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string SolutionName = "default";

    /// <summary>
    /// How long harvesting takes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public LocId VerbText = "harvestable-solution-verb";

    [DataField, AutoNetworkedField]
    public SpriteSpecifier VerbIcon =
        new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/spill.svg.192dpi.png"));

    /// <summary>
    /// Popup shown when the source has no harvestable solution available.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId EmptyMessage = "harvestable-solution-empty";

    /// <summary>
    /// Popup shown when the target container cannot accept more solution.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId TargetFullMessage = "harvestable-solution-target-full";

    /// <summary>
    /// Popup shown after solution is successfully transferred.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId SuccessMessage = "harvestable-solution-success";
    // Nebula edit end
}
