// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Shared._Nebula.CCVar;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.ServerStatus;

namespace Content.Server.Administration;

public sealed partial class ServerApi
{
    private const string RoundStatePath = "/admin_assist/round_state";

    private const int RoundStateMaxTrackedAddresses = 4096;

    private static readonly TimeSpan RoundStateCacheTime = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RoundStateRateLimitPeriod = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RoundStateMainThreadTimeout = TimeSpan.FromSeconds(5);

    private readonly object _roundStateLock = new();
    private readonly Dictionary<IPAddress, int> _roundStateFailures = new();
    private int _roundStateAccepted;
    private long _roundStateWindowStart = Stopwatch.GetTimestamp();
    private RoundStateResponse? _roundStateCache;
    private long _roundStateCachedAt;
    private Task<RoundStateResponse>? _roundStateInFlight;
    private bool _roundStateTokenReuseLogged;

    private void InitializeRoundState()
    {
        _statusHost.AddHandler(async context =>
        {
            if (context.RequestMethod != HttpMethod.Get || context.Url.AbsolutePath != RoundStatePath)
                return false;

            var token = _config.GetCVar(NCCVars.AdminAssistRoundStateToken);
            if (token == string.Empty)
                return false;

            if (token == _token)
            {
                if (!_roundStateTokenReuseLogged)
                {
                    _roundStateTokenReuseLogged = true;
                    _sawmill.Error(
                        $"{NCCVars.AdminAssistRoundStateToken.Name} is the same as the admin API token, the round state API stays off until it is changed.");
                }

                return false;
            }

            context.ResponseHeaders["Cache-Control"] = "no-store";

            var authorized = RoundStateTokenMatches(context, token);
            if (!RoundStateTryCountRequest(authorized ? null : context.RemoteEndPoint.Address))
            {
                await RespondError(
                    context,
                    ErrorCode.BadRequest,
                    HttpStatusCode.TooManyRequests,
                    "Too many requests");
                return true;
            }

            if (!authorized)
            {
                await RespondError(
                    context,
                    ErrorCode.AuthenticationInvalid,
                    HttpStatusCode.Unauthorized,
                    "Authorization is invalid");
                _sawmill.Info($"Unauthorized access attempt to round state API from {context.RemoteEndPoint}");
                return true;
            }

            RoundStateResponse state;
            try
            {
                state = await GetRoundState().WaitAsync(RoundStateMainThreadTimeout);
            }
            catch (TimeoutException)
            {
                await RespondError(
                    context,
                    ErrorCode.BadRequest,
                    HttpStatusCode.ServiceUnavailable,
                    "Server is busy");
                return true;
            }

            await context.RespondJsonAsync(state);
            return true;
        });
    }

    private bool RoundStateTryCountRequest(IPAddress? failedFrom)
    {
        var limit = _config.GetCVar(NCCVars.AdminAssistRoundStateRateLimit);

        lock (_roundStateLock)
        {
            if (Stopwatch.GetElapsedTime(_roundStateWindowStart) > RoundStateRateLimitPeriod)
            {
                _roundStateFailures.Clear();
                _roundStateAccepted = 0;
                _roundStateWindowStart = Stopwatch.GetTimestamp();
            }

            if (failedFrom == null)
                return ++_roundStateAccepted <= limit;

            if (!_roundStateFailures.TryGetValue(failedFrom, out var count) &&
                _roundStateFailures.Count >= RoundStateMaxTrackedAddresses)
            {
                return false;
            }

            if (count >= limit)
                return false;

            _roundStateFailures[failedFrom] = count + 1;
            return true;
        }
    }

    private static bool RoundStateTokenMatches(IStatusHandlerContext context, string token)
    {
        if (!context.RequestHeaders.TryGetValue("Authorization", out var header))
            return false;

        var value = header.ToString();
        var prefix = SS14TokenScheme + " ";
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(value[prefix.Length..].Trim())),
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private Task<RoundStateResponse> GetRoundState()
    {
        lock (_roundStateLock)
        {
            if (_roundStateCache != null && Stopwatch.GetElapsedTime(_roundStateCachedAt) < RoundStateCacheTime)
                return Task.FromResult(_roundStateCache);

            if (_roundStateInFlight is not { IsCompleted: false })
                _roundStateInFlight = RefreshRoundState();

            return _roundStateInFlight;
        }
    }

    private async Task<RoundStateResponse> RefreshRoundState()
    {
        var state = await RunOnMainThread(BuildRoundState);

        lock (_roundStateLock)
        {
            _roundStateCache = state;
            _roundStateCachedAt = Stopwatch.GetTimestamp();
        }

        return state;
    }

    private RoundStateResponse BuildRoundState()
    {
        var ticker = _entitySystemManager.GetEntitySystem<ServerGameTicker>();
        var minds = _entitySystemManager.GetEntitySystem<SharedMindSystem>();
        var roles = _entitySystemManager.GetEntitySystem<SharedRoleSystem>();
        var jobs = _entitySystemManager.GetEntitySystem<SharedJobSystem>();

        var players = new List<RoundStateResponse.Player>();
        foreach (var session in _playerManager.Sessions)
        {
            string? characterName = null;
            string? jobId = null;
            string? jobName = null;
            var antagRoles = new List<RoundStateResponse.Role>();
            var state = "lobby";

            if (minds.TryGetMind(session, out var mindId, out var mind))
            {
                characterName = mind.CharacterName;
                state = GetRoundStateOfBody(mind.OwnedEntity);

                if (jobs.MindTryGetJob(mindId, out var job))
                {
                    jobId = job.ID;
                    jobName = job.LocalizedName;
                }

                foreach (var role in roles.MindGetAllRoleInfo((mindId, mind)))
                {
                    if (!role.Antagonist)
                        continue;

                    antagRoles.Add(new RoundStateResponse.Role
                    {
                        Id = role.Prototype,
                        Name = _loc.GetString(role.Name),
                    });
                }
            }

            players.Add(new RoundStateResponse.Player
            {
                UserId = session.UserId.UserId,
                Username = session.Name,
                CharacterName = characterName,
                JobId = jobId,
                JobName = jobName,
                AntagRoles = antagRoles,
                State = state,
                IsAdmin = _adminManager.GetAdminData(session)?.HasFlag(AdminFlags.Adminhelp) ?? false,
            });
        }

        var map = _gameMapManager.GetSelectedMap();
        return new RoundStateResponse
        {
            ServerName = _config.GetCVar(CCVars.AdminLogsServerName),
            RoundId = ticker.RoundDatabaseId,
            RoundNumber = ticker.RoundId,
            RunLevel = ticker.RunLevel.ToString(),
            RoundDurationSeconds = (int) ticker.RoundDuration().TotalSeconds,
            GamePreset = ticker.CurrentPreset?.ID,
            MapId = map?.ID,
            MapName = map?.MapName,
            PlayerCount = players.Count,
            Players = players,
        };
    }

    private string GetRoundStateOfBody(EntityUid? body)
    {
        if (body is not { } uid || !_entityManager.EntityExists(uid) || _entityManager.HasComponent<GhostComponent>(uid))
            return "ghost";

        if (!_entityManager.TryGetComponent(uid, out MobStateComponent? mobState))
            return "alive";

        return mobState.CurrentState switch
        {
            MobState.Dead => "dead",
            MobState.Critical or MobState.SoftCrit => "crit",
            _ => "alive",
        };
    }

    private sealed class RoundStateResponse
    {
        public required string ServerName { get; init; }

        public required int RoundId { get; init; }

        public required int RoundNumber { get; init; }

        public required string RunLevel { get; init; }
        public required int RoundDurationSeconds { get; init; }
        public required string? GamePreset { get; init; }
        public required string? MapId { get; init; }
        public required string? MapName { get; init; }
        public required int PlayerCount { get; init; }
        public required List<Player> Players { get; init; }

        public sealed class Player
        {
            public required Guid UserId { get; init; }
            public required string Username { get; init; }
            public required string? CharacterName { get; init; }
            public required string? JobId { get; init; }
            public required string? JobName { get; init; }
            public required List<Role> AntagRoles { get; init; }
            public required string State { get; init; }
            public required bool IsAdmin { get; init; }
        }

        public sealed class Role
        {
            public required string Id { get; init; }
            public required string Name { get; init; }
        }
    }
}
