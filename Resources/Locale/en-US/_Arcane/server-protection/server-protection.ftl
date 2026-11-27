server-protection-server-actor = the server (console or config)
server-protection-toggled-on = [ServerProtection] { $system } was enabled by { $actor }.
server-protection-toggled-off = [ServerProtection] { $system } was DISABLED by { $actor }.

emote-protection-ban-reason = Automatic ban for emote spam ({ $count } emotes). Appeal if you believe this was a mistake.
emote-protection-kick-reason = Automatic kick for emote spam ({ $count } emotes).
emote-protection-admin-announcement-ban = { $player } was banned for emote spam ({ $count } emotes).
emote-protection-admin-announcement-kick = { $player } was kicked for emote spam ({ $count } emotes).

admin-action-protection-mass-ban = [ServerProtection] Possible sabotage: admin { $admin } ({ $userId }) placed too many bans in a short time. Last target: { $target }.
admin-action-protection-mass-permission-removal = [ServerProtection] Possible sabotage: admin { $admin } ({ $userId }) is stripping admins of their permissions. Last action: { $action } on { $target }.
admin-action-protection-deadmined = [ServerProtection] { $admin } ({ $userId }) was suspended as admin for suspected sabotage ({ $reason }). Lift the suspension in the permissions panel.
admin-action-protection-banned = [ServerProtection] { $admin } ({ $userId }) was banned for { $minutes } minutes (0 is permanent) for suspected sabotage ({ $reason }).
admin-action-protection-ban-reason = ServerProtection auto-ban: suspicious admin actions ({ $reason }).
admin-action-protection-rank-escalation = [ServerProtection] { $admin } tried to assign an admin rank above their own permissions!
