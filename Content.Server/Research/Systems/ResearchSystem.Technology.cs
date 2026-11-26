using Content.Shared.Database;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
// Nebula start
using System.Linq;
using Content.Shared.Research;
// Nebula end

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("research.tech-web"); // Nebula edit

    /// <summary>
    /// Syncs the primary entity's database to that of the secondary entity's database.
    /// </summary>
    private void Sync(EntityUid primaryUid, EntityUid otherUid, TechnologyDatabaseComponent? primaryDb = null, TechnologyDatabaseComponent? otherDb = null) // Nebula edit
    {
        if (!Resolve(primaryUid, ref primaryDb) || !Resolve(otherUid, ref otherDb))
            return;

        primaryDb.MainDiscipline = otherDb.MainDiscipline;
        // Nebula start
        primaryDb.CurrentTechnologyCards = new List<string>(otherDb.CurrentTechnologyCards);
        primaryDb.SupportedDisciplines = new List<ProtoId<TechDisciplinePrototype>>(otherDb.SupportedDisciplines);
        // Nebula end
        // Nebula start
        primaryDb.VisibleTechnologies = new List<ProtoId<TechnologyPrototype>>(otherDb.VisibleTechnologies);
        primaryDb.AvailableTechnologies = new List<ProtoId<TechnologyPrototype>>(otherDb.AvailableTechnologies);
        primaryDb.ResearchedTechnologies = new List<ProtoId<TechnologyPrototype>>(otherDb.ResearchedTechnologies);
        primaryDb.AvailableExperiments = new List<string>(otherDb.AvailableExperiments);
        primaryDb.UnlockedExperiments = new List<string>(otherDb.UnlockedExperiments);
        primaryDb.ActiveExperiments = new List<string>(otherDb.ActiveExperiments);
        primaryDb.CompletedExperiments = new List<string>(otherDb.CompletedExperiments);
        primaryDb.SkippedExperiments = new List<string>(otherDb.SkippedExperiments);
        primaryDb.ExperimentProgress = otherDb.ExperimentProgress
            .Select(CloneExperimentProgress)
            .ToList();
        // Nebula end
        primaryDb.UnlockedRecipes = new List<ProtoId<LatheRecipePrototype>>(otherDb.UnlockedRecipes); // Nebula edit
        // Nebula start
        primaryDb.IndependentRecipes = new List<ProtoId<LatheRecipePrototype>>(otherDb.IndependentRecipes);
        primaryDb.RevealedTechnologies = new List<ProtoId<TechnologyPrototype>>(otherDb.RevealedTechnologies);
        primaryDb.DiscoveryProgress = new List<TechnologyDiscoveryProgress>(otherDb.DiscoveryProgress);
        primaryDb.UnlockedInfrastructure = new List<string>(otherDb.UnlockedInfrastructure);
        // Nebula end

        Dirty(primaryUid, primaryDb);

        var ev = new TechnologyDatabaseSynchronizedEvent();
        RaiseLocalEvent(primaryUid, ref ev);
    }

    /// <summary>
    ///     If there's a research client component attached to the owner entity,
    ///     and the research client is connected to a research server, this method
    ///     syncs against the research server, and the server against the local database.
    /// </summary>
    /// <returns>Whether it could sync or not</returns>
    private void SyncClientWithServer(EntityUid uid, TechnologyDatabaseComponent? databaseComponent = null, ResearchClientComponent? clientComponent = null) // Nebula edit
    {
        if (!Resolve(uid, ref databaseComponent, ref clientComponent, false))
            return;

        if (clientComponent.Server is not { } serverUid) // Nebula edit
            return;

        // Nebula start
        if (!TryComp(serverUid, out ResearchServerComponent? serverComponent))
            return;

        var authorityUid = GetNetworkAuthority(serverUid, serverComponent);
        if (authorityUid != serverUid)
        {
            UnregisterClient(uid, serverUid, clientComponent, serverComponent, dirtyServer: false);
            RegisterClient(uid, authorityUid, clientComponent, dirtyServer: false);
            return;
        }

        if (!TryComp<TechnologyDatabaseComponent>(serverUid, out var serverDatabase))
            return;
        // Nebula end

        Sync(uid, serverUid, databaseComponent, serverDatabase); // Nebula edit
    }

    /// <summary>
    /// Tries to add a technology to a database, checking if it is able to
    /// </summary>
    /// <returns>If the technology was successfully added</returns>
    private bool UnlockTechnology(EntityUid client, // Nebula edit
        string prototypeid,
        EntityUid user,
        ResearchClientComponent? component = null,
        TechnologyDatabaseComponent? clientDatabase = null)
    {
        if (!ProtoMan.TryIndex<TechnologyPrototype>(prototypeid, out var prototype))
            return false;

        return UnlockTechnology(client, prototype, user, component, clientDatabase);
    }

    /// <summary>
    /// Tries to add a technology to a database, checking if it is able to
    /// </summary>
    /// <returns>If the technology was successfully added</returns>
    private bool UnlockTechnology(EntityUid client, // Nebula edit
        TechnologyPrototype prototype,
        EntityUid user,
        ResearchClientComponent? component = null,
        TechnologyDatabaseComponent? clientDatabase = null)
    {
        if (!Resolve(client, ref component, ref clientDatabase, false))
            return false;

        if (!TryGetClientServer(client, out var serverEnt, out _, component))
            return false;

        if (!CanServerUnlockTechnology(client, prototype, out var finalCosts, clientDatabase, component)) // Nebula edit
            return false;

        AddTechnology(serverEnt.Value, prototype);
        //TrySetMainDiscipline(prototype, serverEnt.Value); // Goobstation commented
        TryConsumePoints(serverEnt.Value, finalCosts); // Nebula edit
        UpdateTechnologyCards(serverEnt.Value);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} unlocked {prototype.ID} (discipline: {prototype.Discipline}, tier: {prototype.Tier}) at {ToPrettyString(client)}, for server {ToPrettyString(serverEnt.Value)}.");
        LogNetworkEvent(serverEnt.Value, "technology", Loc.GetString("research-netlog-technology-unlocked", ("technology", Loc.GetString(prototype.Name)), ("user", GetResearchLogUserName(user))), user); // Nebula edit
        return true;
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    [PublicAPI]
    public void AddTechnology(EntityUid uid, string technology, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!ProtoMan.TryIndex<TechnologyPrototype>(technology, out var prototype))
            return;
        AddTechnology(uid, prototype, component);
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    public void AddTechnology(EntityUid uid, TechnologyPrototype technology, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        //todo this needs to support some other stuff, too
        foreach (var generic in technology.GenericUnlocks)
        {
            if (generic.PurchaseEvent != null)
                RaiseLocalEvent(generic.PurchaseEvent);
        }

        // Nebula start
        var addedRecipes = technology.RecipeUnlocks
            .Select(recipe => recipe.Id)
            .Where(recipe => !component.UnlockedRecipes.Contains(recipe))
            .ToList();

        if (!component.ResearchedTechnologies.Contains(technology.ID))
            component.ResearchedTechnologies.Add(technology.ID);

        foreach (var experiment in technology.UnlockedExperiments)
        {
            if (component.CompletedExperiments.Contains(experiment) || component.SkippedExperiments.Contains(experiment))
                continue;

            if (!component.AvailableExperiments.Contains(experiment))
                component.AvailableExperiments.Add(experiment);
        }
        // Nebula end

        // Nebula start
        foreach (var infrastructure in technology.InfrastructureUnlocks)
        {
            if (!component.UnlockedInfrastructure.Contains(infrastructure))
                component.UnlockedInfrastructure.Add(infrastructure);
        }

        RecalculateTechnologyState(uid, component);
        // Nebula end
        Dirty(uid, component);

        var ev = new TechnologyDatabaseModifiedEvent(addedRecipes);
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    ///     Returns whether a technology can be unlocked on this database,
    ///     taking parent technologies into account.
    /// </summary>
    /// <returns>Whether it could be unlocked or not</returns>
    private bool CanServerUnlockTechnology(EntityUid uid, TechnologyPrototype technology, out List<ResearchPointAmount> finalCosts, TechnologyDatabaseComponent? database = null, ResearchClientComponent? client = null) // Nebula edit
    {
        finalCosts = []; // Nebula edit

        if (!Resolve(uid, ref client, ref database, false))
            return false;

        if (!TryGetClientServer(uid, out var serverUid, out var serverComp, client)) // Nebula edit
            return false;

        // Nebula start
        if (!TryComp<TechnologyDatabaseComponent>(serverUid, out var serverDatabase))
            return false;

        finalCosts = GetTechnologyFinalPointCosts(serverDatabase, technology);

        if (!CanUnlockTechnology(serverDatabase, technology))
            return false;

        return HasSufficientPoints(serverUid.Value, finalCosts, serverComp);
        // Nebula end
    }

    // Nebula start
    public override bool CanUnlockTechnology(TechnologyDatabaseComponent component, TechnologyPrototype tech)
    {
        if (!component.VisibleTechnologies.Contains(tech.ID))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: not visible.");
            return false;
        }

        if (component.ResearchedTechnologies.Contains(tech.ID))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: already researched.");
            return false;
        }

        if (!tech.AllRequiredTechnologies.All(prereq => component.ResearchedTechnologies.Contains(prereq)))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: prerequisites not completed.");
            return false;
        }

        if (!HasRequiredExperiments(component, tech))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: required experiments not completed.");
            return false;
        }

        if (!component.AvailableTechnologies.Contains(tech.ID))
        {
            _sawmill.Debug($"Cannot unlock {tech.ID}: tech is not available.");
            return false;
        }

        return true;
    }
    // Nebula end

    private void OnDatabaseRegistrationChanged(EntityUid uid, TechnologyDatabaseComponent component, ref ResearchRegistrationChangedEvent args)
    {
        if (args.Server != null)
            return;
        component.MainDiscipline = null;
        component.CurrentTechnologyCards = new List<string>();
        component.SupportedDisciplines = new List<ProtoId<TechDisciplinePrototype>>();
        // Nebula start
        component.VisibleTechnologies = [];
        component.AvailableTechnologies = [];
        component.ResearchedTechnologies = [];
        component.AvailableExperiments = [];
        component.UnlockedExperiments = [];
        component.ActiveExperiments = [];
        component.CompletedExperiments = [];
        component.SkippedExperiments = [];
        component.ExperimentProgress = [];
        // Nebula end
        component.UnlockedRecipes = new List<ProtoId<LatheRecipePrototype>>();
        // Nebula start
        component.IndependentRecipes = [];
        component.RevealedTechnologies = [];
        component.DiscoveryProgress = [];
        component.UnlockedInfrastructure = [];
        // Nebula end
        Dirty(uid, component);
    }

    // Nebula start
    private static ResearchExperimentProgress CloneExperimentProgress(ResearchExperimentProgress source)
    {
        return new ResearchExperimentProgress
        {
            ExperimentId = source.ExperimentId,
            Progress = source.Progress,
            Target = source.Target,
            UniqueProgressKeys = new HashSet<string>(source.UniqueProgressKeys),
            ScannedEntities = new HashSet<NetEntity>(source.ScannedEntities),
            CompletedAt = source.CompletedAt,
        };
    }
    // Nebula end
}
