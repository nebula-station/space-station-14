using Content.Server.Research.Systems;
using Content.Server.Research.TechnologyDisk.Components;
using Content.Shared.UserInterface;
using Content.Shared.Research;
using Content.Shared.Research.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
// Nebula start
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
// Nebula end

namespace Content.Server.Research.TechnologyDisk.Systems;

public sealed partial class DiskConsoleSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    // Nebula start
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    // Nebula end

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DiskConsoleComponent, DiskConsolePrintDiskMessage>(OnPrintDisk);
        SubscribeLocalEvent<DiskConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<DiskConsoleComponent, ResearchRegistrationChangedEvent>(OnRegistrationChanged);
        SubscribeLocalEvent<DiskConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);

        SubscribeLocalEvent<DiskConsolePrintingComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DiskConsolePrintingComponent, DiskConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var printing, out var console, out var xform))
        {
            if (printing.FinishTime > _timing.CurTime)
                continue;

            // Nebula start
            var disk = SpawnAtPosition(console.DiskPrototype, xform.Coordinates);
            if (printing.Actor is { } actor && !TerminatingOrDeleted(actor))
            {
                if (!_hands.TryPickupAnyHand(actor, disk))
                    _popup.PopupEntity(Loc.GetString("research-disk-terminal-print-complete"), actor, actor);
            }

            if (printing.Server is { } server)
                _research.LogNetworkEvent(server, "disk", Loc.GetString("research-netlog-disk-printed", ("points", printing.Price), ("user", _research.GetResearchLogUserName(printing.Actor))), printing.Actor);
            // Nebula end

            RemComp(uid, printing);
//            Spawn(console.DiskPrototype, xform.Coordinates); // Nebula edit
        }
    }

    private void OnPrintDisk(EntityUid uid, DiskConsoleComponent component, DiskConsolePrintDiskMessage args)
    {
        if (HasComp<DiskConsolePrintingComponent>(uid))
            return;

        if (!_research.TryGetClientServer(uid, out var server, out var serverComp))
            return;

        if (serverComp.Points < component.PricePerDisk)
            return;

        _research.ModifyServerPoints(server.Value, -component.PricePerDisk, serverComp);
        _research.LogNetworkEvent(server.Value, "disk", Loc.GetString("research-netlog-disk-printing-started", ("points", component.PricePerDisk), ("user", _research.GetResearchLogUserName(args.Actor))), args.Actor, serverComp); // Nebula edit
        _audio.PlayPvs(component.PrintSound, uid);

        var printing = EnsureComp<DiskConsolePrintingComponent>(uid);
        printing.FinishTime = _timing.CurTime + component.PrintDuration;
        // Nebula start
        printing.Actor = args.Actor;
        printing.Server = server.Value;
        printing.Price = component.PricePerDisk;
        // Nebula end
        UpdateUserInterface(uid, component);
    }

    private void OnPointsChanged(EntityUid uid, DiskConsoleComponent component, ref ResearchServerPointsChangedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnRegistrationChanged(EntityUid uid, DiskConsoleComponent component, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnBeforeUiOpen(EntityUid uid, DiskConsoleComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    public void UpdateUserInterface(EntityUid uid, DiskConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var totalPoints = 0;
        if (_research.TryGetClientServer(uid, out _, out var server))
        {
            totalPoints = server.Points;
        }

        var canPrint = !(TryComp<DiskConsolePrintingComponent>(uid, out var printing) && printing.FinishTime >= _timing.CurTime) &&
                       totalPoints >= component.PricePerDisk;

        var state = new DiskConsoleBoundUserInterfaceState(totalPoints, component.PricePerDisk, canPrint);
        _ui.SetUiState(uid, DiskConsoleUiKey.Key, state);
    }

    private void OnShutdown(EntityUid uid, DiskConsolePrintingComponent component, ComponentShutdown args)
    {
        UpdateUserInterface(uid);
    }
}
