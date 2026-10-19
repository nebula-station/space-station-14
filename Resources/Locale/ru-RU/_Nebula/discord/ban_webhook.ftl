# System localization

nebula-webhook-server-name = Nebula [18+]
nebula-webhook-unknown = Неизвестно

ban-webhook-unknown-error = NotFound

# Titles

ban-webhook-role-ban-role = 🚨 Бан ролей
ban-webhook-role-ban-server = 🚨 Серверный бан
ban-webhook-role-unban-server = 💚 Разбан

# Timing

ban-webhook-expaire-at = ⏱️ { $time }

ban-webhook-unbanned-at-title = ⏱️ Время разбана
ban-webhook-unbanned-at = { $time }

ban-webhook-never = Навсегда

ban-webhook-days = { $days ->
        [one] { $days } день
        [few] { $days } дня
       *[other] { $days } дней
    }

ban-webhook-hours = { $hours ->
        [one] { $hours } час
        [few] { $hours } часа
       *[other] { $hours } часов
    }

ban-webhook-minutes = { $minutes ->
        [one] { $minutes } минута
        [few] { $minutes } минуты
       *[other] { $minutes } минут
    }

# Users

ban-webhook-target = 👤 Нарушитель
ban-webhook-admin = 🛡️ Администратор

# Other

ban-webhook-footer = Раунд: { $round }

ban-webhook-reason = 📌 Причина
ban-webhook-roles = { $count ->
        [one] 🎭 Роль
       *[other] 🎭 Роли
    }
