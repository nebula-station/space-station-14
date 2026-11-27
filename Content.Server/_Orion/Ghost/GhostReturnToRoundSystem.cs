// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane/blob/045e1f70c57558b76595316d8f399599f85bc753/Content.Server/_Orion/Ghost/GhostReturnToRoundSystem.cs
// Ported from https://github.com/ArcaneSS14/arcane/blob/045e1f70c57558b76595316d8f399599f85bc753/Content.Server/GameTicking/GameTicker.Spawning.cs

using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared._Orion.Ghost;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost.Components;
using Content.Shared.Ghost.Systems;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._Orion.Ghost;

public sealed partial class GhostReturnToRoundSystem : SharedGhostReturnToRoundSystem
{
    [Dependency] private ServerGameTicker _gameTicker = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private SharedGhostSystem _ghostSystem = default!;

    private int _ghostRespawnMaxPlayers;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(Cfg, CCVars.GhostRespawnMaxPlayers, value => _ghostRespawnMaxPlayers = value, true);
    }

    public void TryGhostReturnToRound(Entity<GhostComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!_playerManager.TryGetSessionByEntity(ent, out var session))
            return;

        if (_playerManager.PlayerCount >= _ghostRespawnMaxPlayers)
        {
            SendChatMsg(session, Loc.GetString("ghost-respawn-max-players", ("players", _ghostRespawnMaxPlayers)));
            return;
        }

        var now = GameTiming.CurTime;
        var timeOffset = now - ent.Comp.TimeOfDeath;

        if (timeOffset < TimeSpan.Zero)
        {
            _ghostSystem.SetTimeOfDeath((ent.Owner, ent.Comp), now);
            timeOffset = TimeSpan.Zero;
        }

        if (timeOffset < GhostRespawnTime)
        {
            SendChatMsg(session, Loc.GetString("ghost-respawn-time-left", ("time", (GhostRespawnTime - timeOffset).ToString(@"mm\:ss"))));
            return;
        }

        _gameTicker.Respawn(session);
        _adminLogger.Add(LogType.Mind, LogImpact.Medium, $"{Loc.GetString("ghost-respawn-log-return-to-lobby", ("userName", session.Name))}");

        SendChatMsg(session, Loc.GetString("ghost-respawn-window-rules-footer"));
    }

    /// <summary>
    /// Checks that a late-joining player doesn't come back as a character that already was in the round.
    /// </summary>
    public bool CanLateJoinAs(ICommonSession player, HumanoidCharacterProfile character)
    {
        if (_adminManager.IsAdmin(player))
            return true;

        if (IsSameCharacter(player, character, out var checkAvoid))
            return true;

        var message = checkAvoid
            ? Loc.GetString("ghost-respawn-same-character-slightly-changed-name")
            : Loc.GetString("ghost-respawn-same-character");
        SendChatMsg(player, message);
        return false;
    }

    private bool IsSameCharacter(ICommonSession player, HumanoidCharacterProfile character, out bool checkAvoid)
    {
        checkAvoid = false;

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mind))
        {
            if (mind.OriginalOwnerUserId != player.UserId
                || mind.CharacterName is not { } oldName
                || mind.OwnedEntity == null)
                continue;

            if (oldName == character.Name)
                return false;

            var similarity = CalculateStringSimilarity(oldName, character.Name);
            switch (similarity)
            {
                case >= 85f:
                    _chatManager.SendAdminAlert(Loc.GetString("ghost-respawn-log-character-almost-same",
                        ("player", player.Name),
                        ("try", false),
                        ("oldName", oldName),
                        ("newName", character.Name)));
                    checkAvoid = true;
                    return false;
                case >= 50f:
                    _chatManager.SendAdminAlert(Loc.GetString("ghost-respawn-log-character-almost-same",
                        ("player", player.Name),
                        ("try", true),
                        ("oldName", oldName),
                        ("newName", character.Name)));
                    break;
            }
        }

        return true;
    }

    private static float CalculateStringSimilarity(string str1, string str2)
    {
        var (n, m) = (str1.Length, str2.Length);
        if (n == 0 || m == 0)
            return 0;

        var d = new int[n + 1, m + 1];
        for (var i = 0; i <= n; i++)
        {
            d[i, 0] = i;
        }

        for (var j = 0; j <= m; j++)
        {
            d[0, j] = j;
        }

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = str1[i - 1] == str2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        var maxLength = Math.Max(n, m);
        return (maxLength - d[n, m]) / (float) maxLength * 100;
    }

    [SubscribeNetworkEvent]
    private void OnGhostReturnToRoundRequest(GhostReturnToRoundRequest msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ghost || !TryComp<GhostComponent>(ghost, out var ghostComponent))
            return;

        TryGhostReturnToRound((ghost, ghostComponent));
    }

    private void SendChatMsg(ICommonSession session, string message)
    {
        _chatManager.ChatMessageToOne(ChatChannel.Server,
            message,
            Loc.GetString("chat-manager-server-wrap-message", ("message", message)),
            default,
            false,
            session.Channel,
            Color.Red);
    }
}
