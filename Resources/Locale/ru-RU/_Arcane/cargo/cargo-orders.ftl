cargo-console-paper-delivery-destination-default = В отдел заказчика
cargo-console-menu-order-row-product-delivery-destination = Место доставки: { $destination }
cargo-console-order-menu-delivery-destination = Место доставки:
cargo-console-menu-order-requester-format = { $name }, { $job }
cargo-console-secure-order-checkbox = Поместить заказ в защищённый ящик отдела? (+{ $cost }кр)
cargo-console-bounty-manifest-name = этикетка запроса ({ $entries })
cargo-console-bounty-manifest-name-entry = { $detailName } x { $detailQuantity }
cargo-account-cargo-destination = Склад снабжения
cargo-account-engineering-destination = Инженерный отдел
cargo-account-medical-destination = Медицинский отдел
cargo-account-science-destination = Научный отдел
cargo-account-security-destination = Отдел службы безопасности
cargo-account-service-destination = Сервисный отдел
cargo-console-arcane-paper-print-name = Заказ №{$orderNumber} ({$itemName} x{$orderQuantity})
cargo-console-arcane-paper-print-text = [head=2]Заказ №{$orderNumber}[/head]
    {"[bold]Товар:[/bold]"} {$itemName} (x{$orderQuantity})
    {"[bold]Заказчик:[/bold]"} {$requester}
    {"[bold]Доставить:[/bold]"} {$destination}
    {"[head=3]Информация о заказе[/head]"}
    {"[bold]Плательщик[/bold]:"} {$account} [font="Monospace"]\[{$accountcode}\][/font]
    {"[bold]Одобрил:[/bold]"} {$approver}
    {"[bold]Причина:[/bold]"} {$reason}
cargo-arcane-acquisition-slip-body = [head=3]Сведения о товаре[/head]
    {"[bold]Товар:[/bold]"} {$product}
    {"[bold]Описание:[/bold]"} {$description}
    {"[bold]Цена за единицу:[/bold]"} ${$unit}
    {"[bold]Количество:[/bold]"} {$amount}
    {"[bold]Стоимость:[/bold]"} ${$cost}
    {"[head=3]Сведения о покупке[/head]"}
    {"[bold]Заказчик:[/bold]"} {$orderer}
    {"[bold]Доставить:[/bold]"} {$destination}
    {"[bold]Причина:[/bold]"} {$reason}
