ghost-respawn-max-players = Cannot respawn right now. There should be fewer than {$players} players.
ghost-respawn-window-rules-footer = By respawning, you [color=#ff7700]agree[/color] [color=#ff0000]not to use any knowledge gained as your previous character[/color]. Violation of this rule may constitute a server ban. Please read the server rules for more details.
ghost-respawn-same-character-slightly-changed-name = You just changed some symbols in the name. Are you serious?
ghost-respawn-same-character = You cannot respawn as the same character. Please select a different one in character preferences.
ghost-respawn-log-character-almost-same = Player {$player} { $try ->
    [true] joined
    *[false] tried to join
} the round after respawning with a similar name. Previous name: { $oldName }, current: { $newName }.
ghost-respawn-log-return-to-lobby = { $userName } returned to the lobby.
ghost-respawn-command-no-entity = This command can only be used by a player with an attached entity.
ghost-respawn-time-left = Time left until you can return to the round: { $time }

ghost-gui-return-to-round-ready-button = Return to round
ghost-gui-return-to-round-button = Until respawn: { $time }
ghost-gui-return-to-round-button-tooltip = Allows return to the round by going to the lobby.

cmd-returntoround-desc = Returns a ghost to the lobby once the respawn timer has passed.
cmd-returntoround-help = Usage: returntoround
