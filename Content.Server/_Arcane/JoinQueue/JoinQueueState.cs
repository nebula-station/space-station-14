// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dfbda555db9acabf3827cf3b0785c6deaa5c1a8b/Content.Arcane.Server/JoinQueue/JoinQueueState.cs

using Robust.Shared.Network;

namespace Content.Server._Arcane.JoinQueue;

/// <summary>
/// Keeps a reconnecting player in their original FIFO position without reserving a game slot offline.
/// Entries are created before asynchronous profile/permission loading so completion order cannot reorder them.
/// </summary>
internal sealed class JoinQueueState<TSession> where TSession : class
{
    private readonly List<Entry> _entries = [];
    private readonly Dictionary<NetUserId, Entry> _byUser = [];

    public IReadOnlyList<Entry> Entries => _entries;

    public bool TryGet(NetUserId user, out Entry entry) => _byUser.TryGetValue(user, out entry!);

    public Entry Connect(NetUserId user, TSession session, TimeSpan now)
    {
        Prune(now);
        if (_byUser.TryGetValue(user, out var entry))
        {
            entry.Session = session;
            entry.Ready = false;
            entry.Reserved = false;
            entry.ExpiresAt = null;
            return entry;
        }

        entry = new Entry(user, session, now);
        _entries.Add(entry);
        _byUser.Add(user, entry);
        return entry;
    }

    public bool IsCurrent(NetUserId user, TSession session) =>
        _byUser.TryGetValue(user, out var entry) && ReferenceEquals(entry.Session, session);

    public bool MarkReady(NetUserId user, TSession session)
    {
        if (!_byUser.TryGetValue(user, out var entry) || !ReferenceEquals(entry.Session, session))
            return false;
        entry.Ready = true;
        return true;
    }

    public bool Disconnect(NetUserId user, TSession session, TimeSpan expiresAt)
    {
        if (!_byUser.TryGetValue(user, out var entry) || !ReferenceEquals(entry.Session, session))
            return false;
        entry.Session = null;
        entry.Ready = false;
        entry.Reserved = false;
        entry.ExpiresAt = expiresAt;
        return true;
    }

    public bool Remove(NetUserId user, TSession session, out Entry entry)
    {
        if (!_byUser.TryGetValue(user, out entry!) || !ReferenceEquals(entry.Session, session))
            return false;
        _byUser.Remove(user);
        _entries.Remove(entry);
        return true;
    }

    public bool Reserve(NetUserId user, TSession session, out Entry entry)
    {
        if (!_byUser.TryGetValue(user, out entry!) || !ReferenceEquals(entry.Session, session)
            || !entry.Ready || entry.Reserved)
            return false;
        entry.Reserved = true;
        return true;
    }

    public bool TryReserve(out Entry entry)
    {
        foreach (var candidate in _entries)
        {
            if (candidate.Session == null || candidate.Reserved)
                continue;
            if (!candidate.Ready)
                break;

            return Reserve(candidate.User, candidate.Session, out entry);
        }
        entry = null!;
        return false;
    }

    public bool Prune(TimeSpan now)
    {
        var removed = false;
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.Session != null || entry.ExpiresAt is not { } expires || now < expires)
                continue;
            _entries.RemoveAt(i);
            _byUser.Remove(entry.User);
            removed = true;
        }
        return removed;
    }

    public void Clear()
    {
        _entries.Clear();
        _byUser.Clear();
    }

    internal sealed class Entry(NetUserId user, TSession session, TimeSpan waitedSince)
    {
        public readonly NetUserId User = user;
        public readonly TimeSpan WaitedSince = waitedSince;
        public TSession? Session = session;
        public TimeSpan? ExpiresAt;
        public bool Ready;
        public bool Reserved;
    }
}
