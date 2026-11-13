from __future__ import annotations

from datetime import datetime, timedelta, timezone
from uuid import UUID

from admin_assist.tracker import MAX_DETECTOR_CALLS, MAX_FAILURES, Tracker

P = UUID("00000000-0000-0000-0000-0000000000a1")
Q = UUID("00000000-0000-0000-0000-0000000000b2")
ADMIN = UUID("00000000-0000-0000-0000-0000000000e5")
T0 = datetime(2026, 11, 1, 18, 0, tzinfo=timezone.utc)


def sec(n: float) -> datetime:
    return T0 + timedelta(seconds=n)


def msg(msg_id: int, at: float, *, owner: UUID = P, admin: bool = False, round_id: int | None = 7) -> dict:
    return {
        "ahelp_message_id": msg_id,
        "round_id": round_id,
        "server_id": 1,
        "ticket_owner_user_id": owner,
        "sender_user_id": ADMIN if admin else owner,
        "sender_is_admin": admin,
        "sent_at": sec(at),
    }


def kinds(tracker: Tracker, at: float) -> list[tuple[str, str | None]]:
    return [(a.kind, a.trigger) for a in tracker.due(sec(at))]


def test_messages_of_one_thread_make_one_ticket(config):
    tracker = Tracker(config)
    first = tracker.observe(msg(1, 0))
    assert tracker.observe(msg(2, 3)) is first
    assert tracker.observe(msg(3, 5, owner=Q)) is not first
    assert tracker.observe(msg(4, 6, round_id=8)) is not first
    assert (first.first_message_id, first.last_message_id, first.last_player_message_id) == (1, 2, 2)
    assert first.pending and not first.opened_by_staff


def test_silence_longer_than_the_gap_starts_a_new_ticket(config):
    tracker = Tracker(config)
    first = tracker.observe(msg(1, 0))
    assert tracker.observe(msg(2, config.ticket_gap_s - 1)) is first
    second = tracker.observe(msg(3, 2 * config.ticket_gap_s + 5))
    assert second is not first and second.first_message_id == 3 and second.summaries == 0


def test_out_of_order_ids_do_not_move_the_ticket_backwards(config):
    tracker = Tracker(config)
    ticket = tracker.observe(msg(5, 4))
    tracker.observe(msg(4, 3))
    assert (ticket.last_message_id, ticket.last_player_message_id, ticket.last_message_at) == (5, 5, sec(4))


def test_detector_is_asked_after_the_debounce_and_only_once_per_message(config):
    tracker = Tracker(config)
    ticket = tracker.observe(msg(1, 0))
    assert kinds(tracker, config.detector_debounce_s - 1) == []
    assert kinds(tracker, config.detector_debounce_s) == [("detect", None)]

    ticket.detector_verdict = (1, False)
    assert kinds(tracker, 30) == []
    tracker.observe(msg(2, 31))
    assert kinds(tracker, 32) == []
    assert kinds(tracker, 31 + config.detector_debounce_s) == [("detect", None)]


def test_done_summarises_and_wait_is_overruled_by_the_forced_timer(config):
    tracker = Tracker(config)
    ticket = tracker.observe(msg(1, 0))
    ticket.detector_verdict = (1, True)
    assert kinds(tracker, 8) == [("summarise", "detector")]

    ticket.detector_verdict = (1, False)
    assert kinds(tracker, config.detector_force_s - 1) == []
    assert kinds(tracker, config.detector_force_s) == [("summarise", "forced")]


def test_busy_tickets_are_left_alone(config):
    tracker = Tracker(config)
    ticket = tracker.observe(msg(1, 0))
    ticket.busy = True
    assert kinds(tracker, 100) == []


def test_follow_up_only_while_no_staff_member_answered_and_only_so_many(config):
    tracker = Tracker(config)
    ticket = tracker.observe(msg(1, 0))
    ticket.handled_through_id, ticket.summaries = 1, 1
    assert kinds(tracker, 100) == []

    tracker.observe(msg(2, 110))
    assert kinds(tracker, 110 + config.detector_debounce_s) == [("detect", None)]

    ticket.summaries = config.ticket_max_summaries
    assert kinds(tracker, 110 + config.detector_force_s) == []

    ticket.summaries = 1
    tracker.observe(msg(3, 120, admin=True))
    assert ticket.first_staff_reply_at == sec(120)
    tracker.observe(msg(4, 125))
    assert kinds(tracker, 125 + config.detector_force_s) == []


def test_first_summary_still_comes_when_staff_answered_first(config):
    tracker = Tracker(config)
    tracker.observe(msg(1, 0))
    tracker.observe(msg(2, 2, admin=True))
    assert kinds(tracker, config.detector_force_s) == [("summarise", "forced")]


def test_a_thread_opened_by_staff_is_never_summarised(config):
    tracker = Tracker(config)
    ticket = tracker.observe(msg(1, 0, admin=True))
    tracker.observe(msg(2, 5))
    assert ticket.opened_by_staff and ticket.pending
    assert kinds(tracker, 5 + config.detector_force_s) == []


def test_stale_and_failing_tickets_are_given_up_on(config):
    tracker = Tracker(config)
    ticket = tracker.observe(msg(1, 0))
    assert kinds(tracker, config.ticket_stale_s + 1) == [("give_up", "устарел")]

    tracker = Tracker(config)
    ticket = tracker.observe(msg(1, 0))
    for n in range(1, MAX_FAILURES + 1):
        Tracker.failed(ticket, sec(80))
        assert ticket.failures == n
        assert kinds(tracker, 80) == []
        assert kinds(tracker, 80 + 15 * n) == [("summarise", "forced")]
    Tracker.failed(ticket, sec(140))
    assert kinds(tracker, 140 + 15 * (MAX_FAILURES + 1)) == [("give_up", "модель не отвечает")]


def test_closed_tickets_are_dropped_and_the_next_message_opens_a_new_one(config):
    tracker = Tracker(config)
    ticket = tracker.observe(msg(1, 0))
    ticket.handled_through_id = 1
    assert tracker.due(sec(config.ticket_gap_s + 1)) == [] and tracker.tickets == []
    assert tracker.observe(msg(2, config.ticket_gap_s + 2)) is not ticket


def test_restore_keeps_the_newest_ticket_per_thread(config):
    old = Tracker(config)
    a = old.observe(msg(1, 0))
    b = old.observe(msg(2, 2 * config.ticket_gap_s))
    tracker = Tracker(config)
    tracker.restore([a, b])
    assert tracker.tickets == [b]
    assert tracker.observe(msg(3, 2 * config.ticket_gap_s + 5)) is b


def test_a_player_who_never_stops_writing_is_summarised_after_a_few_detector_calls(config):
    tracker = Tracker(config)
    step = config.detector_debounce_s + 1
    assert step < config.detector_force_s
    for n in range(MAX_DETECTOR_CALLS):
        ticket = tracker.observe(msg(n + 1, n * step))
        assert kinds(tracker, n * step + config.detector_debounce_s) == [("detect", None)]
        ticket.detector_calls += 1
        ticket.detector_verdict = (n + 1, False)
    last = MAX_DETECTOR_CALLS + 1
    tracker.observe(msg(last, (last - 1) * step))
    assert kinds(tracker, (last - 1) * step + 1) == []
    assert kinds(tracker, (last - 1) * step + config.detector_debounce_s) == [("summarise", "forced")]
