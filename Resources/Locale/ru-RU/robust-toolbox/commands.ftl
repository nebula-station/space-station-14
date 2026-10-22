cmd-addcomp-desc = Добавляет компонент сущности.
cmd-addcomp-help = Использование: { $command } <uid> <имя компонента>
cmd-addcompc-desc = Добавляет компонент сущности на клиенте.
cmd-addcompc-help = Использование: { $command } <uid> <имя компонента>
cmd-addmap-desc = Добавляет в раунд новую пустую карту. Если mapID уже существует, эта команда ничего не сделает.
cmd-addmap-help = Использование: { $command } <mapID> [pre-init]
cmd-addview-desc = Позволяет подписаться на просмотр сущности в целях отладки.
cmd-addview-help = Использование: { $command } <entityUid>
cmd-addviewc-desc = Позволяет подписаться на просмотр сущности в целях отладки.
cmd-addviewc-help = Использование: { $command } <entityUid>
cmd-bind-arg-command = <InputCommand>
cmd-bind-arg-key = <KeyName>
cmd-bind-arg-mode = <BindMode>
cmd-bind-desc = Привязывает комбинацию клавиш ввода с командой ввода.
cmd-bind-help =
    Использование: bind { cmd-bind-arg-key } { cmd-bind-arg-mode } { cmd-bind-arg-command }
    Обратите внимание, что это НЕ сохраняет привязки автоматически.
    Используйте команду 'svbind', чтобы сохранить конфигурацию привязки.
cmd-chunkinfo-desc = Получает информацию о чанке под курсором мыши.
cmd-chunkinfo-help = Использование: chunkinfo
cmd-cldbglyr-desc = Переключение слоёв отладки поле зрения и освещения.
cmd-cldbglyr-help =
    Использование: cldbglyr <layer>: Toggle <layer>
    cldbglyr: Отключить все слои
cmd-cls-desc = Очищает консоль.
cmd-cls-help = Очищает консоль отладки от всех сообщений.
cmd-csi-desc = Открывает интерактивную консоль C#.
cmd-csi-help = Использование: csi
cmd-cspawn-desc = Спавнит на стороне клиента сущность определённого типа у ваших ног.
cmd-cspawn-help = Использование: { $command } <тип сущности>
cmd-cvar-arg-name = <имя | ?>
cmd-cvar-compl-list = Список доступных CVar-ов
cmd-cvar-desc = Получает или устанавливает CVar.
cmd-cvar-help =
    Использование: cvar <name | ?> [значение]
    Если значение предоставлено, оно спарсится и сохранится как новое значение CVar.
    Если нет, отобразится текущее значение CVar.
    Используйте 'cvar ?' для получения списка всех зарегистрированных CVar-ов.
cmd-cvar-invalid-args = Должно быть представлено ровно один или два аргумента.
cmd-cvar-not-registered = CVar '{ $cvar }' не зарегистрирован. Используйте 'cvar ?' для получения списка всех зарегистрированных CVar-ов.
cmd-cvar-parse-error = Входное значение имеет неправильный формат для типа { $type }
cmd-cvar-value-hidden = <value hidden>
cmd-delete-desc = Удаляет сущность с указанным ID.
cmd-delete-help = Использование: { $command } <entity UID>
cmd-devwindow-desc = Окно разработки
cmd-devwindow-help = Использование: devwindow
cmd-disconnect-desc = Немедленно отключиться от сервера и вернуться в главное меню.
cmd-disconnect-help = Использование: disconnect
cmd-dmetamem-desc = Выводит члены типа в формате, подходящем для файла конфигурации песочницы.
cmd-dmetamem-help = Использование: dmetamem <type>
cmd-dump_dependency_injectors-desc = Дамп кэша инжектора зависимостей IoCManager.
cmd-dump_dependency_injectors-help = Использование: dump_dependency_injectors
cmd-dump_dependency_injectors-total-count = Общее количество: { $total }
cmd-dump_event_tables-arg-entity = <entityUid>
cmd-dump_event_tables-desc = Выводит таблицы направленных событий для сущности.
cmd-dump_event_tables-error-entity = Недопустимая сущность
cmd-dump_event_tables-help = Использование: dump_event_tables <entityUid>
cmd-dump_event_tables-missing-arg-entity = Отсутствует аргумент сущности
cmd-dump_net_comps-desc = Выводит таблицу сетевых компонентов.
cmd-dump_net_comps-error-writeable = Регистрация всё ещё доступна для записи, сетевые идентификаторы не были сгенерированы.
cmd-dump_net_comps-header = Регистрации сетевых компонентов:
cmd-dump_net_comps-help = Использование: dump_net-comps
cmd-dump_netserializer_type_map-desc = Дамп карты типов NetSerializer и хеша сериализатора.
cmd-dump_netserializer_type_map-help = Использование: dump_netserializer_type_map
cmd-dumpentities-desc = Дамп списка объектов.
cmd-dumpentities-help = Выводит список объектов с UID и прототипом.
cmd-echo-desc = Вывести аргументы в консоль
cmd-echo-help = Использование: echo "<сообщение>"
cmd-entfo-desc = Отображает подробную диагностику сущности.
cmd-entfo-help =
    Использование: entfo <entityuid>
    UID сущности может иметь префикс 'c', чтобы быть преобразованной в UID клиентской сущности.
cmd-error-dir-not-found = Не удалось найти директорию: { $dir }.
cmd-error-file-not-found = Не удалось найти файл: { $file }.
cmd-exec-arg-filename = <имя файла>
cmd-exec-desc = Исполняет скриптовый файл из записываемых пользовательских данных игры
cmd-exec-help =
    Использование: exec <fileName>
    Каждая строка в файле выполняется как одна команда, если только она не начинается со знака #
cmd-failure-no-attached-entity = К этой оболочке не привязана никакая сущность.
cmd-fill-desc = Заполнить консоль для отладки.
cmd-fill-help = Заполняет консоль всякой чепухой для отладки.
cmd-flushcookies-desc = Сброс хранилища CEF-cookie на диск
cmd-flushcookies-help =
    Это гарантирует правильное сохранение файлов cookie на диске в случае неаккуратного выключения.
    Имейте в виду, что фактическая операция является асинхронной.
cmd-forall-desc = Запускает команду для всех сущностей с данным компонентом.
cmd-forall-help = Использование: forall <bql query> do <command...>
cmd-fuck-desc = Вызывает исключение
cmd-fuck-help = Вызывает исключение
cmd-gc-arg-generation = [поколение]
cmd-gc-desc = Запускает GC (Garbage Collector, Сборка мусора)
cmd-gc-failed-parse = Не удалось спарсить аргумент.
cmd-gc-help =
    Использование: gc [поколение]
    Использует GC.Collect() для запуска Сборки мусора.
    Если был предоставлен аргумент, то он спарсится как номер поколения GC и используется GC.Collect(int).
    Используйте команду 'gfc' для проведения сборки мусора, со сжатием 'кучи больших объектов' (LOH-compacting).
cmd-gc_mode-arg-type = [тип]
cmd-gc_mode-attempt = попытка изменения режима задержки gc: { $prevMode } -> { $mode }
cmd-gc_mode-current = текущий режим задержки gc: { $prevMode }
cmd-gc_mode-desc = Изменяет/отображает режим задержки GC
cmd-gc_mode-help =
    Использование: gc_mode [тип]
    Если аргумент не был предоставлен, вернётся текущий режим задержки GC.
    Если аргумент был пропущен, он спарсится как GCLatencyMode и будет установлен как режим задержки GC.
cmd-gc_mode-option = - { $mode }
cmd-gc_mode-possible = возможные режимы:
cmd-gc_mode-result = полученный режим задержки gc: { $mode }
cmd-gc_mode-unknown = неизвестный режим задержки gc: { $arg }
cmd-gcf-desc = Запускает GC, полную, со сжатием 'кучи больших объектов' (LOH-compacting) и всего.
cmd-gcf-help =
    Использование: gcf
    Выполняет полный GC.Collect(2, GCCollectionMode.Forced, true, true) одновременно сжимая 'кучу больших объектов' LOH.
    Скорее всего, это приведёт к зависанию на сотни миллисекунд, имейте в виду.
cmd-getclipboard-desc = Получает системный буфер обмена
cmd-getclipboard-help = Использование: Getclipboard
cmd-getcomponentregistration-desc = Получает информацию о регистрации компонента.
cmd-getcomponentregistration-help = Использование: getcomponentregistration <имя компонента>
cmd-gridtc-desc = Получить количество плиток в гриде.
cmd-gridtc-help = Использование: gridtc <gridId>
cmd-guidump-desc = Дамп дерева интерфейса в /guidump.txt в данные пользователя.
cmd-guidump-help = Использование: guidump
cmd-hardquit-desc = Мгновенно убивает игровой клиент.
cmd-hardquit-help = Убивает игровой клиент мгновенно, не оставляя следов. Не говорит серверу пока.
cmd-help-arg-cmdname = [имя команды]
cmd-help-desc = Выводит общую справку или справку по определённой команде
cmd-help-help =
    Использование: help [имя команды]
    Если имя команды не будет указано, будет выведена общая справка. Если имя команды будет указано, будет выведена справка по этой команде.
cmd-help-invalid-args = Недопустимое количество аргументов.
cmd-help-no-args = Чтобы получить справку по определённой команде, используйте 'help <command>'. Для получения списка всех доступных команд используйте 'list'. Для поиска по командам используйте 'list <filter>'.
cmd-help-top = { $command } - { $description }
cmd-help-unknown = Неизвестная команда: { $command }
cmd-hint-loadmap-rotation = [вращение]
cmd-hint-loadmap-uids = [float]
cmd-hint-loadmap-x-position = [позиция x]
cmd-hint-loadmap-y-position = [позиция y]
cmd-hint-savebp-id = <Grid EntityID>
cmd-hint-savemap-force = [bool]
cmd-hint-savemap-id = <MapID>
cmd-hint-savemap-path = <Путь>
cmd-hub_advertise_now-desc = Немедленно разместить сервер в хабе
cmd-hub_advertise_now-help = Использование: hub_advertise_now
cmd-hwid-desc = Возвращает текущий HWID (HardWare ID).
cmd-hwid-help = Использование: hwid
cmd-invalid-arg-number-error = Недопустимое число аргументов.
cmd-key-info-desc = Информация о ключе для клавиши.
cmd-key-info-help = Использование: keyinfo <Кнопка>
cmd-kick-desc = Кикает подключённого игрока с сервера, отключая его от сети.
cmd-kick-help = Использование: { $command } <PlayerIndex> [<Причина>]
cmd-launchauth-desc = Загрузить токены аутентификации из данных лаунчера, чтобы облегчить тестирование работающих серверов.
cmd-launchauth-help = Использование: launchauth <account name>
cmd-ldrsc-desc = Предварительно кэширует ресурс.
cmd-ldrsc-help = Использование: ldrsc <path> <type>
cmd-lightbb-desc = Переключить отображение световой ограничительной рамки.
cmd-lightbb-help = Использование: lightbb
cmd-list-arg-filter = [фильтр]
cmd-list-desc = Выводит список доступных команд с опциональным поисковым фильтром
cmd-list-heading = SIDE NAME            DESC{ "\u000A" }-------------------------{ "\u000A" }
cmd-list-help =
    Использование: list [фильтр]
    Выводит список всех доступных команд. Если был предоставлен аргумент, он будет использоваться для фильтрации команд по имени.
cmd-listplayers-desc = Перечисляет всех игроков, подключённых в данный момент.
cmd-listplayers-help = Использование: { $command }
cmd-loadgrid-desc = Загружает грид из файла на существующую карту.
cmd-loadgrid-help = Использование: { $command } <MapID> <Путь> [x y] [вращение] [storeUids]
cmd-loadmap-desc = Загружает карту с диска в игру.
cmd-loadmap-error = При загрузке карты из { $path } произошла ошибка.
cmd-loadmap-exists = Карта { $mapId } уже существует.
cmd-loadmap-help = Использование: { $command } <MapID> <Путь> [x] [y] [вращение] [consistentUids]
cmd-loadmap-nullspace = Невозможно загрузить в карту 0.
cmd-loadmap-success = Карта { $mapId } была загружена из { $path }.
cmd-loc-desc = Выводит абсолютное местоположение сущности игрока в консоль.
cmd-loc-help = Использование: { $command }
cmd-loglevel-desc = Изменяет уровень логирования для предоставленного sawmill.
cmd-loglevel-help =
    Использование: loglevel <sawmill> <level>
    sawmill: Метка, которая префиксирует сообщения логов. Именно для него вы устанавливаете уровень.
    level: Уровень логирования. Должно соответствовать одному из значений перечисления LogLevel.
cmd-lsasm-desc = Выводит список загруженных сборок по контексту загрузки
cmd-lsasm-help = Использование: lsasm
cmd-lsgrid-desc = Перечисляет гриды.
cmd-lsgrid-help = Использование: { $command }
cmd-lsmap-desc = Перечисляет карты.
cmd-lsmap-help = Использование: { $command }
cmd-mapinit-desc = Запускает инициализацию карты на карте.
cmd-mapinit-help = Использование: { $command } <mapID>
cmd-mem-desc = Выводит информацию об управляемой памяти
cmd-mem-help = Использование: mem
cmd-mem-report =
    Размер кучи: { TOSTRING($heapSize, "N0") }
    Всего распределено: { TOSTRING($totalAllocated, "N0") }
cmd-monitor-arg-count = Отсутствует аргумент монитора
cmd-monitor-arg-monitor = <monitor>
cmd-monitor-desc = Переключение отладочного монитора в меню F3.
cmd-monitor-help =
    Использование: monitor <name>
    Возможные мониторы: { $monitors }
    Вы также можете использовать специальные значения "-all" и "+all", чтобы соответственно скрыть или показать все мониторы.
cmd-monitor-invalid-name = Недопустимое имя монитора
cmd-monitor-minus-all-hint = Скрывает все мониторы
cmd-monitor-plus-all-hint = Показывает все мониторы
cmd-monitorinfo-desc = Информация о мониторах
cmd-monitorinfo-help = Использование: monitorinfo <id>
cmd-net-draw-interp-desc = Переключает отладочный рисунок сетевой интерполяции.
cmd-net-draw-interp-help = Использование: net_draw_interp
cmd-net-entity-report-desc = Переключает панель отчёта о сетевых сущностях.
cmd-net-entity-report-help = Использование: net_entityreport
cmd-net-refresh-desc = Запрашивает полное состояние сервера.
cmd-net-refresh-help = Использование: net_refresh
cmd-net-watch-ent-desc = Выводит на консоль все сетевые обновления для EntityId.
cmd-net-watch-ent-help = Использование: net_watchent <0|EntityUid>
cmd-net_draw_interp-desc = Включает отладочную отрисовку сетевой интерполяции.
cmd-net_draw_interp-help = Использование: net_draw_interp <0|EntityUid>
cmd-net_entityreport-desc = Переключает панель отчёта о сетевых сущностях.
cmd-net_entityreport-help = Использование: net_entityreport
cmd-net_graph-desc = Переключает панель статистики сети.
cmd-net_graph-help = Использование: net_graph
cmd-net_refresh-desc = Запрашивает полное состояние сервера.
cmd-net_refresh-help = Использование: net_refresh
cmd-net_watchent-desc = Выводит в консоль все сетевые обновления для EntityId.
cmd-net_watchent-help = Использование: net_watchent <0|EntityUid>
cmd-netaudit-desc = Выводит информацию о безопасности NetMsg.
cmd-netaudit-help = Использование: { $command }
cmd-overrideplayername-desc = Изменяет имя, используемое при попытке подключения к серверу.
cmd-overrideplayername-help = Использование: overrideplayername <name>
cmd-parse-failure-bool = { $arg } не является допустимым bool.
cmd-parse-failure-entity-exist = UID { $arg } не соответствует существующей сущности.
cmd-parse-failure-float = { $arg } не является допустимым float.
cmd-parse-failure-integer = { $arg } не является допустимым integer.
cmd-parse-failure-mapid = { $arg } не является допустимым MapId.
cmd-parse-failure-uid = { $arg } не является допустимым UID сущности.
cmd-physics-desc = Показывает наложение отладочной физики. Аргумент определяет наложение.
cmd-physics-help = Использование: physics <aabbs / com / contactnormals / contactpoints / distance / joints / shapeinfo / shapes>
cmd-physics-overlay = { $overlay } не является распознанным оверлеем
cmd-profsnap-desc = Сделать снимок профилирования.
cmd-profsnap-help = Использование: profsnap
cmd-quit-desc = Корректное завершение работы клиента игры.
cmd-quit-help = Правильно завершает работу игрового клиента, уведомляя об этом подключённый сервер и т.д.
cmd-remoteexec-desc = Выполняет команду на стороне сервера
cmd-remoteexec-help =
    Использование: > <command> [arg] [arg] [arg...]
    Выполняет команду на стороне сервера. Это необходимо, если на клиенте имеется команда с таким же именем, так как при простом выполнении команды сначала будет запущена команда на клиенте.
cmd-removeview-desc = Позволяет отписаться от просмотра сущности в целях отладки.
cmd-removeview-help = Использование: { $command } <entityUid>
cmd-restart-desc = Корректно перезапускает сервер (не только раунд).
cmd-restart-help = Использование: { $command }
cmd-rldloc-desc = Перезагружает локализацию (клиент и сервер).
cmd-rldloc-help = Использование: rldloc
cmd-rldrsc-desc = Перезагружает ресурсы.
cmd-rldrsc-help = Использование: rldrsc <path> <type>
cmd-rldshader-desc = Перезагружает все шейдеры.
cmd-rldshader-help = Использование: rldshader
cmd-rmcomp-desc = Удаляет компонент у сущности.
cmd-rmcomp-help = Использование: { $command } <uid> <имя компонента>
cmd-rmcompc-desc = Удаляет компонент у сущности на клиенте.
cmd-rmcompc-help = Использование: { $command } <uid> <имя компонента>
cmd-rmgrid-desc = Удаляет грид с карты. Вы не можете удалить стандартный грид.
cmd-rmgrid-help = Использование: { $command } <gridId>
cmd-rmmap-desc = Удаляет карту из мира. Вы не можете удалить nullspace.
cmd-rmmap-help = Использование: { $command } <mapId>
cmd-saveconfig-desc = Сохраняет конфигурацию клиента в файл конфигурации.
cmd-saveconfig-help = Использование: { $command }
cmd-savegrid-desc = Сериализует грид на диск.
cmd-savegrid-help = Использование: { $command } <gridID> <Path>
cmd-savemap-attempt = Попытка сохранить карту { $mapId } в { $path }.
cmd-savemap-desc = Сериализует карту на диск. Не будет сохранять карту после инициализации, если это не будет сделано принудительно.
cmd-savemap-help = Использование: { $command } <MapID> <Путь> [принудительно]
cmd-savemap-init-warning = Попытка сохранить карту после инициализации без принудительного сохранения.
cmd-savemap-not-exist = Целевая карта не существует.
cmd-savemap-success = Карта успешно сохранена.
cmd-scene-desc = Немедленно сменяет UI сцены/состояния.
cmd-scene-help = Использование: scene <className>
cmd-scsi-desc = Открывает интерактивную консоль C# на сервере.
cmd-scsi-help = Использование: scsi
cmd-sendgarbage-desc = Отправляет мусор на сервер.
cmd-sendgarbage-help = Сервер ответит 'нет ты'.
cmd-set-ambient-light-desc = Позволяет установить эмбиентое освещение для указанной карты, в формате SRGB.
cmd-set-ambient-light-help = Использование: { $command } [mapid] [r g b a]
cmd-set-ambient-light-parse = Не удалось спарсить аргументы как байтовые значения цветов.
cmd-setclipboard-desc = Устанавливает системный буфер обмена
cmd-setclipboard-help = Использование: setclipboard <text>
cmd-setinputcontext-desc = Устанавливает активный контекст ввода.
cmd-setinputcontext-help = Использование: setinputcontext <context>
cmd-setmonitor-desc = Установить монитор
cmd-setmonitor-help = Использование: setmonitor <id>
cmd-sggcell-desc = Перечисляет сущности в ячейке сетки привязки.
cmd-sggcell-help = Использование: sggcell <gridID> <vector2i>\nЭтот vector2i параметр в форме x<int>,y<int>.
cmd-showanchored-desc = Показывает закреплённые объекты на определённой плитке.
cmd-showanchored-help = Использование: showanchored
cmd-showgridnodes-desc = Показывает узлы для разделения сетки.
cmd-showgridnodes-help = Использование: showgridnodes
cmd-showislands-desc = Показывает текущие физические тела, задействованные в каждом physics island.
cmd-showislands-help = Использование: showislands
cmd-showpos-desc = Включает отрисовку для всех позиций сущностей в игре.
cmd-showpos-help = Использование: showpos
cmd-showrays-desc = Переключает отладку отображения физических лучей. Необходимо указать целое число для <raylifetime>.
cmd-showrays-help = Использование: showrays <raylifetime>
cmd-showspritebb-desc = Переключить отображение границ спрайта
cmd-showspritebb-help = Использование: showspritebb
cmd-showtime-desc = Показывает время сервера.
cmd-showtime-help = Использование: { $command }
cmd-showvelocities-desc = Отображает угловую и линейную скорости.
cmd-showvelocities-help = Использование: showvelocities
cmd-shutdown-desc = Корректно выключает сервер.
cmd-shutdown-help = Использование: { $command }
cmd-spawn-desc = Создаёт сущность определённого типа.
cmd-spawn-help = spawn <прототип> ИЛИ spawn <прототип> <относительная сущность ID> ИЛИ spawn <прототип> <x> <y>
cmd-spin-desc = Заставляет сущность вращаться. Сущность по умолчанию является надклассом прикреплённого игрока.
cmd-spin-help = Использование: { $command } velocity [drag] [entityUid]
cmd-szr_stats-desc = Сообщить статистику сериализатора.
cmd-szr_stats-help = Использование: szr_stats
cmd-testbed-desc = Загружает физический испытательный стенд на указаной карте.
cmd-testbed-help = Использование: { $command } <mapid> <test>
cmd-testlog-desc = Записывает протокол тестов в sawmill.
cmd-testlog-help =
    Использование: testlog <sawmill> <level> <message>
    sawmill: Метка, префиксируемая логированному сообщению.
    level: Уровень логирования. Должно соответствовать одному из значений перечисления LogLevel.
    message: Логируемое сообщение. Заключите в двойные кавычки, если хотите использовать пробелы.
cmd-togglefov-desc = Переключает поле зрения клиента.
cmd-togglefov-help = Использование: togglefov
cmd-togglehardfov-desc = Включает жёсткое поле зрения клиента. (для отладки space-station-14#2353)
cmd-togglehardfov-help = Использование: togglehardfov
cmd-togglelight-desc = Переключает рендеринг света.
cmd-togglelight-help = Использование: togglelight
cmd-togglelightbuf-desc = Переключение рендеринга освещения. Сюда входят тени, но не поле зрения.
cmd-togglelightbuf-help = Использование: togglelightbuf
cmd-togglelookup-desc = Показывает/скрывает границы списка сущностей с помощью наложения.
cmd-togglelookup-help = Использование: togglelookup
cmd-toggleshadows-desc = Переключение рендеринга теней.
cmd-toggleshadows-help = Использование: toggleshadows
cmd-tp-desc = Телепортирует игрока в любую точку в раунде.
cmd-tp-help = Использование: { $command } <x> <y> [<mapID>]
cmd-tpgrid-desc = Телепортирует грид в новое место.
cmd-tpgrid-help = Использование: { $command } <gridId> <X> <Y> [<MapId>]
cmd-tpto-desc = Телепортирует текущего игрока или указанных игроков/сущностей к местоположению первого игрока/сущности.
cmd-tpto-destination-hint = место назначения (uid или имя пользователя)
cmd-tpto-help = Использование: { $command } <имя пользователя|uid> [имя пользователя|NetEntity]...
cmd-tpto-parse-error = Не удаётся распознать сущность или игрока: { $str }
cmd-tpto-victim-hint = сущность для телепортации (uid или имя пользователя)
cmd-uitest-desc = Открыть UI окно для тестирования
cmd-uitest-help = Использование: uitest
cmd-uitest2-arg-tab = <tab>
cmd-uitest2-desc = Открывает UI контрольного тестирования ОС
cmd-uitest2-error-args = Ожидается не более одного аргумента
cmd-uitest2-error-tab = Недопустимая вкладка: '{ $value }'
cmd-uitest2-help = Использование: uitest2 <tab>
cmd-uitest2-title = UITest2
cmd-vfs_ls-desc = Перечислить содержимое каталогов в VFS.
cmd-vfs_ls-err-args = Нужен ровно 1 аргумент.
cmd-vfs_ls-help =
    Использование: vfs_list <path>
    Пример:
    vfs_list /Assemblies
cmd-vfs_ls-hint-path = <путь>
cmd-vram-desc = Отображает статистику использования видеопамяти игрой.
cmd-vram-help = Использование: vram
cmd-vv-desc = Открывает просмотр переменных.
cmd-vv-help = Использование: vv <сущность ID|IoC имя интерфейса|SIoC имя интерфейса>
cmd-vvinvoke-desc = Вызов/запуск пути с аргументами с помощью VV.
cmd-vvinvoke-help = Использование: vvinvoke <path> [arguments...]
cmd-vvread-desc = Получить значение пути с помощью VV (View Variables).
cmd-vvwrite-desc = Изменить значение пути с помощью VV (View Variables).
cmd-vvwrite-help = Использование: vvwrite <path>
cmd-watch-desc = Открывает окно просмотра переменных.
cmd-watch-help = Использование: watch

cmd-chunkentities-arg-range = <range>
cmd-chunkentities-arg-root = <root entity>
cmd-chunkentities-arg-x = <x>
cmd-chunkentities-arg-y = <y>
cmd-chunkentities-desc = Список chunk-сущностей во вьюпорте клиента или в указанном радиусе.
cmd-chunkentities-entry = { $netEntity } uid={ $uid } root={ $root } chunk={ $chunk } comps={ $componentCount } { $name }
cmd-chunkentities-error-invalid-root = Недопустимая корневая сущность: { $root }
cmd-chunkentities-error-no-map = Нет сущности карты для текущего глаза { $map }.
cmd-chunkentities-error-nullspace = Текущий глаз находится в nullspace.
cmd-chunkentities-error-parse = x, y и range должны быть числами.
cmd-chunkentities-help = Использование: { $command } [<root entity> <x> <y> <range>]
cmd-chunkentities-range-header = Chunk-сущности для { $root } около ({ $x }, { $y }) радиус { $range }:
cmd-chunkentities-root-count = Корень { $root }: { $count }
cmd-chunkentities-total = Всего: { $count }
cmd-chunkentities-viewport-header = Chunk-сущности во вьюпорте клиента на карте { $map } ({ $viewport }):
cmd-cvar_subs-arg-name = <name>
cmd-cvar_subs-desc = Список подписок OnValueChanged для CVar.
cmd-cvar_subs-help = Использование: { $command } <name>
cmd-cvar_subs-invalid-args = Нужен ровно один аргумент.
cmd-hint-float = [float]
cmd-parse-failure-cultureinfo = "{ $arg }" не является допустимым CultureInfo.
cmd-parse-failure-enum = { $arg } не является значением Enum { $enum }.
cmd-parse-failure-grid = { $arg } не является допустимым гридом.
cmd-parse-failure-session = Нет сессии с именем пользователя: { $username }
cmd-parse-failure-session-guid = Нет сессии с GUID: { $guid }
cmd-pausemap-desc = Ставит карту на паузу, останавливая симуляцию на ней.
cmd-pausemap-help = Использование: pausemap <map ID>
cmd-querymappaused-desc = Проверяет, на паузе ли карта.
cmd-querymappaused-help = Использование: querymappaused <map ID>
cmd-savemap-error = Не удалось сохранить карту! См. лог сервера.
cmd-showangvel-desc = Показать угловую скорость всех сущностей на экране.
cmd-showangvel-help = Использование: { $command }
cmd-showrot-desc = Показать поворот всех сущностей на экране.
cmd-showrot-help = Использование: { $command }
cmd-showvel-desc = Показать локальную скорость всех сущностей на экране.
cmd-showvel-help = Использование: { $command }
cmd-shutdown-hint-1 = Причина
cmd-unpausemap-desc = Снимает карту с паузы, возобновляя симуляцию.
cmd-unpausemap-help = Использование: unpausemap <map ID>
cmd-vvread-help = Использование: { $command } <path>
