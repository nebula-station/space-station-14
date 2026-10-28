using System.Threading;
using System.Threading.Tasks;
using Content.Shared._Nebula.CCVar;
using Content.Shared.CCVar;

namespace Content.Server.Database;

public sealed partial class ServerDbManager
{
    private readonly SemaphoreSlim _scopeLock = new(1, 1);
    private volatile bool _scopeCached;
    private int _cachedScopeId;

    private void HookRecordScope()
    {
        _cfg.OnValueChanged(NCCVars.IsolateRecords, _ => _scopeCached = false);
        _cfg.OnValueChanged(CCVars.AdminLogsServerName, _ => _scopeCached = false);
    }

    // null when the cvar is off
    public async Task<int?> GetRecordScopeServerId()
    {
        if (!_cfg.GetCVar(NCCVars.IsolateRecords))
            return null;

        if (_scopeCached)
            return _cachedScopeId;

        await _scopeLock.WaitAsync();
        try
        {
            if (!_cfg.GetCVar(NCCVars.IsolateRecords))
                return null;

            if (_scopeCached)
                return _cachedScopeId;

            var server = await AddOrGetServer(_cfg.GetCVar(CCVars.AdminLogsServerName));
            _cachedScopeId = server.Id;
            _scopeCached = true;
            return _cachedScopeId;
        }
        finally
        {
            _scopeLock.Release();
        }
    }

    // off stays on the sync path, tests need that
    private Task<T> RunScoped<T>(Func<int?, Task<T>> command)
    {
        if (!_cfg.GetCVar(NCCVars.IsolateRecords))
            return RunDbCommand(() => command(null));

        return RunScopedCore(command);
    }

    private async Task<T> RunScopedCore<T>(Func<int?, Task<T>> command)
    {
        var scope = await GetRecordScopeServerId();
        return await RunDbCommand(() => command(scope));
    }
}
