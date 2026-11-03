// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Content.Shared._Nebula.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.Administration;

public sealed partial class NebulaServerApi : IPostInjectInit
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IGameTiming _timing = default!;

    private HttpClient _http = default!;
    private ISawmill _sawmill = default!;

    private bool _isActive;
    private string _apiToken = string.Empty;
    private string _apiUrl = string.Empty;
    private TimeSpan _nextHealthCheck;

    void IPostInjectInit.PostInject()
    {
        _sawmill = Logger.GetSawmill("napi");
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public void Initialize()
    {
        _config.OnValueChanged(NCCVars.ApiToken, token =>
        {
            _apiToken = token;
            _nextHealthCheck = TimeSpan.Zero;
            _ = RefreshState();
        });

        _config.OnValueChanged(NCCVars.ApiUrl, url =>
        {
            _apiUrl = NormalizeUrl(url);
            _nextHealthCheck = TimeSpan.Zero;
            _ = RefreshState();
        });

        _apiToken = _config.GetCVar(NCCVars.ApiToken);
        _apiUrl = NormalizeUrl(_config.GetCVar(NCCVars.ApiUrl));

        _ = RefreshState();
    }

    private static string NormalizeUrl(string url) => url.Trim().TrimEnd('/');

    private async Task RefreshState()
    {
        if (string.IsNullOrWhiteSpace(_apiToken) || string.IsNullOrWhiteSpace(_apiUrl))
        {
            SetActive(false);
            return;
        }

        SetActive(await IsConnectToApiAlive());
    }

    private void SetActive(bool active)
    {
        if (_isActive == active)
            return;

        _isActive = active;

        if (active)
            _sawmill.Info("Successfully connected");
        else
            _sawmill.Warning("Api disabled");
    }

    /// <summary>
    /// Checks the connection again if the API is inactive, so it recovers on its own after the bot was down.
    /// The checks are spaced out by <see cref="NCCVars.ApiHealthCheckInterval"/>, callers in between get false.
    /// </summary>
    private async Task<bool> EnsureActive()
    {
        if (_isActive || _timing.RealTime < _nextHealthCheck)
            return _isActive;

        _nextHealthCheck = _timing.RealTime + TimeSpan.FromSeconds(_config.GetCVar(NCCVars.ApiHealthCheckInterval));
        await RefreshState();

        return _isActive;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var req = new HttpRequestMessage(method, $"{_apiUrl}{path}");
        req.Headers.Add("X-Internal-Api-Key", _apiToken);
        return req;
    }

    private async Task<bool> IsConnectToApiAlive()
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Get, $"/api/link/{Guid.Empty}");
            using var resp = await _http.SendAsync(req);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                _sawmill.Error("Api rejected the token (401)");

            return resp.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Api health check failed: {e.Message}");
            return false;
        }
    }
}
