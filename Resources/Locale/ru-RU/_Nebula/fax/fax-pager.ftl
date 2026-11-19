ent-FaxPager = пейджер факса
    .desc = Пищит, когда на привязанный факс приходит сообщение. Нажми им по факсу, чтобы привязать или отвязать.

fax-pager-linked = Привязан к факсу "{ $fax }".
fax-pager-unlinked = Отвязан от факса "{ $fax }".
fax-pager-full = Все слоты заняты. Сначала отвяжи какой-нибудь факс.
fax-pager-examine-none = Не привязан ни к одному факсу.
fax-pager-examine = Привязанные факсы ({ $count }/{ $max }): { $faxes }
fax-pager-message = Новый факс на "{ $fax }" от "{ $from }"
fax-pager-message-wrap = [font size=12][bold]Пейджер[/bold]: [/font]{ $message }
fax-pager-verb-unlink = Отвязать
fax-pager-verb-link-station = Привязать все факсы вокруг
fax-pager-linked-station = Привязано новых факсов: { $count }
fax-pager-no-grid = Пейджер не на станции и не на шаттле.

ent-FaxPagerAdmin = { ent-FaxPager }
    .desc = Пищит, когда на привязанный факс приходит сообщение. Можно привязать сколько угодно факсов.
    .suffix = Админ
ent-FaxPagerAdminAuto = { ent-FaxPager }
    .desc = Сам привязывается ко всем факсам станции. Если станции нет, то ко всем факсам шаттла или карты, где лежит.
    .suffix = Админ, Авто
