observer-statistic-popularity = { $name ->
    [noone] В этом раунде ни за кем не наблюдали.
    *[other] За [color=#B0B0B0]{ $name }[/color]([color=gray]{ $username }[/color]) в какой-то момент { $count ->
        [1] наблюдал один призрак
        *[other] наблюдали призраки: { $count }
        }. Интересно, что там произошло?
}
