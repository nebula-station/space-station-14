cargo-console-paper-delivery-destination-default = To the requester's department
cargo-console-menu-order-row-product-delivery-destination = Deliver to: {$destination}
cargo-console-order-menu-delivery-destination = Deliver to:
cargo-console-menu-order-requester-format = {$name}, {$job}
cargo-console-secure-order-checkbox = Deliver in the department's secure crate (+${$cost})
cargo-console-bounty-manifest-name = bounty manifest ({$entries})
cargo-console-bounty-manifest-name-entry = {$name} x{$amount}
cargo-account-cargo-destination = Cargo bay
cargo-account-engineering-destination = Engineering
cargo-account-medical-destination = Medbay
cargo-account-science-destination = Science
cargo-account-security-destination = Security
cargo-account-service-destination = Service
cargo-console-arcane-paper-print-name = Order #{$orderNumber} ({$itemName} x{$orderQuantity})
cargo-console-arcane-paper-print-text = [head=2]Order #{$orderNumber}[/head]
    {"[bold]Item:[/bold]"} {$itemName} (x{$orderQuantity})
    {"[bold]Requested by:[/bold]"} {$requester}
    {"[bold]Deliver to:[/bold]"} {$destination}
    {"[head=3]Order Information[/head]"}
    {"[bold]Payer[/bold]:"} {$account} [font="Monospace"]\[{$accountcode}\][/font]
    {"[bold]Approved by:[/bold]"} {$approver}
    {"[bold]Reason:[/bold]"} {$reason}
cargo-arcane-acquisition-slip-body = [head=3]Asset Detail[/head]
    {"[bold]Product:[/bold]"} {$product}
    {"[bold]Description:[/bold]"} {$description}
    {"[bold]Unit cost:[/bold"}] ${$unit}
    {"[bold]Amount:[/bold]"} {$amount}
    {"[bold]Cost:[/bold]"} ${$cost}
    {"[head=3]Purchase Detail[/head]"}
    {"[bold]Orderer:[/bold]"} {$orderer}
    {"[bold]Deliver to:[/bold]"} {$destination}
    {"[bold]Reason:[/bold]"} {$reason}
