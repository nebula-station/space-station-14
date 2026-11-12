roles-antag-erp-changeling-name = ЕРП-генокрад
roles-antag-erp-changeling-objective = Мирный генокрад, питающийся возбуждением своих жертв.
role-subtype-erp-changeling = ЕРП-генокрад
erp-changeling-round-end-agent-name = ЕРП-генокрад

erp-changeling-role-greeting = Вы - новый подвид генокрадов, отрёкшийся от опасных и агрессивных форм путей развития, питающийся феромонами своих жертв с последующим извлечением ДНК через оргазм цели. НаноТрейзен все ещё считает ваш вид неизученным, от чего у вас имеется риск быть пойманным и отправленным на ЦК под заключением. Будьте аккуратны и действуйте скрытно, вы свободны в своих целях, но помните - вы НЕ антагонист, на вас действуют правила обычного члена экипажа, но с полным разрешением на изнасилования.

ghost-role-information-erp-changeling-name = ЕРП-генокрад
ghost-role-information-erp-changeling-description = Новый подвид генокрадов, отрёкшийся от агрессивных путей развития и питающийся феромонами своих жертв.
ghost-role-information-erp-changeling-rules =
    Вы [color=#FF69B4][bold]НЕ антагонист[/bold][/color]: на вас действуют правила обычного члена экипажа, но с полным разрешением на изнасилования.
    НаноТрейзен считает ваш вид неизученным и может отправить вас на ЦК под заключением, поэтому действуйте скрытно.
    Уважайте теги согласия: при "Спросить" сначала спросите в LOOC, при "Нет" - не трогайте.

erp-changeling-fail-consent = { CAPITALIZE($target) } не даёт согласия на это.
erp-changeling-fed = Мы питаемся возбуждением { $target } и получаем { $points } очк. эволюции.

store-ling-category-erp = ЕРП

evolutionmenu-erp-sting-crocin-name = Кроциновое жало
evolutionmenu-erp-sting-crocin-desc = Незаметно вводит жертве кроцин, возбуждая её. Стоимость: 15 химикатов.
evolutionmenu-erp-sting-hexacrocin-name = Гексакроциновое жало
evolutionmenu-erp-sting-hexacrocin-desc = Незаметно вводит жертве гексакроцин, разжигая в ней сильную похоть. Стоимость: 15 химикатов.
evolutionmenu-erp-sting-panty-dropper-name = Раздевающее жало
evolutionmenu-erp-sting-panty-dropper-desc = Незаметно вводит жертве "Жидкий трусосброс": она возбуждается и сбрасывает одежду. Стоимость: 15 химикатов.
evolutionmenu-erp-arm-tentacle-name = Рука-щупальце
evolutionmenu-erp-arm-tentacle-desc = Превращает одну из рук в приятное щупальце для ЕРП-взаимодействий. Бесплатно.

objective-condition-erp-orgasm-title = Довести { $targetName }, { CAPITALIZE($job) }, до оргазма.
objective-condition-erp-take-form-title = Принять облик { $targetName }, { CAPITALIZE($job) }.
objective-condition-erp-arouse-crew-title = Возбудить жалами { $count } членов экипажа.
objective-condition-erp-arouse-crew-description = Нам нужно ужалить ЕРП-жалами { $count } разных членов экипажа.

ent-ActionStingCrocin = Кроциновое жало
    .desc = Незаметно вводит жертве кроцин, возбуждая её. Стоимость: 15 химикатов.
ent-ActionStingHexacrocin = Гексакроциновое жало
    .desc = Незаметно вводит жертве гексакроцин, разжигая в ней сильную похоть. Стоимость: 15 химикатов.
ent-ActionStingPantyDropper = Раздевающее жало
    .desc = Незаметно вводит жертве "Жидкий трусосброс": она возбуждается и сбрасывает одежду. Стоимость: 15 химикатов.
ent-ActionToggleArmTentacle = Переключить руку-щупальце
    .desc = Превращает одну из рук в приятное щупальце. Повторное использование втягивает его обратно. Бесплатно.
ent-ArmTentacleChangeling = рука-щупальце
    .desc = Оно извивается и пульсирует!
    .suffix = Неснимаемое
ent-MindRoleErpChangeling = Роль ЕРП-генокрада
    .desc = { ent-BaseMindRoleAntag.desc }
ent-SpawnPointGhostErpChangeling = { ent-BaseAntagSpawner }
    .desc = { ent-BaseAntagSpawner.desc }
ent-ErpChangelingOrgasmObjective = { ent-BaseChangelingObjective }
    .desc = Наша жертва должна почувствовать нас. Её тело должно полностью сдаться.
ent-ErpChangelingTakeFormObjective = { ent-BaseChangelingObjective }
    .desc = Мы хотим носить их тело как своё.
ent-ErpChangelingArouseCrewObjective = { ent-BaseChangelingObjective }
    .desc = { ent-BaseChangelingObjective.desc }
ent-ErpChangelingEscapeObjective = Сбежать на ЦентКом живым и свободным на эвакуационном шаттле.
    .desc = Наше выступление окончено, пора покинуть сцену.
