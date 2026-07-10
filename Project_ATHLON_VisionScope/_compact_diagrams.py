"""Remove blank lines inside ```text fenced code blocks."""
from __future__ import annotations

import re
import sys
from pathlib import Path

FENCE_OPEN = re.compile(r"^(`{3,}|~{3,})\s*(\w*)?\s*$")


def compact_text_blocks(src: str) -> tuple[str, int]:
    lines = src.splitlines()
    out: list[str] = []
    in_fence = False
    fence_lang = ""
    block_lines: list[str] = []
    compacted_blocks = 0

    for line in lines:
        if FENCE_OPEN.match(line.strip()):
            if not in_fence:
                in_fence = True
                match = FENCE_OPEN.match(line.strip())
                fence_lang = (match.group(2) or "").lower() if match else ""
                block_lines = []
                out.append(line)
                continue

            if fence_lang == "text":
                compacted = [block_line for block_line in block_lines if block_line.strip()]
                if len(compacted) != len(block_lines):
                    compacted_blocks += 1
                out.extend(compacted)
                out.append(line)
            else:
                out.extend(block_lines)
                out.append(line)

            in_fence = False
            fence_lang = ""
            block_lines = []
            continue

        if in_fence:
            block_lines.append(line)
        else:
            out.append(line)

    result = "\n".join(out)
    if src.endswith("\n"):
        result += "\n"
    return result, compacted_blocks


def main() -> None:
    base = Path(__file__).resolve().parent
    targets = (
        [Path(arg) for arg in sys.argv[1:]]
        if len(sys.argv) > 1
        else sorted(base.glob("Project_Athlon_Book_Chapter_*.md"))
    )

    for target in targets:
        original = target.read_text(encoding="utf-8")
        updated, count = compact_text_blocks(original)
        target.write_text(updated, encoding="utf-8")

        orig_blocks = re.findall(r"```text\n(.*?)```", original, re.S)
        new_blocks = re.findall(r"```text\n(.*?)```", updated, re.S)
        orig_blanks = sum(
            1 for block in orig_blocks for line in block.splitlines() if not line.strip()
        )
        new_blanks = sum(
            1 for block in new_blocks for line in block.splitlines() if not line.strip()
        )

        print(f"{target.name}: compacted {count} block(s), removed {orig_blanks - new_blanks} blank line(s)")


if __name__ == "__main__":
    main()
