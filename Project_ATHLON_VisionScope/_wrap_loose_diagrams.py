"""Wrap loose arrow diagrams in ```text blocks and compact them."""
from __future__ import annotations

import re
import sys
from pathlib import Path

FENCE_OPEN = re.compile(r"^(`{3,}|~{3,})\s*(\w*)?\s*$")
ARROW = "\u2193"  # ↓


def is_diagram_line(line: str) -> bool:
    stripped = line.strip()
    if not stripped:
        return True
    if stripped == ARROW:
        return True
    if stripped.startswith("#"):
        return False
    if stripped.startswith(("-", "*", "|", "`", ">", "[")):
        return False
    if stripped.endswith(":"):
        return False
    if re.match(r"^\d+\.\s", stripped):
        return True
    if stripped.endswith("."):
        return len(stripped) <= 45 and stripped.count(".") == 1
    if any(ch in stripped for ch in ",;!?"):
        return False
    if len(stripped) > 60:
        return False
    return True


def wrap_loose_diagrams(src: str) -> tuple[str, int]:
    lines = src.splitlines()
    out: list[str] = []
    in_fence = False
    wrapped = 0
    i = 0

    while i < len(lines):
        line = lines[i]
        if FENCE_OPEN.match(line.strip()):
            in_fence = not in_fence
            out.append(line)
            i += 1
            continue

        if in_fence or not is_diagram_line(line):
            out.append(line)
            i += 1
            continue

        start = i
        while i < len(lines) and is_diagram_line(lines[i]):
            i += 1

        run = lines[start:i]
        non_blank = [part for part in run if part.strip()]
        has_arrow = any(part.strip() == ARROW for part in run)

        if has_arrow and len(non_blank) >= 3:
            compact = [part for part in run if part.strip()]
            out.append("```text")
            out.extend(compact)
            out.append("```")
            if i < len(lines) and lines[i].strip():
                out.append("")
            wrapped += 1
        else:
            out.extend(run)

    result = "\n".join(out)
    if src.endswith("\n"):
        result += "\n"
    return result, wrapped


def main() -> None:
    base = Path(__file__).resolve().parent
    targets = (
        [Path(arg) for arg in sys.argv[1:]]
        if len(sys.argv) > 1
        else sorted(base.glob("Project_Athlon_Book_Chapter_*.md"))
    )

    total = 0
    for target in targets:
        original = target.read_text(encoding="utf-8")
        updated, count = wrap_loose_diagrams(original)
        if count:
            target.write_text(updated, encoding="utf-8")
            print(f"{target.name}: wrapped {count} loose diagram(s)")
            total += count

    if total == 0:
        print("No loose diagrams found.")


if __name__ == "__main__":
    main()
