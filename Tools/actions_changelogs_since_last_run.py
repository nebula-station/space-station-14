#!/usr/bin/env python3

"""
Sends updates to a Discord webhook for new changelog entries since the last GitHub Actions publish run.

Automatically figures out the last run and changelog contents with the GitHub API.
"""

import os
import re
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable
from urllib.parse import parse_qsl, urlencode, urlparse

import json
import requests
import yaml

DEBUG = False
DEBUG_CHANGELOG_FILE_OLD = Path("Resources/Changelog/Old.yml")
GITHUB_API_URL = os.environ.get("GITHUB_API_URL", "https://api.github.com")

# https://discord.com/developers/docs/resources/webhook
DISCORD_EMBED_DESCRIPTION_LIMIT = 3900
MAX_CHANGE_MESSAGE_LENGTH = 1000
DISCORD_MESSAGE_DELAY_SECONDS = 0.5
NEBULA_EMBED_COLOR = 0x6D3286

CHANGELOG_FILE = "Resources/Changelog/Nebula.yml" # Nebula edit

TYPES_TO_EMOJI = {"Fix": "🐛", "Add": "🆕", "Remove": "❌", "Tweak": "⚒️"}
TYPES_TO_LABELS = {
    "Fix": "Исправлено",
    "Add": "Добавлено",
    "Remove": "Удалено",
    "Tweak": "Изменено",
}

DISCORD_COMPONENTS_V2_FLAG = 1 << 15
DISCORD_MEDIA_GALLERY_MAX_ITEMS = 10

HTTP_TIMEOUT_SECONDS = 30
MEDIA_MAX_DOWNLOAD_SIZE = 10 * 1024 * 1024
MEDIA_READ_CHUNK_SIZE = 64 * 1024
MEDIA_FILE_EXTENSIONS = frozenset({
    ".apng", ".avif", ".bmp", ".gif", ".jpeg", ".jpg",
    ".m4v", ".mkv", ".mov", ".mp4", ".png", ".webm", ".webp",
})
_media_counter = 0

ChangelogEntry = dict[str, Any]

class MediaTypeError(RuntimeError):
    pass


def iter_media_urls(entry: ChangelogEntry) -> Iterable[str]:
    media = entry.get("media") or []
    if not isinstance(media, list):
        return
    seen: set[str] = set()
    for value in media:
        url = str(value or "").strip()
        parsed = urlparse(url)
        if parsed.scheme not in {"http", "https"} or not parsed.netloc:
            continue
        if parsed.username or parsed.password or len(url) > 2000:
            continue
        if url in seen:
            continue
        seen.add(url)
        yield url


def detect_media_type(data: bytes) -> tuple[str, str]:
    if data.startswith(b"\xff\xd8\xff"):
        return "image/jpeg", ".jpg"
    if data.startswith(b"\x89PNG\r\n\x1a\n"):
        return "image/png", ".png"
    if data.startswith((b"GIF87a", b"GIF89a")):
        return "image/gif", ".gif"
    if data.startswith(b"RIFF") and data[8:12] == b"WEBP":
        return "image/webp", ".webp"
    if data.startswith(b"\x1a\x45\xdf\xa3"):
        return "video/webm", ".webm"
    if len(data) >= 12 and data[4:8] == b"ftyp":
        if data[8:12] == b"qt  ":
            return "video/quicktime", ".mov"
        return "video/mp4", ".mp4"
    raise MediaTypeError("unrecognized media content")


def download_media(session: requests.Session, url: str):
    global _media_counter
    try:
        response = session.get(url, stream=True, timeout=HTTP_TIMEOUT_SECONDS)
        response.raise_for_status()

        length = response.headers.get("Content-Length")
        if length and length.isdigit() and int(length) > MEDIA_MAX_DOWNLOAD_SIZE:
            print(f"Media too large ({url}): {length} bytes")
            return None

        data = bytearray()
        for chunk in response.iter_content(chunk_size=MEDIA_READ_CHUNK_SIZE):
            data.extend(chunk)
            if len(data) > MEDIA_MAX_DOWNLOAD_SIZE:
                print(f"Media exceeded the size cap ({url})")
                return None
    except requests.RequestException as exc:
        print(f"Failed to download media ({url}): {exc}")
        return None

    try:
        mime, ext = detect_media_type(bytes(data))
    except MediaTypeError:
        ext = Path(urlparse(url).path).suffix.lower()
        if ext not in MEDIA_FILE_EXTENSIONS:
            print(f"Unrecognized media type ({url}); falling back to a link")
            return None
        mime = "application/octet-stream"

    _media_counter += 1
    return f"changelog-media-{_media_counter}{ext}", mime, bytes(data), url

def main():
    webhook_nebula = os.environ.get("DISCORD_WEBHOOK_URL")

    if not webhook_nebula:
        raise RuntimeError("DISCORD_WEBHOOK_URL is not set")

    if DEBUG:
        # to debug this script locally, you can use
        # a separate local file as the old changelog
        last_changelog_stream = DEBUG_CHANGELOG_FILE_OLD.read_text()
        publish_context = None
    else:
        # when running this normally in a GitHub actions workflow,
        # it will get the old changelog from the GitHub API
        last_changelog_stream, publish_context = get_last_changelog()
        if last_changelog_stream is None:
            # Nebula edit
            print("No previous successful publish run found; using this run as the changelog baseline")
            return

    last_changelog = yaml.safe_load(last_changelog_stream)
    with open(CHANGELOG_FILE, "r") as f:
        cur_changelog = yaml.safe_load(f)

    diff = list(diff_changelog(last_changelog, cur_changelog))
    entries = list(group_changelog_entries_by_pull_request(diff))

    if not entries:
        print("No new changelog entries since the last successful publish")
        return

    change_count = sum(len(entry.get("changes") or []) for entry in entries)
    print(
        f"Sending {len(entries)} pull request changelog entries "
        f"with {change_count} changes since the last successful publish"
    )
    send_changelog(entries, publish_context)

def get_most_recent_workflow(
    sess: requests.Session, current_run: dict[str, Any]
) -> Any | None:
    past_runs = get_past_runs(sess, current_run)
    for run in past_runs["workflow_runs"]:
        # First past successful run that isn't our current run.
        if run["id"] == current_run["id"]:
            continue

        return run

    return None # Nebula edit


def get_current_run(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/actions/runs/{github_run}"
    )
    resp.raise_for_status()
    return resp.json()


def get_past_runs(sess: requests.Session, current_run: Any) -> Any:
    """
    Get all successful workflow runs before our current one.
    """
    params = {"status": "success", "created": f"<={current_run['created_at']}"}
    resp = sess.get(f"{current_run['workflow_url']}/runs", params=params)
    resp.raise_for_status()
    return resp.json()


def get_last_changelog() -> tuple[str | None, dict[str, Any] | None]:
    github_repository = os.environ["GITHUB_REPOSITORY"]
    github_run = os.environ["GITHUB_RUN_ID"]
    github_token = os.environ["GITHUB_TOKEN"]

    session = requests.Session()
    session.headers["Authorization"] = f"Bearer {github_token}"
    session.headers["Accept"] = "application/vnd.github+json"
    session.headers["X-GitHub-Api-Version"] = "2022-11-28"

    current_run = get_current_run(session, github_repository, github_run)
    # Nebula edit start
    since_sha = os.environ.get("CHANGELOG_SINCE_SHA", "").strip()
    if since_sha:
        most_recent = {}
        last_sha = since_sha
        print(f"Using changelog baseline override: {last_sha}")
    else:
        most_recent = get_most_recent_workflow(session, current_run)
        if most_recent is None:
            return None, None
        last_sha = most_recent["head_sha"]
        print(f"Last successful publish job was {most_recent['id']}: {last_sha}")
    # Nebula edit end
    last_changelog_stream = get_last_changelog_by_sha(
        session, last_sha, github_repository
    )

    return last_changelog_stream, {
        "github_repository": github_repository,
        "current_run": current_run,
        "previous_run": most_recent,
    }


def get_last_changelog_by_sha(
    sess: requests.Session, sha: str, github_repository: str
) -> str:
    """
    Use GitHub API to get the previous version of the changelog YAML (Actions builds are fetched with a shallow clone)
    """
    params = {
        "ref": sha,
    }
    headers = {"Accept": "application/vnd.github.raw"}

    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/contents/{CHANGELOG_FILE}",
        headers=headers,
        params=params,
    )
    resp.raise_for_status()
    return resp.text


def diff_changelog(
    old: dict[str, Any], cur: dict[str, Any]
) -> Iterable[ChangelogEntry]:
    """
    Find all new entries not present in the previous publish.
    """
    old_entry_keys = {get_entry_pull_request_key(e) for e in old.get("Entries", [])}
    return (
        e
        for e in cur.get("Entries", [])
        if get_entry_pull_request_key(e) not in old_entry_keys
    )


def group_changelog_entries_by_pull_request(
    entries: Iterable[ChangelogEntry],
) -> Iterable[ChangelogEntry]:
    """
    Discord messages are grouped by PR so players can react to every PR separately.
    """
    grouped: dict[str, ChangelogEntry] = {}

    for entry in entries:
        changes = list(entry.get("changes") or [])
        if not changes:
            print(f"Skipping changelog entry {entry.get('id')} without changes")
            continue

        key = get_entry_pull_request_key(entry)
        existing = grouped.get(key)
        if existing:
            existing.setdefault("changes", []).extend(changes)
            continue

        grouped[key] = dict(entry, changes=changes)

    return grouped.values()


def get_entry_pull_request_key(entry: ChangelogEntry) -> str:
    url = str(entry.get("url") or "").strip()
    if url:
        return url

    return f"{entry.get('id')}:{entry.get('title')}"


def get_discord_body(embed: dict[str, Any]) -> dict[str, Any]:
    return {
        "embeds": [embed],
        "allowed_mentions": {"parse": []},
        "flags": 0,
    }

def entry_to_container(entry, media_files, link_urls):
    embed = entry_to_embed(entry)
    components = [
        {"type": 10, "content": f"**{embed['title']}**" if not embed.get("url")
            else f"**[{embed['title']}]({embed['url']})**"},
        {"type": 14, "divider": True, "spacing": 1},
        {"type": 10, "content": embed["description"]},
    ]
    if link_urls:
        components.append({"type": 10, "content": "\n".join(f"🔗 {u}" for u in link_urls)})
    if media_files:
        components.append({"type": 14, "divider": True, "spacing": 1})
        components.append({"type": 12, "items": [
            {"media": {"url": f"attachment://{name}"}} for name, _, _, _ in media_files
        ]})
    components.append({"type": 14, "divider": True, "spacing": 1})
    components.append({"type": 10, "content": f"-# {embed['footer']['text']}"})
    return {"type": 17, "accent_color": NEBULA_EMBED_COLOR, "components": components}

def cv2_url(webhook_url):
    p = urlparse(webhook_url)
    q = dict(parse_qsl(p.query)); q["with_components"] = "true"
    return p._replace(query=urlencode(q)).geturl()

def send_with_retry(
    webhook_url: str,
    body: dict[str, Any],
    name: str,
    files: list | None = None,
) -> None:
    retry_attempt = 0
    max_retries = 30

    kwargs: dict[str, Any] = {"timeout": 60}
    if files:
        kwargs["data"] = {"payload_json": json.dumps(body)}
        kwargs["files"] = files
    else:
        kwargs["json"] = body

    while True:
        try:
            response = requests.post(cv2_url(webhook_url), **kwargs)
        except requests.exceptions.RequestException as e:
            retry_attempt += 1
            if retry_attempt > max_retries:
                raise RuntimeError(f"[{name}] Failed to send message") from e
            retry_after = min(30, 2 ** min(retry_attempt, 5))
            print(f"[{name}] Request failed, retrying after {retry_after} seconds")
            time.sleep(retry_after)
            continue

        if response.status_code in (429, 500, 502, 503, 504):
            retry_attempt += 1
            if retry_attempt > max_retries:
                raise RuntimeError(f"[{name}] Too many retries, giving up")
            retry_after = get_retry_after(response, retry_attempt)
            print(
                f"[{name}] Discord returned {response.status_code}, "
                f"retrying after {retry_after} seconds"
            )
            time.sleep(retry_after)
            continue

        if not response.ok:
            raise RuntimeError(
                f"[{name}] Discord rejected the message "
                f"({response.status_code}): {response.text}"
            )

        print(f"Sent to {name} webhook")
        break

def get_retry_after(response: requests.Response, retry_attempt: int) -> float:
    try:
        retry_after = float(response.json().get("retry_after", 0))
    except (AttributeError, ValueError, TypeError):
        retry_after = 0

    if retry_after <= 0:
        retry_after_header = response.headers.get("Retry-After")
        if retry_after_header:
            try:
                retry_after = float(retry_after_header)
            except ValueError:
                retry_after = 0

    if retry_after <= 0:
        retry_after = min(30, 2 ** min(retry_attempt, 5))

    return retry_after + 0.25


def send_entry(session: requests.Session, entry: ChangelogEntry, name: str) -> None:
    media_files, link_urls = [], []
    for url in iter_media_urls(entry):
        downloaded = download_media(session, url)
        if downloaded:
            media_files.append(downloaded)
        else:
            link_urls.append(url)

    link_urls += [f[3] for f in media_files[DISCORD_MEDIA_GALLERY_MAX_ITEMS:]]
    media_files = media_files[:DISCORD_MEDIA_GALLERY_MAX_ITEMS]

    body: dict[str, Any] = {
        "components": [entry_to_container(entry, media_files, link_urls)],
        "flags": DISCORD_COMPONENTS_V2_FLAG,
        "allowed_mentions": {"parse": []},
    }
    if media_files:
        body["attachments"] = [
            {"id": i, "filename": f[0]} for i, f in enumerate(media_files)
        ]
    files = [(f"files[{i}]", (f[0], f[2], f[1])) for i, f in enumerate(media_files)]

    send_discord_webhook(body, name, files)

def send_discord_webhook(body: dict[str, Any], message_name: str, files=None) -> None:
    webhook_urls = [
        os.environ.get("DISCORD_WEBHOOK_URL"),
    ]

    if not any(webhook_urls):
        raise RuntimeError("No Discord webhooks configured!")

    for webhook_url in webhook_urls:
        if webhook_url:
            send_with_retry(webhook_url, body, message_name, files)

def changelog_entries_to_embeds(entries: Iterable[ChangelogEntry]) -> list[dict[str, Any]]:
    """Process structured changelog entries into Discord embeds."""
    return [entry_to_embed(entry) for entry in entries]


def entry_to_embed(entry: ChangelogEntry) -> dict[str, Any]:
    lines: list[str] = []
    grouped_changes: dict[str, list[str]] = {}

    for change in entry.get("changes") or []:
        change_type = str(change.get("type") or "Other")
        emoji = TYPES_TO_EMOJI.get(change_type, "❓")
        message = truncate(str(change.get("message") or ""), MAX_CHANGE_MESSAGE_LENGTH)
        if not message:
            continue

        grouped_changes.setdefault(change_type, []).append(f"- {emoji} {message}")

    for change_type, changes in grouped_changes.items():
        if lines:
            lines.append("")
        label = TYPES_TO_LABELS.get(change_type, "Прочее")
        lines.append(f"**{label}**")
        lines.extend(changes)

    description = truncate(
        "\n".join(lines) or "Без описанных изменений",
        DISCORD_EMBED_DESCRIPTION_LIMIT,
    )
    title = format_entry_title(entry)

    embed: dict[str, Any] = {
        "title": title,
        "description": description,
        "color": NEBULA_EMBED_COLOR,
        "footer": {
            "text": truncate(str(entry.get("author") or "Unknown"), 256),
        },
    }

    url = entry.get("url")
    if url and str(url).strip():
        embed["url"] = str(url)

    avatar_url = entry.get("avatar_url")
    if avatar_url and str(avatar_url).strip():
        embed["footer"]["icon_url"] = str(avatar_url)

    timestamp = entry.get("time")
    if timestamp:
        embed["timestamp"] = str(timestamp)

    return embed


def format_entry_title(entry: ChangelogEntry) -> str:
    title = str(entry.get("title") or "Changelog update")
    pr_number = get_pull_request_number(entry.get("url"))
    if pr_number:
        title = f"#{pr_number} - {title}"

    return truncate(title, 256)


def get_pull_request_number(url: Any) -> str | None:
    if not url:
        return None

    match = re.search(r"/pull/(\d+)", str(url))
    if not match:
        return None

    return match.group(1)


def create_header_embed(
    entries: list[ChangelogEntry], publish_context: dict[str, Any] | None
) -> dict[str, Any]:
    change_count = sum(len(entry.get("changes") or []) for entry in entries)
    current_run = (publish_context or {}).get("current_run") or {}
    previous_run = (publish_context or {}).get("previous_run") or {}
    github_repository = (publish_context or {}).get("github_repository")

    previous_update = format_discord_date(
        previous_run.get("updated_at") or previous_run.get("created_at")
    )
    version = format_version_link(github_repository, current_run.get("head_sha"))

    description = (
        f"Предыдущее обновление: {previous_update}\n"
        f"Изменений: **{format_plural(len(entries), 'PR', 'PR', 'PR')}**, "
        f"**{format_plural(change_count, 'пункт', 'пункта', 'пунктов')}**\n"
        f"Версия: {version}"
    )

    embed: dict[str, Any] = {
        "title": "Небула - обновление серверов",
        "description": description,
        "color": NEBULA_EMBED_COLOR,
    }

    if github_repository and current_run.get("head_sha"):
        embed["url"] = get_commit_tree_url(github_repository, current_run["head_sha"])

    current_timestamp = current_run.get("updated_at") or current_run.get("created_at")
    if current_timestamp:
        embed["timestamp"] = str(current_timestamp)

    return embed


def format_discord_date(value: Any) -> str:
    if not value:
        return "неизвестно"

    try:
        timestamp = datetime.fromisoformat(str(value).replace("Z", "+00:00"))
    except ValueError:
        return str(value)

    if timestamp.tzinfo is None:
        timestamp = timestamp.replace(tzinfo=timezone.utc)

    return f"<t:{int(timestamp.timestamp())}:d>"


def format_version_link(github_repository: Any, sha: Any) -> str:
    if not sha:
        return "неизвестно"

    short_sha = str(sha)[:7]
    if not github_repository:
        return short_sha

    return f"[{short_sha}]({get_commit_tree_url(github_repository, sha)})"


def format_plural(value: int, one: str, few: str, many: str) -> str:
    remainder = value % 100
    if 11 <= remainder <= 14:
        word = many
    else:
        last_digit = value % 10
        if last_digit == 1:
            word = one
        elif 2 <= last_digit <= 4:
            word = few
        else:
            word = many

    return f"{value} {word}"


def get_commit_tree_url(github_repository: Any, sha: Any) -> str:
    return f"https://github.com/{github_repository}/tree/{sha}"


def truncate(value: str, limit: int) -> str:
    if len(value) <= limit:
        return value

    return value[: limit - 6].rstrip() + " [...]"


def send_changelog(
    entries: list[ChangelogEntry],
    publish_context: dict[str, Any] | None,
) -> None:
    with requests.Session() as session:
        for index, entry in enumerate(entries, start=1):
            send_entry(session, entry, f"Nebula changelog PR {index}/{len(entries)}")
            if index != len(entries):
                time.sleep(DISCORD_MESSAGE_DELAY_SECONDS)

if __name__ == "__main__":
    main()
