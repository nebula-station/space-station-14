// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/dead-space-server/EridaGoob/blob/312c4de3ed33142c2a488771f4d1f58bbc499974/Content.Server/_Orion/ServerProtection/Chat/ChatProtectionSystem.cs

using Content.Shared._Orion.ServerProtection.Chat;
using Content.Shared.Administration.Managers;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.ServerProtection.Chat;

/// <summary>
/// Blocks IC and OOC messages containing banned words and punishes their authors.
/// </summary>
public sealed partial class ChatProtectionSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private ISharedAdminManager _admin = default!;
    [Dependency] private ServerProtectionPunishmentSystem _punishment = default!;

    private static readonly ProtoId<ChatProtectionListPrototype> ICList = "IC_BannedWords";
    private static readonly ProtoId<ChatProtectionListPrototype> OOCList = "OOC_BannedWords";

    private ISawmill _log = default!;
    private string[] _icWords = [];
    private string[] _oocWords = [];
    private readonly HashSet<NetUserId> _punished = [];

    private bool _protectionEnabled;
    private bool _eraseEnabled;
    private bool _banEnabled;
    private bool _kickEnabled;
    private bool _deleteMessagesEnabled;
    private int _banDuration;

    public override void Initialize()
    {
        base.Initialize();

        _log = Logger.GetSawmill("serverprotection.chat_protection");

        Subs.CVar(_cfg, CCVars.ChatProtectionEnabled, v => _protectionEnabled = v, true);
        Subs.CVar(_cfg, CCVars.ChatProtectionEraseEnabled, v => _eraseEnabled = v, true);
        Subs.CVar(_cfg, CCVars.ChatProtectionBanEnabled, v => _banEnabled = v, true);
        Subs.CVar(_cfg, CCVars.ChatProtectionKickEnabled, v => _kickEnabled = v, true);
        Subs.CVar(_cfg, CCVars.ChatProtectionDeleteMessages, v => _deleteMessagesEnabled = v, true);
        Subs.CVar(_cfg, CCVars.ChatProtectionBanDuration, v => _banDuration = v, true);

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        CacheWords();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ChatProtectionListPrototype>())
            CacheWords();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
            _punished.Remove(args.Session.UserId);
    }

    private void CacheWords()
    {
        _icWords = _proto.TryIndex(ICList, out var ic) ? [.. ic.Words] : [];
        _oocWords = _proto.TryIndex(OOCList, out var ooc) ? [.. ooc.Words] : [];

        _log.Info($"Кэшировано {_icWords.Length} IC и {_oocWords.Length} OOC запрещённых слов.");
    }

    /// <summary>
    /// Checks an IC message typed by the player.
    /// </summary>
    /// <returns>True if the message must be blocked.</returns>
    public bool CheckIC(ICommonSession player, string message)
    {
        return Check(player, message, _icWords, "IC");
    }

    /// <summary>
    /// Checks an OOC, LOOC or dead chat message typed by the player.
    /// </summary>
    /// <returns>True if the message must be blocked.</returns>
    public bool CheckOOC(ICommonSession player, string message)
    {
        return Check(player, message, _oocWords, "OOC");
    }

    private bool Check(ICommonSession player, string message, string[] words, string channel)
    {
        if (!_protectionEnabled || string.IsNullOrEmpty(message) || _admin.IsAdmin(player, true))
            return false;

        if (_punished.Contains(player.UserId))
            return true;

        foreach (var word in words)
        {
            if (!message.Contains(word, StringComparison.OrdinalIgnoreCase))
                continue;

            HandleViolation(player, word, channel);
            return true;
        }

        return false;
    }

    private void HandleViolation(ICommonSession player, string word, string channel)
    {
        _log.Info($"{player.Name} ({player.UserId}) использовал запрещённое слово: '{word}' в {channel}");

        if (_deleteMessagesEnabled)
            _punishment.DeleteMessages(player);

        if (channel == "IC" && _eraseEnabled)
            _punishment.EraseCharacter(player);

        if (_banEnabled)
        {
            _punished.Add(player.UserId);
            _punishment.SendAdminAlert(Loc.GetString("chat-protection-admin-announcement-ban-reason",
                ("player", player.Name),
                ("word", word),
                ("channel", channel)));
            _punishment.ApplyBan(player,
                Loc.GetString("chat-protection-ban-reason", ("word", word), ("channel", channel)),
                _banDuration);
        }
        else if (_kickEnabled)
        {
            _punished.Add(player.UserId);
            _punishment.SendAdminAlert(Loc.GetString("chat-protection-admin-announcement-kick-reason",
                ("player", player.Name),
                ("word", word),
                ("channel", channel)));
            _punishment.KickPlayer(player,
                Loc.GetString("chat-protection-kick-reason", ("word", word), ("channel", channel)));
        }
    }
}
