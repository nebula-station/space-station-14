namespace Content.Server.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed partial class DiskConsolePrintingComponent : Component
{
    public TimeSpan FinishTime;

    // Nebula start
    public EntityUid? Actor;

    public EntityUid? Server;

    public int Price;
    // Nebula end
}
