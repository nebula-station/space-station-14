server-protection-server-actor = сервер (консоль или конфиг)
server-protection-toggled-on = [ServerProtection] { $system } включена: { $actor }.
server-protection-toggled-off = [ServerProtection] { $system } ВЫКЛЮЧЕНА: { $actor }.

emote-protection-ban-reason = Автоматический бан за спам эмоциями ({ $count } эмоций). Подайте апелляцию, если считаете это ошибкой.
emote-protection-kick-reason = Автоматический кик за спам эмоциями ({ $count } эмоций).
emote-protection-admin-announcement-ban = { $player } забанен за спам эмоциями ({ $count } эмоций).
emote-protection-admin-announcement-kick = { $player } кикнут за спам эмоциями ({ $count } эмоций).

admin-action-protection-mass-ban = [ServerProtection] Возможный саботаж: админ { $admin } ({ $userId }) выдал слишком много банов за короткое время. Последняя цель: { $target }.
admin-action-protection-mass-permission-removal = [ServerProtection] Возможный саботаж: админ { $admin } ({ $userId }) снимает права с других админов. Последнее действие: { $action }, цель: { $target }.
admin-action-protection-deadmined = [ServerProtection] Права { $admin } ({ $userId }) приостановлены из-за подозрения в саботаже ({ $reason }). Снять приостановку можно в панели прав.
admin-action-protection-banned = [ServerProtection] { $admin } ({ $userId }) забанен на { $minutes } мин. (0 — навсегда) из-за подозрения в саботаже ({ $reason }).
admin-action-protection-ban-reason = Автобан ServerProtection: подозрительные действия админа ({ $reason }).
admin-action-protection-rank-escalation = [ServerProtection] { $admin } пытался выдать ранг с правами выше своих!
