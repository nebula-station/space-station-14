using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared.Research.Components;
// Nebula start
using Content.Shared.Research;
using Content.Shared.Examine;
using Robust.Shared.Timing;
using Content.Shared.Emag.Systems;
// Nebula end

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeServer()
    {
        SubscribeLocalEvent<ResearchServerComponent, ComponentStartup>(OnServerStartup);
        SubscribeLocalEvent<ResearchServerComponent, ComponentShutdown>(OnServerShutdown);
        SubscribeLocalEvent<ResearchServerComponent, TechnologyDatabaseModifiedEvent>(OnServerDatabaseModified);
    }

    private void OnServerStartup(EntityUid uid, ResearchServerComponent component, ComponentStartup args)
    {
        var unusedId = EntityQuery<ResearchServerComponent>(true)
            .Max(s => s.Id) + 1;
        component.Id = unusedId;
        // Nebula start
        EnsurePointBalance(component, "General");
        Dirty(uid, component);
        // Nebula end
    }

    // Nebula start
    [SubscribeLocalEvent] // Nebula edit
    private void OnServerMapInit(EntityUid uid, ResearchServerComponent component, MapInitEvent args)
    {
        AssignServerName(component);
        LogNetworkEvent(uid, "network", Loc.GetString("research-netlog-server-joined", ("server", component.ServerName)));
        Dirty(uid, component);
        ConnectUnregisteredClientsOnServerGrid(uid);
        Timer.Spawn(0, () => SyncServerClients(uid));
    }

    private void SyncServerClients(EntityUid uid)
    {
        if (!TryComp<ResearchServerComponent>(uid, out var server))
            return;

        foreach (var client in server.Clients.ToArray())
        {
            if (TryComp<ResearchClientComponent>(client, out var clientComponent))
                SyncClientWithServer(client, clientComponent: clientComponent);
        }
    }

    private void ConnectUnregisteredClientsOnServerGrid(EntityUid serverUid)
    {
        var serverGrid = Transform(serverUid).GridUid;
        if (serverGrid is null)
            return;

        var query = EntityQueryEnumerator<ResearchClientComponent, TransformComponent>();
        while (query.MoveNext(out var clientUid, out var client, out var xform))
        {
            if (client.Server is not null || xform.GridUid != serverGrid)
                continue;

            TryAutoRegisterClient(clientUid, client);
        }
    }
    // Nebula end

    private void OnServerShutdown(EntityUid uid, ResearchServerComponent component, ComponentShutdown args)
    {
        // Nebula start
        var survivingAuthority = GetNetworkServers(uid, component)
            .Where(s => s != uid)
            .OrderBy(ent => TryComp<ResearchServerComponent>(ent, out var comp) ? comp.Id : int.MaxValue)
            .FirstOrDefault();

        if (survivingAuthority != default)
        {
            LogNetworkEvent(
                survivingAuthority,
                "network",
                Loc.GetString("research-netlog-server-left", ("server", component.ServerName)));
        }
        // Nebula end

        foreach (var client in new List<EntityUid>(component.Clients))
        {
            UnregisterClient(client, uid, serverComponent: component, dirtyServer: false);
        }
    }

    // Nebula start
    private void AssignServerName(ResearchServerComponent component)
    {
        if (!string.IsNullOrWhiteSpace(component.ServerName) && component.ServerName != "RND-Server")
            return;

        const string charset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var chars = new char[6];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = charset[_random.Next(charset.Length)];
        }

        component.ServerName = $"RND-Server {new string(chars)}";
    }
    // Nebula end

    private void OnServerDatabaseModified(EntityUid uid, ResearchServerComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref args);
        }
    }

    // Nebula start
    [SubscribeLocalEvent] // Nebula edit
    private void OnResearchClientEmagged(EntityUid uid, ResearchClientComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        if (!TryGetClientServer(uid, out var serverUid, out _))
            return;

        var intrusionMessage = Loc.GetString("research-netlog-emag-device-interference", ("device", MetaData(uid).EntityName));
        var message = BuildCorruptedMessage(intrusionMessage);
        LogNetworkEvent(serverUid.Value, "security", message);
        args.Handled = true;
    }
    // Nebula end

    private bool CanRun(EntityUid uid)
    {
        return this.IsPowered(uid, EntityManager);
    }

    private void UpdateServer(EntityUid uid, int time, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // Nebula start
        if (GetNetworkAuthority(uid, component) != uid)
            return;
        // Nebula end

        if (!CanRun(uid))
            return;

        // Nebula start
        foreach (var generation in GetPointGenerationPerSecond(uid, component))
        {
            ModifyServerPoints(uid, generation.Type, generation.Amount * time, component);
        }
        // Nebula end
    }

    /// <summary>
    /// Registers a client to the specified server.
    /// </summary>
    /// <param name="client">The client being registered</param>
    /// <param name="server">The server the client is being registered to</param>
    /// <param name="clientComponent"></param>
    /// <param name="serverComponent"></param>
    /// <param name="dirtyServer">Whether to dirty the server component after registration</param>
    private void RegisterClient(EntityUid client, EntityUid server, ResearchClientComponent? clientComponent = null, ResearchServerComponent? serverComponent = null,  bool dirtyServer = true) // Nebula edit
    {
        if (!Resolve(client, ref clientComponent, false) || !Resolve(server, ref serverComponent, false))
            return;

        // Nebula start
        var authorityServer = GetNetworkAuthority(server, serverComponent);
        if (authorityServer != server)
            serverComponent = null;

        server = authorityServer;
        if (!Resolve(server, ref serverComponent, false))
            return;
        // Nebula end

        if (serverComponent.Clients.Contains(client))
            return;

        serverComponent.Clients.Add(client);
        clientComponent.Server = server;
        SyncClientWithServer(client, clientComponent: clientComponent);

        if (dirtyServer && !TerminatingOrDeleted(server))
            Dirty(server, serverComponent);

        var ev = new ResearchRegistrationChangedEvent(server);
        RaiseLocalEvent(client, ref ev);
    }

    /// <summary>
    /// Unregisterse a client from its server
    /// </summary>
    /// <param name="client"></param>
    /// <param name="clientComponent"></param>
    /// <param name="dirtyServer"></param>
    private void UnregisterClient(EntityUid client, ResearchClientComponent? clientComponent = null, bool dirtyServer = true) // Nebula edit
    {
        if (!Resolve(client, ref clientComponent))
            return;

        if (clientComponent.Server is not { } server)
            return;

        UnregisterClient(client, server, clientComponent, dirtyServer: dirtyServer);
    }

    /// <summary>
    /// Unregisters a client from its server
    /// </summary>
    /// <param name="client"></param>
    /// <param name="server"></param>
    /// <param name="clientComponent"></param>
    /// <param name="serverComponent"></param>
    /// <param name="dirtyServer"></param>
    private void UnregisterClient(EntityUid client, EntityUid server, ResearchClientComponent? clientComponent = null, ResearchServerComponent? serverComponent = null, bool dirtyServer = true) // Nebula edit
    {
        if (!Resolve(client, ref clientComponent, false) || !Resolve(server, ref serverComponent, false))
            return;

        serverComponent.Clients.Remove(client);
        clientComponent.Server = null;
        SyncClientWithServer(client, clientComponent: clientComponent);

        if (dirtyServer && !TerminatingOrDeleted(server))
        {
            Dirty(server, serverComponent);
        }

        var ev = new ResearchRegistrationChangedEvent(null);
        RaiseLocalEvent(client, ref ev);
    }

/* // Nebula edit
    /// <summary>
    /// Gets the amount of points generated by all the server's sources in a second.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public int GetPointsPerSecond(EntityUid uid, ResearchServerComponent? component = null)
    {
        var points = 0;

        if (!Resolve(uid, ref component))
            return points;

        if (!CanRun(uid))
            return points;

        var ev = new ResearchServerGetPointsPerSecondEvent(uid, points);
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref ev);
        }
        RaiseLocalEvent(uid, ref ev); // Goobstation: We raise on the server as well in case its working as a point source.
        return ev.Points;
    }
*/

    // Nebula start
    public List<ResearchPointAmount> GetPointGenerationPerSecond(EntityUid uid, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component) || !CanRun(uid))
            return [];

        Dictionary<string, int> generation = [];
        foreach (var networkServer in GetNetworkServers(uid, component))
        {
            // The server's generation switch stops all its periodic points: own sources and every client (anomaly
            // vessels too). One-off rewards such as experiments do not come through here.
            if (TryComp<ResearchServerControlStatusComponent>(networkServer, out var status) && !status.GenerationEnabled)
                continue;

            var ev = new ResearchServerGetPointsPerSecondByTypeEvent(networkServer, new());

            if (TryComp<ResearchServerComponent>(networkServer, out var networkServerComp))
            {
                foreach (var client in networkServerComp.Clients)
                {
                    RaiseLocalEvent(client, ref ev);
                }
            }

            RaiseLocalEvent(networkServer, ref ev);

            foreach (var amount in ev.Points)
            {
                generation.TryAdd(amount.Type, 0);
                generation[amount.Type] += amount.Amount;
            }
        }

        return generation.Select(x => new ResearchPointAmount { Type = x.Key, Amount = x.Value }).ToList();
    }
    // Nebula end

    /// <summary>
    /// Adds a specified number of points to a server.
    /// </summary>
    /// <param name="uid">The server</param>
    /// <param name="points">The amount of points being added</param>
    /// <param name="component"></param>
    public void ModifyServerPoints(EntityUid uid, int points, ResearchServerComponent? component = null)
    // Nebula start
    {
        ModifyServerPoints(uid, "General", points, component);
    }
    // Nebula end

    public void ModifyServerPoints(EntityUid uid, string type, int points, ResearchServerComponent? component = null) // Nebula edit
    {
        if (points == 0)
            return;

        if (!Resolve(uid, ref component))
            return;

        // Nebula start
        var authorityServer = GetNetworkAuthority(uid, component);
        if (authorityServer != uid)
        {
            uid = authorityServer;
            component = null;

            if (!Resolve(uid, ref component, false))
                return;
        }

        EnsurePointBalance(component, type);
        var totalByType = 0;
        for (var i = 0; i < component.PointBalances.Count; i++)
        {
            if (component.PointBalances[i].Type != type)
                continue;

            var balance = component.PointBalances[i];
            balance.Amount += points;
            component.PointBalances[i] = balance;
            totalByType = balance.Amount;
            break;
        }

        component.Points = GetPointBalance(uid, "General", component);
        // Nebula end
        var ev = new ResearchServerPointsChangedEvent(uid, component.Points, points);
        var typeEv = new ResearchServerPointTypeChangedEvent(uid, type, totalByType, points); // Nebula edit
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref ev);
            RaiseLocalEvent(client, ref typeEv); // Nebula edit
        }
        Dirty(uid, component);
    }
    // Nebula start
    private int GetPointBalance(EntityUid uid, string type, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return 0;

        foreach (var balance in component.PointBalances)
        {
            if (balance.Type == type)
                return balance.Amount;
        }

        return 0;
    }

    private bool HasSufficientPoints(EntityUid uid, IEnumerable<ResearchPointAmount> costs, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        var aggregatedCosts = costs
            .GroupBy(cost => cost.Type)
            .Select(group => new ResearchPointAmount
            {
                Type = group.Key,
                Amount = group.Sum(cost => cost.Amount),
            })
            .ToList();

        foreach (var cost in aggregatedCosts)
        {
            if (GetPointBalance(uid, cost.Type, component) < cost.Amount)
                return false;
        }

        return true;
    }

    private void TryConsumePoints(EntityUid uid, IEnumerable<ResearchPointAmount> costs, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var costList = costs
            .GroupBy(cost => cost.Type)
            .Select(group => new ResearchPointAmount
            {
                Type = group.Key,
                Amount = group.Sum(cost => cost.Amount),
            })
            .ToList();

        if (!HasSufficientPoints(uid, costList, component))
            return;

        foreach (var cost in costList)
        {
            ModifyServerPoints(uid, cost.Type, -cost.Amount, component);
        }
    }

    private IEnumerable<EntityUid> GetNetworkServers(EntityUid uid, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return [uid];

        List<EntityUid> servers = [];
        var query = EntityQueryEnumerator<ResearchServerComponent>();
        while (query.MoveNext(out var serverUid, out var serverComp))
        {
            if (serverComp.NetworkId != component.NetworkId)
                continue;

            servers.Add(serverUid);
        }

        return servers;
    }

    private EntityUid GetNetworkAuthority(EntityUid uid, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return uid;

        return GetNetworkServers(uid, component)
            .OrderBy(ent => TryComp<ResearchServerComponent>(ent, out var comp) ? comp.Id : int.MaxValue)
            .FirstOrDefault(uid);
    }

    public string GetResearchLogUserName(EntityUid? user)
    {
        if (user is not { } uid)
            return Loc.GetString("research-netlog-user-system");

        return TryComp(uid, out MetaDataComponent? meta)
            ? meta.EntityName
            : ToPrettyString(uid);
    }

    public void LogNetworkEvent(EntityUid uid, string category, string message, EntityUid? actor = null, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var servers = GetNetworkServers(uid, component).ToList();
        if (servers.Count == 0)
            servers.Add(uid);

        foreach (var serverUid in servers)
        {
            if (!TryComp(serverUid, out ResearchServerComponent? serverComponent))
                continue;

            serverComponent.Logs.Add(new ResearchLogEntry
            {
                Timestamp = _timing.CurTime,
                Category = category,
                Message = message,
                Actor = actor.HasValue ? GetNetEntity(actor.Value) : null,
            });

            if (serverComponent.Logs.Count > 100)
                serverComponent.Logs.RemoveAt(0);

            Dirty(serverUid, serverComponent);
        }
    }

    private static void EnsurePointBalance(ResearchServerComponent component, string type)
    {
        if (component.PointBalances.Any(balance => balance.Type == type))
            return;

        component.PointBalances.Add(new ResearchPointAmount
        {
            Type = type,
            Amount = 0,
        });
    }

    [SubscribeLocalEvent]
    private void OnServerExamined(EntityUid uid, ResearchServerComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var generation = GetPointGenerationPerSecond(uid, component);
        var points = generation.Sum(x => x.Amount);
        var typePoints = string.Join(", ",
            generation
                .OrderBy(x => x.Type)
                .Select(x => Loc.GetString("research-server-examine-type-entry",
                    ("type", LocalizePointType(x.Type)),
                    ("amount", x.Amount))));

        var msg = Loc.GetString("research-server-examine",
            ("name", component.ServerName),
            ("points", points));

        if (!string.IsNullOrEmpty(typePoints))
            msg += "\n" + typePoints;

        args.PushMarkup(msg);
    }

    private string LocalizePointType(string type)
    {
        var key = $"research-point-type-{type.ToLowerInvariant()}";
        return Loc.TryGetString(key, out var localized) ? localized : type;
    }
    // Nebula end
}
