ent-InfiniteDormExitDoor = дверь выхода
    .desc = Деревянная дверь. Её ручка источает странную ауру.
    .suffix = Беск. Дормы

ent-InfinityDormTeleporter = бесконечный шар
    .desc = Шар, на плоскости которого выверены цифры...
    .suffix = Беск. Дормы

ent-InfiniteDormSpawnMarker = маркер спавна беск. дорм
    .desc = Маркер для определения, где в бесконечном дорматории игрок должен будет появиться.
    .suffix = Беск. Дормы

infinity-dorm-teleporter-ui-title = Меню бесконечных дорматориев

infinity-dorm-teleporter-ui-button-confirm = Переместиться

infinity-dorm-teleporter-ui-current-room-label = Текущая комната:

infinity-dorm-teleporter-ui-dont-selected = Не выбрана!

infinity-dorm-teleporter-ui-input-number = Номер дорматория

infinity-dorm-exit-ui-title = Выход из дормы
infinity-dorm-exit-ui-players-label = Игроков внутри
infinity-dorm-exit-ui-closed-toggle = Закрыть дормы для посторонних
infinity-dorm-exit-ui-owner-only = Менять доступ может только владелец
infinity-dorm-exit-ui-button-exit = Выйти

infinity-dorm-exit-ui-is-closed = Дормы:
infinity-dorm-exit-ui-is-closed-answer =
    { $bool ->
        [true] Закрыты
       *[false] Открыты
    }

infinity-dorm-players-ui-title = Игроки в дормах
infinity-dorm-players-ui-empty = Сейчас никого нет
infinity-dorm-players-ui-group = Дорматории №{$index} ({$count})

infinity-dorm-teleporter-ui-players-list-button = Список посетителей

infinity-dorm-teleporter-popup-bad-alert-level = Вы не можете посетить дормы во время кода "{ $code }"
infinity-dorm-teleporter-popup-distance = Вы не дотягиваетесь до шара

infinity-dorm-teleporter-popup-client-pidor = Ну ты тоже не ахуевай с изменением клиента

infinity-dorm-teleporter-popup-rate-limit = Слишком часто. Попробуйте немножко позже

infinity-dorm-teleporter-popup-rooms-limit = Вы достигли лимита одновременных комнат. Пожалуйста дождитесь их очистки ({ $count })

infinity-dorm-teleporter-popup-closed = Данные дормы закрыты

infinity-dorm-rooms-ui-title = Ваши комнаты

infinity-dorm-rooms-ui-empty = У вас нет личных комнат

infinity-dorm-rooms-ui-unnamed = Без названия

infinity-dorm-rooms-ui-number = № { $number }

infinity-dorm-rooms-ui-visitors = Посетителей: { $count }

infinity-dorm-rooms-ui-deletes = Удаление через: { $time }

infinity-dorm-teleporter-ui-rooms-list-button = Мои комнаты


infinity-dorm-system-hand-teleport-attempt = Вы не можете использовать телепортер в данной зоне!
