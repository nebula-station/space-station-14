// <Trauma>
using Content.Goobstation.Common.Pirates;
using Content.Goobstation.Common.Research;
using Robust.Shared.Prototypes;
using System.Linq;
// </Trauma>
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private IdentitySystem _identity = default!;

    private void InitializeConsole()
    {
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleUnlockTechnologyMessage>(OnConsoleUnlock);
        SubscribeLocalEvent<ResearchConsoleComponent, BeforeActivatableUIOpenEvent>(OnConsoleBeforeUiOpened);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchRegistrationChangedEvent>(OnConsoleRegistrationChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseModifiedEvent>(OnConsoleDatabaseModified);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseSynchronizedEvent>(OnConsoleDatabaseSynchronized);
//        SubscribeLocalEvent<ResearchConsoleComponent, GotEmaggedEvent>(OnEmagged); // Nebula edit
    }

    private void OnConsoleUnlock(EntityUid uid, ResearchConsoleComponent component, ConsoleUnlockTechnologyMessage args)
    {
        // goob edit - spirates
        var eqe = EntityQueryEnumerator<ResourceSiphonComponent>();
        while (eqe.MoveNext(out var siphon))
        {
            if (siphon.Active)
            {
                _popup.PopupEntity(Loc.GetString("console-block-something"), args.Actor);
                return;
            }
        }
        // goob edit end

        var act = args.Actor;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!ProtoMan.TryIndex<TechnologyPrototype>(args.Id, out var technologyPrototype))
            return;

        if (TryComp<AccessReaderComponent>(uid, out var access) && !_accessReader.IsAllowed(act, uid, access))
        {
            _popup.PopupEntity(Loc.GetString("research-console-no-access-popup"), act);
            return;
        }

        CanServerUnlockTechnology(uid, technologyPrototype, out var paidCosts); // Nebula edit

        if (!UnlockTechnology(uid, args.Id, act))
        // Nebula start
        {
            _popup.PopupEntity(Loc.GetString("research-console-unlock-failed-popup"), act);
            return;
        }
        // Nebula end

        if (!_emag.CheckFlag(uid, EmagType.Interaction) && technologyPrototype.AnnounceOnUnlock) // Nebula edit
        {
            var costText = FormatResearchPointAmounts(paidCosts); // Nebula edit
            var message = Loc.GetString(
                "research-console-unlock-technology-radio-broadcast",
                ("technology", Loc.GetString(technologyPrototype.Name)),
                ("amount", costText), // Nebula edit
                ("approver", _identity.GetIdentityShortInfo(act, uid) ?? string.Empty)
            );
            // Nebula start
            var announceChannels = technologyPrototype.AnnounceChannels.Count > 0
                ? technologyPrototype.AnnounceChannels
                : [component.AnnouncementChannel];

            foreach (var channel in announceChannels)
            {
                _radio.SendRadioMessage(uid, message, channel, uid, escapeMarkup: false);
            }
            // Nebula end
            SayTechnologyUnlocked(uid, technologyPrototype, costText, _identity.GetIdentityShortInfo(act, uid) ?? string.Empty); // Nebula edit
        }

        SyncClientWithServer(uid); // Nebula edit
    }

    private void OnConsoleBeforeUiOpened(EntityUid uid, ResearchConsoleComponent component, BeforeActivatableUIOpenEvent args)
    {
        SyncClientWithServer(uid);
    }

    private void UpdateConsoleInterface(EntityUid uid, ResearchConsoleComponent? component = null, ResearchClientComponent? clientComponent = null)
    {
        if (!Resolve(uid, ref component, ref clientComponent, false))
            return;

        // R&D Console Rework Start
        // var allTechs = ProtoMan.EnumeratePrototypes<TechnologyPrototype>().ToList(); // Nebula edit
        Dictionary<string, ResearchAvailability> techList;
        // Nebula start
        Dictionary<string, ResearchTechnologyLockReason> lockReasons;
        List<ResearchConsoleExperimentData> experiments;
        var networkId = string.Empty;
        List<ResearchPointAmount> pointBalances = [];
        List<ResearchLogEntry> logs = [];
        TechnologyDatabaseComponent? syncedDb = null;
        // Nebula end
        var points = 0;

        if (TryGetClientServer(uid, out var serverUid, out var server, clientComponent) &&
            TryComp<TechnologyDatabaseComponent>(serverUid, out var db))
        {
/* // Nebula edit
            var unlockedTechs = new HashSet<ProtoId<TechnologyPrototype>>(db.UnlockedTechnologies);
            techList = allTechs.ToDictionary(
                proto => proto.ID,
                proto =>
*/
            // Nebula start
            syncedDb = db;
            networkId = server.NetworkId;
            pointBalances = server.PointBalances.ToList();
            logs = server.Logs.ToList();
            var visible = new HashSet<ProtoId<TechnologyPrototype>>(db.VisibleTechnologies);
            var available = new HashSet<ProtoId<TechnologyPrototype>>(db.AvailableTechnologies);
            var researched = new HashSet<ProtoId<TechnologyPrototype>>(db.ResearchedTechnologies);

            techList = visible.ToDictionary(
                techId => techId.ToString(),
                techId =>
            // Nebula end
                {
                    if (researched.Contains(techId)) // Nebula edit
                        return ResearchAvailability.Researched;

/* // Nebula edit
                    var prereqsMet = proto.TechnologyPrerequisites.All(p => unlockedTechs.Contains(p));
                    var canAfford = server.Points >= proto.Cost;
*/

                    // Nebula start
                    if (!available.Contains(techId))
                        return ResearchAvailability.PrereqsMet;

                    var proto = ProtoMan.Index(techId);
                    var allCosts = GetTechnologyFinalPointCosts(db, proto);
                    var canAfford = HasSufficientPoints(serverUid.Value, allCosts, server);
                    // Nebula end

                    return canAfford // Nebula edit
                        ? ResearchAvailability.Available // Nebula edit
                        : ResearchAvailability.Unavailable;
                });

            // Nebula start
            lockReasons = ProtoMan.EnumeratePrototypes<TechnologyPrototype>()
                .Where(proto => db.SupportedDisciplines.Contains(proto.Discipline))
                .ToDictionary(proto => proto.ID,
                    proto =>
                {
                    var reason = GetTechnologyLockReason(db, proto);
                    if (reason != ResearchTechnologyLockReason.None)
                        return reason;

                    var costs = GetTechnologyFinalPointCosts(db, proto);

                    if (!HasSufficientPoints(serverUid.Value, costs, server) && !db.ResearchedTechnologies.Contains(proto.ID))
                        return ResearchTechnologyLockReason.InsufficientPoints;

                    return reason;
                });

            experiments = BuildExperimentUiData(db);
            // Nebula end

            if (clientComponent != null)
                points = clientComponent.ConnectedToServer ? server.Points : 0;
        }
        else
        {
            techList = []; // Nebula edit
            // Nebula start
            lockReasons = [];
            experiments = [];
            // Nebula end
        }

        // Nebula start
        _uiSystem.SetUiState(uid,
            ResearchConsoleUiKey.Key,
            new ResearchConsoleBoundInterfaceState(
                points,
                techList,
                syncedDb?.VisibleTechnologies.ToList() ?? [],
                syncedDb?.AvailableTechnologies.ToList() ?? [],
                syncedDb?.ResearchedTechnologies.ToList() ?? [],
                syncedDb?.CompletedExperiments.ToList() ?? [],
                experiments,
                lockReasons,
                networkId,
                pointBalances,
                logs));
        // Nebula end
        // R&D Console Rework End
    }

    // Nebula start
    private List<ResearchConsoleExperimentData> BuildExperimentUiData(TechnologyDatabaseComponent database)
    {
        List<ResearchConsoleExperimentData> data = [];
        foreach (var experiment in ProtoMan.EnumeratePrototypes<ResearchExperimentPrototype>())
        {
            if (experiment.Hidden)
                continue;

            var progress = database.ExperimentProgress.FirstOrDefault(p => p.ExperimentId == experiment.ID);
            var state = ResearchExperimentState.Unavailable;

            if (database.SkippedExperiments.Contains(experiment.ID))
                state = ResearchExperimentState.Skipped;
            else if (database.CompletedExperiments.Contains(experiment.ID))
                state = ResearchExperimentState.Completed;
            else if (database.ActiveExperiments.Contains(experiment.ID))
                state = ResearchExperimentState.Active;
            else if (database.AvailableExperiments.Contains(experiment.ID))
                state = ResearchExperimentState.Available;

            if (state == ResearchExperimentState.Unavailable)
                continue;

            var target = progress.Target > 0 ? progress.Target : Math.Max(1, experiment.Objective.Target);
            data.Add(new ResearchConsoleExperimentData(experiment.ID, progress.Progress, target, state));
        }

        return data.OrderByDescending(e => e.State == ResearchExperimentState.Active)
            .ThenBy(e => e.Id)
            .ToList();
    }
    // Nebula end

    private void OnPointsChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchServerPointsChangedEvent args)
    {
        if (!_uiSystem.IsUiOpen(uid, ResearchConsoleUiKey.Key))
            return;
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleRegistrationChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchRegistrationChangedEvent args)
    {
        SyncClientWithServer(uid);

        // Nebula start
        if (CompOrNull<ResearchClientComponent>(uid)?.Server == null)
            UpdateConsoleInterface(uid, component);
        // Nebula end
    }

    private void OnConsoleDatabaseModified(EntityUid uid, ResearchConsoleComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        // Nebula start
        // TechnologyDatabaseSynchronizedEvent, which updates the interface; doing both here synced twice.
        // SyncClientWithServer(uid);
        // UpdateConsoleInterface(uid, component);
        // Nebula end
    }

    private void OnConsoleDatabaseSynchronized(EntityUid uid, ResearchConsoleComponent component, ref TechnologyDatabaseSynchronizedEvent args)
    {
        UpdateConsoleInterface(uid, component);
    }

/* // Nebula edit
    private void OnEmagged(Entity<ResearchConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }
*/

    // Nebula start
    private static string BuildCorruptedMessage(string message)
    {
        const string symbols = "!@#$%^&*?№";
        var chars = message.ToCharArray();
        var replaced = 0;

        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetter(chars[i]) || Random.Shared.NextDouble() > 0.24)
                continue;

            chars[i] = symbols[Random.Shared.Next(symbols.Length)];
            replaced++;
        }

        if (replaced != 0)
            return new string(chars);
        {
            for (var i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetter(chars[i]))
                    continue;

                chars[i] = symbols[Random.Shared.Next(symbols.Length)];
                break;
            }
        }

        return new string(chars);
    }
    // Nebula end
}
