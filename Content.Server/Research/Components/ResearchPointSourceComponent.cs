namespace Content.Server.Research.Components;

[RegisterComponent]
public sealed partial class ResearchPointSourceComponent : Component
{
    // Nebula start
    [DataField]
    public string PointType = "General";
    // Nebula end

    [DataField("pointspersecond"), ViewVariables(VVAccess.ReadWrite)]
    public int PointsPerSecond;

    // Nebula start
    [DataField]
    public string? RequiredInfrastructure;
    // Nebula end

    [DataField("active"), ViewVariables(VVAccess.ReadWrite)]
    public bool Active;
}
