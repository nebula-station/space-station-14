set-game-preset-command-description = Установить игровой пресет для текущего раунда.
set-game-preset-command-help-text = setgamepreset <id>
set-game-preset-preset-error = Не удаётся найти игровой пресет "{ $preset }"
set-game-preset-optional-argument-not-integer = If argument 2 is provided it must be a number.
set-game-preset-preset-set-finite = Set game preset to "{ $preset }" for the next { $rounds } rounds.

set-game-preset-command-hint-1 = <id>
set-game-preset-command-hint-2 = [количество раундов]
set-game-preset-command-hint-3 = [пресет для обмана]
set-game-preset-decoy-error = Если третий аргумент предоставлен, он должен быть валидным пресетом. Не удаётся найти игровой пресет "{ $preset }"
set-game-preset-preset-set-finite-with-decoy = Установлен пресет "{ $preset }" на { $rounds ->
    [one] следующий раунд
    [few] следующие { $rounds } раунда
    *[other] следующие { $rounds } раундов
}, отображая в лобби { $decoy }.
