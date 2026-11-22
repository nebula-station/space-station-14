bodycam-verb-enable = Включить бодикамеру
bodycam-verb-disable = Выключить бодикамеру
bodycam-examine-enabled = Индикатор записи [color=red]горит[/color].
bodycam-examine-disabled = Индикатор записи не горит.
bodycam-popup-enabled = Запись бодикамеры включена
bodycam-popup-disabled = Запись бодикамеры выключена
bodycam-pocket-denied = Из кармана бодикамера ничего не запишет.

bodycam-record-name = №{ $id } { $name }
bodycam-wearer-none = никто
bodycam-wearer-job = { $name } ({ $job })

bodycam-footage-window-title = Записи бодикамер
bodycam-footage-round-time = Время смены: { $time }
bodycam-footage-available = Архив доступен
bodycam-footage-unavailable = Архив недоступен
bodycam-footage-cameras = Бодикамеры
bodycam-footage-camera-on = { $name } [запись]
bodycam-footage-camera-off = { $name }
bodycam-footage-segments = Записанные отрезки
bodycam-footage-no-segments = Эта бодикамера за смену ничего не записала.
bodycam-footage-segment = { $from } - { $to } · { $wearer }
bodycam-footage-segment-open = { $from } - сейчас · { $wearer }
bodycam-footage-from = С
bodycam-footage-to = По
bodycam-footage-last = Последние
bodycam-footage-minutes = минут
bodycam-footage-length = Длина { $length } (макс. { $max } мин)
bodycam-footage-length-too-long = [color=red]Слишком долго[/color]: { $length } (макс. { $max } мин)
bodycam-footage-unfiltered = Без фильтра (админское)
bodycam-footage-request = Запросить запись
bodycam-footage-watch = Смотреть
bodycam-footage-watch-note = На время просмотра вы отключитесь от сервера, но потом переподключитесь.

bodycam-footage-status-idle = Выберите бодикамеру и отрезок времени.
bodycam-footage-status-queued = Ожидание в очереди архива...
bodycam-footage-status-processing = Извлечение записи...
bodycam-footage-status-sending = Загрузка записи...
bodycam-footage-status-sent = Запись готова. Нажмите "Смотреть".
bodycam-footage-status-sent-detail = Получено { $size } МБ.
bodycam-footage-status-failed = Запрос не выполнен.

bodycam-footage-access-denied = Доступ запрещён.
bodycam-footage-error-camera = Этой бодикамеры нет в архиве.
bodycam-footage-error-unavailable = Эта смена не записывается.
bodycam-footage-error-range = Неверный отрезок времени.
bodycam-footage-error-too-long = Запрошенная запись слишком длинная. Максимум { $max } минут.
bodycam-footage-error-corrupt = Файл архива повреждён.
bodycam-footage-error-version = Архив записан другой версией сервера.
bodycam-footage-error-empty = В этом отрезке времени ещё нет записи.
bodycam-footage-error-internal = Архиву не удалось извлечь запись.
bodycam-footage-error-disconnected = Запросивший отключился до получения записи.
bodycam-footage-disconnect-reason = Просмотр записи бодикамеры

cmd-bodycam_cut-desc = Вырезает отрывок из файла повтора сервера.
cmd-bodycam_cut-help = Использование: bodycam_cut <файл повтора в папке повторов> <с тика> <по тик> [net id камеры]. Без камеры отрывок не фильтруется.
cmd-bodycam_cut-missing = Файл повтора { $path } не найден.
cmd-bodycam_cut-done = Записан { $path } ({ $size } КиБ, тики { $first }-{ $last }) за { $time } с.
