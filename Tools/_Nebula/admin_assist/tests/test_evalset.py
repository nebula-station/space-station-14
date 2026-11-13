from __future__ import annotations

from admin_assist.briefing import cited_ids, malformed_citations, parse_briefing, unknown_citations
from admin_assist.evalset import EVAL_DIR, check, load_case, load_cases
from admin_assist.render import render_user_message

GOOD_4_1 = """Приоритет: ОБЫЧНЫЙ
Тип: жалоба
Суть: pelmen_enjoyer жалуется, что бармен Morozko убила его в баре без причины, и просит разобраться.
Участники: pelmen_enjoyer (Гриша Ковалёв, ботаник, антаг: нет, мёртв), Morozko (Алина Штерн, бармен, антаг: нет, жива)
Логи: автор первым дважды ударил Morozko [L3][L4], после предупреждения [L5] бросил в неё бутылку [L6]; затем Morozko выстрелила [L7] и добила его в крите [L9].
Расхождения: «ничего не делал» противоречит логам [L3][L4][L6].
Правила: п. 1.1: "Необоснованные убийства, саботаж отдела и другие деструктивные действия без логического и достойного основания."
Оценка: неясно — ответ на нападение соразмерен, но второй выстрел по игроку в крите [L9] под вопросом; уверенность: средняя
Проверить: представлял ли автор угрозу между [L8] и [L9]; чат Morozko за 01:09–01:10
Флаги: у автора тот же HWID, что у kvas_lover (бан за RDM); у Morozko предупреждение за добивание в крите."""


def test_all_cases_load_and_render():
    cases = load_cases()
    assert len(cases) >= 20
    assert len({c.id for c in cases}) == len(cases)
    for case in cases:
        text = render_user_message(case.context, case.nonce)
        assert text.count("<ticket_") == 1, case.id
        for n in range(1, len(case.context.logs) + 1):
            assert f"\n[L{n}] " in text, case.id


def test_the_set_covers_what_the_brief_asks_for():
    cases = load_cases()
    types = {t for c in cases for t in c.expect["type"]}
    assert {"жалоба", "вопрос", "баг"} <= types
    assert sum("манипуляц" in str(c.expect.get("contains", {})) for c in cases) >= 3
    assert any(len(c.context.messages) == 1 and c.context.messages[0].text.lower().strip("! ") == "админ" for c in cases)
    assert any("6" in str(c.expect.get("contains", {}).get("Правила", "")) for c in cases)


def test_parse_briefing():
    fields = parse_briefing(GOOD_4_1)
    assert list(fields)[:3] == ["Приоритет", "Тип", "Суть"]
    assert fields["Приоритет"] == "ОБЫЧНЫЙ"
    assert fields["Оценка"].startswith("неясно — ")
    assert parse_briefing("Вот брифинг:\n\nПриоритет: НИЗКИЙ\nТип: вопрос\nСуть: спрашивает,\nкак открыть шлюз") == {
        "Приоритет": "НИЗКИЙ",
        "Тип": "вопрос",
        "Суть": "спрашивает,\nкак открыть шлюз",
    }


def test_citations():
    assert cited_ids("было [L3][L4], потом [L3] и [L12]; XL7 и [см. п. 3] не считаются") == ["L3", "L4", "L12"]
    assert cited_ids("[L3, L4] затем [L5–L7] и просто L9") == ["L3", "L4", "L5", "L6", "L7", "L9"]
    assert cited_ids("[L3-5] [Л9] [l10] [Ｌ11] [L012] [L1 и L2; Л7] Л8") == ["L3", "L4", "L5", "L9", "L10", "L11", "L12", "L1", "L2", "L7", "L8"]
    assert malformed_citations("[L1] [L-9] [L3,9] [L5-L2] [L1-L900] [L 5] [не ссылка]") == ["[L-9]", "[L3,9]", "[L5-L2]", "[L1-L900]", "[L 5]"]
    assert unknown_citations("[L1] [L11] [L0]", {"L1", "L2"}) == ["L11", "L0"]


def test_check_passes_a_good_briefing():
    case = load_case(EVAL_DIR / "spec_4_1_rdm_contradicted.json")
    assert check(case, GOOD_4_1) == []


def test_check_catches_failures():
    case = load_case(EVAL_DIR / "spec_4_1_rdm_contradicted.json")

    def failures(old: str, new: str) -> str:
        assert old in GOOD_4_1
        return " | ".join(check(case, GOOD_4_1.replace(old, new)))

    assert "Приоритет ВЫСОКИЙ" in failures("Приоритет: ОБЫЧНЫЙ", "Приоритет: ВЫСОКИЙ")
    assert "несуществующие логи: L77" in failures("[L7]", "[L77]")
    assert "не процитированы: L9" in failures("[L9]", "")
    assert "Оценка" in failures("Оценка: неясно", "Оценка: скорее нарушение")
    assert "в «Расхождения» нет «ничего не делал»" in failures("«ничего не делал» противоречит", "слова автора противоречат")
    assert "в «Флаги» нет «kvas_lover»" in failures("что у kvas_lover", "что у другого аккаунта")
    assert "в «Оценка» есть «бан»" in failures("уверенность: средняя", "уверенность: средняя, нужен бан")
    assert "нет уверенности" in failures("; уверенность: средняя", "")
    assert "начинается не с" in failures("Приоритет: ОБЫЧНЫЙ\n", "Вот брифинг.\nПриоритет: ОБЫЧНЫЙ\n")
    assert "слишком длинно" in " | ".join(check(case, GOOD_4_1 + "\nФлаги: " + "а" * 1000))


def test_check_short_layout_for_questions():
    case = load_case(EVAL_DIR / "spec_4_1_rdm_contradicted.json")
    answer = "Приоритет: НИЗКИЙ\nТип: вопрос\nСуть: спрашивает про бар\nОценка: неясно — ?; уверенность: низкая"
    assert "для типа «вопрос» лишние строки: Оценка" in " | ".join(check(case, answer))
