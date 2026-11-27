// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/02b6d617ada28d2a78cfd227b9860838fb082909/Content.Server/Research/Systems/ResearchSystem.Commands.Arcane.cs

using System.Linq;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private string GetLocalizedPointType(string type)
    {
        return LocalizeResearchPointType(type);
    }

    public int UnlockAllTechnologiesOnServer(EntityUid uid, ResearchServerComponent? server = null, TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(uid, ref server, ref database, false))
            return 0;

        var authority = GetNetworkAuthority(uid, server);
        if (authority != uid)
        {
            uid = authority;
            server = null;
            database = null;

            if (!Resolve(uid, ref server, ref database, false))
                return 0;
        }

        var unlocked = 0;
        foreach (var technology in ProtoMan.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (!database.SupportedDisciplines.Contains(technology.Discipline))
                database.SupportedDisciplines.Add(technology.Discipline);

            if (!database.RevealedTechnologies.Contains(technology.ID))
                database.RevealedTechnologies.Add(technology.ID);

            if (database.ResearchedTechnologies.Contains(technology.ID))
                continue;

            database.ResearchedTechnologies.Add(technology.ID);
            unlocked++;
        }

        if (unlocked == 0)
            return 0;

        var previouslyUnlockedRecipes = database.UnlockedRecipes.ToHashSet();
        RecalculateTechnologyState(uid, database);
        UpdateTechnologyCards(uid, database);
        Dirty(uid, database);

        var newlyUnlockedRecipes = database.UnlockedRecipes.Except(previouslyUnlockedRecipes).Select(recipe => recipe.Id).ToList();
        var ev = new TechnologyDatabaseModifiedEvent(newlyUnlockedRecipes);
        RaiseLocalEvent(uid, ref ev);

        LogNetworkEvent(uid,
            "admin",
            Loc.GetString("research-netlog-admin-unlocked-all-technologies", ("user", Loc.GetString("research-netlog-user-admin"))),
            null,
            server);

        return unlocked;
    }

    public int AddServerPointsByType(EntityUid uid, string type, int amount, ResearchServerComponent? server = null)
    {
        ModifyServerPoints(uid, type, amount, server);
        var balance = GetPointBalance(uid, type, server);
        var localizedType = GetLocalizedPointType(type);
        LogNetworkEvent(uid,
            "admin",
            Loc.GetString("research-netlog-admin-added-points",
                ("user", Loc.GetString("research-netlog-user-admin")),
                ("type", localizedType),
                ("amount", amount),
                ("balance", balance)),
            null,
            server);
        return balance;
    }
}
