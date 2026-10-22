# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

reagent-name-eldritch = жуткая эссенция
reagent-desc-eldritch = Странная жидкость, нарушающая законы физики. Заряжает энергией и исцеляет тех, кто способен заглянуть за пределы хрупкой реальности, но невероятно опасна для неверных.
reagent-physical-desc-eldritch = жуткая
flavor-complex-eldritch = Вл'к'с'ла

crucible-soul-effect-examine-message = { "[" }color=#fb793a]{ CAPITALIZE(SUBJECT($ent)) }, похоже, { GENDER($ent) ->
        [male] не в себе
        [female] не в себе
        [epicene] не в себе
       *[other] не в себе
    }.[/color]
entity-condition-guidebook-environment-temperature = температура окружения
    { $invert ->
        [true] не ниже
        *[false] не выше
    } { $threshold } градусов
entity-condition-guidebook-has-body-part = у цели
    { $invert ->
        [true] нет
        *[false] есть
    } { $part }
entity-condition-guidebook-heretic-or-ghoul = цель — еретик или гуль
entity-condition-guidebook-not-heretic-or-ghoul = цель — не еретик и не гуль
entity-condition-guidebook-nullrod-not-protected = цель не защищена нуль-жезлом
entity-condition-guidebook-nullrod-protected = цель защищена нуль-жезлом
entity-condition-guidebook-on-fire = цель
    { $invert ->
        [true] не горит
        *[false] горит
    }
flavor-complex-clarity = как глаза
flavor-complex-crucible-soul = как что-то меж равнин
flavor-complex-ether = освежающе
flavor-complex-marshal = болезненно
flavor-complex-rust = как гнилая медь
reagent-desc-clarity = Тускло-жёлтая жидкость. Кажется, она регулярно то появляется, то исчезает. Позволяет видеть сквозь стены и предметы.
reagent-desc-crucible-soul = Ярко-оранжевая полупрозрачная жидкость. Позволяет проходить сквозь стены. По окончании действия вы телепортируетесь в исходное место.
reagent-desc-eldritch-rust = Вязкая комковатая бурая жижа.
reagent-desc-ether = Вызывающая тошноту густая зелёная жидкость. Полностью восстанавливает тело, затем погружает в усиленный сон на целую минуту.
reagent-desc-marshal = Бесцветная тёмная жидкость. Увеличивает физическую силу: чем сильнее вы ранены, тем яростнее ваши атаки и тем меньше урона вы получаете. Атаки в ближнем бою восстанавливают здоровье и выносливость, но со временем вы получаете урон — и чем вы здоровее, тем его больше.
reagent-effect-guidebook-deconvert-ghoul = возвращает гуля в нормальное состояние
reagent-effect-guidebook-has-status-effect =
    { $invert ->
        [true] нет
        *[false] есть
    } статус-эффект { $effect }
reagent-name-clarity = закат и рассвет
reagent-name-crucible-soul = горнило души
reagent-name-eldritch-rust = потусторонняя ржавчина
reagent-name-ether = эфир новорождённого
reagent-name-marshal = раненый солдат
reagent-physical-desc-clarity = прозрачное
reagent-physical-desc-crucible-soul = потустороннее
reagent-physical-desc-ether = онемляющее
reagent-physical-desc-marshal = мучительное
reagent-physical-desc-rusty = ржавое
wounded-solider-effect-examine-message = [color=#5e718e]{ CAPITALIZE(SUBJECT($ent)) } в состоянии неумирающего неистовства.[/color]
