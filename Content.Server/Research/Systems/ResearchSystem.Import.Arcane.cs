// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Server/Research/Systems/ResearchSystem.Import.Arcane.cs

using System.Linq;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    /// <summary>
    /// Imports researched technologies directly into the database without triggering per-technology unlock effects.
    /// Intended for snapshot-style data transfers such as research data disks.
    /// </summary>
    public int ImportTechnologySnapshot(EntityUid uid,
        IEnumerable<string> technologies,
        IEnumerable<string>? independentRecipes = null,
        TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(uid, ref database, false))
            return 0;

        var imported = 0;
        foreach (var technologyId in technologies)
        {
            if (!ProtoMan.TryIndex<TechnologyPrototype>(technologyId, out _))
                continue;

            if (database.ResearchedTechnologies.Contains(technologyId))
                continue;

            database.ResearchedTechnologies.Add(technologyId);
            imported++;
        }

        foreach (var recipeId in independentRecipes ?? [])
        {
            if (!ProtoMan.HasIndex<LatheRecipePrototype>(recipeId) || database.IndependentRecipes.Contains(recipeId))
                continue;

            database.IndependentRecipes.Add(recipeId);
            imported++;
        }

        if (imported == 0)
            return 0;

        var previouslyUnlockedRecipes = database.UnlockedRecipes.ToHashSet();
        RecalculateTechnologyState(uid, database);
        UpdateTechnologyCards(uid, database);
        Dirty(uid, database);

        // One bulk notification so connected lathes pick the recipes up, without per-technology unlock rewards.
        var newlyUnlockedRecipes = database.UnlockedRecipes.Except(previouslyUnlockedRecipes).Select(recipe => recipe.Id).ToList();
        var ev = new TechnologyDatabaseModifiedEvent(newlyUnlockedRecipes);
        RaiseLocalEvent(uid, ref ev);
        return imported;
    }

    /// <inheritdoc/>
    public override EntityUid GetEffectiveServer(EntityUid server)
    {
        return GetNetworkAuthority(server);
    }

    /// <summary>
    /// The server a research machine works with: its selected one or, deselected, the first on its grid, resolved to
    /// the network's authority, so progress, points and what the machine shows all belong to one database.
    /// </summary>
    public bool TryResolveMachineServer(EntityUid machine, out EntityUid server)
    {
        server = EntityUid.Invalid;
        if (TryComp<ResearchClientComponent>(machine, out var client) && client.Server is { } selected)
        {
            server = GetNetworkAuthority(selected);
            return true;
        }

        var fallback = GetServers(machine).OrderBy(s => s.Comp.Id).FirstOrDefault();
        if (fallback.Owner == EntityUid.Invalid)
            return false;

        server = GetNetworkAuthority(fallback.Owner);
        return true;
    }

    /// <summary>
    /// The server whose database a research network actually uses: clients and points go to it, so snapshots
    /// are read from and written to it too.
    /// </summary>
    public bool TryGetNetworkDatabase(EntityUid serverUid,
        out Entity<ResearchServerComponent, TechnologyDatabaseComponent> authority)
    {
        authority = default;
        if (!TryComp<ResearchServerComponent>(serverUid, out var server))
            return false;

        var authorityUid = GetNetworkAuthority(serverUid, server);
        if (!TryComp<ResearchServerComponent>(authorityUid, out var authorityServer) ||
            !TryComp<TechnologyDatabaseComponent>(authorityUid, out var database))
            return false;

        authority = (authorityUid, authorityServer, database);
        return true;
    }
}
