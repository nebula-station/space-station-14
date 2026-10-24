#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Yaroslav Yudaev <ydaevy10@gmail.com>
# SPDX-License-Identifier: MIT

import argparse
import os
import re
from dataclasses import dataclass
from pathlib import Path


DEFAULT_COMMENT_LIMIT = 60000
MAX_HEADER_LENGTH = 12000


@dataclass(frozen=True)
class PreparedComment:
    body: str
    oversized: bool
    original_length: int
    omitted_groups: int


def split_rsi_details(markdown: str) -> tuple[str, list[str]]:
    first_details = markdown.find("<details>")
    if first_details == -1:
        return markdown[:MAX_HEADER_LENGTH].rstrip(), []

    header = markdown[:first_details].rstrip()
    details = re.findall(r"<details>.*?</details>", markdown[first_details:], flags=re.S)
    return header, details


def prepare_comment(markdown: str, run_url: str, comment_limit: int = DEFAULT_COMMENT_LIMIT) -> PreparedComment:
    if len(markdown) <= comment_limit:
        return PreparedComment(
            body=markdown,
            oversized=False,
            original_length=len(markdown),
            omitted_groups=0,
        )

    header, details = split_rsi_details(markdown)
    notice = oversized_notice(markdown, run_url)
    intro = (
        f"{header}\n\n"
        f"{notice}\n\n"
    )

    if not details:
        body = cap_without_cutting_words(f"{notice}\n\n{header}", comment_limit)
        return PreparedComment(
            body=body,
            oversized=True,
            original_length=len(markdown),
            omitted_groups=0,
        )

    selected: list[str] = []
    for index, block in enumerate(details):
        omitted = len(details) - index - 1
        suffix = omitted_suffix(omitted)
        candidate = intro + "".join(selected) + block + suffix
        if len(candidate) > comment_limit:
            break
        selected.append(block)

    omitted = len(details) - len(selected)
    body = intro + "".join(selected) + omitted_suffix(omitted)
    return PreparedComment(
        body=cap_without_cutting_words(body, comment_limit),
        oversized=True,
        original_length=len(markdown),
        omitted_groups=omitted,
    )


def omitted_suffix(count: int) -> str:
    return (
        f"\n\n_Omitted {count} RSI group(s) because GitHub comments are limited "
        "to 65,536 characters._\n"
    )


def oversized_notice(markdown: str, run_url: str) -> str:
    return (
        "> Full RSI diff is too large for one GitHub comment "
        f"({len(markdown)} characters, GitHub limit is 65,536). "
        "This comment shows the first entries that fit. "
        f"Download the full `rsi-diff-full.md` artifact from the [workflow run]({run_url})."
    )


def cap_without_cutting_words(text: str, limit: int) -> str:
    if len(text) <= limit:
        return text

    marker = "\n\n_Comment truncated to fit GitHub's 65,536 character limit._\n"
    if limit <= len(marker):
        return text[:limit]

    return text[: limit - len(marker)].rstrip() + marker


def write_github_outputs(path: str, prepared: PreparedComment):
    with open(path, "a", encoding="utf-8") as output:
        output.write(f"oversized={str(prepared.oversized).lower()}\n")
        output.write(f"original-length={prepared.original_length}\n")
        output.write(f"comment-length={len(prepared.body)}\n")
        output.write(f"omitted-groups={prepared.omitted_groups}\n")


def parse_args():
    parser = argparse.ArgumentParser(description="Prepare an RSI diff PR comment body.")
    parser.add_argument("--input", required=True, help="Path to the full RSI diff markdown.")
    parser.add_argument("--output", required=True, help="Path to write the capped PR comment markdown.")
    parser.add_argument("--run-url", required=True, help="Workflow run URL for the full diff artifact.")
    parser.add_argument(
        "--comment-limit",
        type=int,
        default=DEFAULT_COMMENT_LIMIT,
        help="Maximum PR comment body length to write. Defaults to a safe value below GitHub's limit.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    markdown = Path(args.input).read_text(encoding="utf-8")
    prepared = prepare_comment(markdown, args.run_url, args.comment_limit)
    Path(args.output).write_text(prepared.body, encoding="utf-8")

    github_output = os.environ.get("GITHUB_OUTPUT")
    if github_output:
        write_github_outputs(github_output, prepared)

    print(f"RSI diff markdown length: {prepared.original_length}")
    print(f"PR comment length: {len(prepared.body)}")
    print(f"Oversized: {prepared.oversized}")
    print(f"Omitted RSI groups: {prepared.omitted_groups}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
