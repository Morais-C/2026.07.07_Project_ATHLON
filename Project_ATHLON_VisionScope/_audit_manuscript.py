"""Audit manuscript markdown for common formatting defects."""
from __future__ import annotations

import re
import sys
from pathlib import Path

FENCE_OPEN = re.compile(r"^(`{3,}|~{3,})\s*(\w*)?\s*$")


def audit_file(path: Path) -> list[str]:
    issues: list[str] = []
    lines = path.read_text(encoding="utf-8").splitlines()
    in_fence = False
    fence_lang = ""
    fence_start = 0
    block_lines: list[str] = []

    for i, line in enumerate(lines, start=1):
        stripped = line.strip()
        if FENCE_OPEN.match(stripped):
            if not in_fence:
                in_fence = True
                match = FENCE_OPEN.match(stripped)
                fence_lang = (match.group(2) or "").lower() if match else ""
                fence_start = i
                block_lines = []
                continue

            # closing fence
            if fence_lang == "text":
                blank_inside = sum(1 for bl in block_lines if not bl.strip())
                if blank_inside:
                    issues.append(
                        f"L{fence_start}: text block has {blank_inside} blank line(s) "
                        f"(lines {fence_start}-{i})"
                    )
            in_fence = False
            fence_lang = ""
            block_lines = []
            continue

        if in_fence:
            block_lines.append(line)
            continue

        # loose arrow lines outside fences
        if stripped == "↓":
            prev_nonempty = ""
            for back in range(i - 2, -1, -1):
                if lines[back].strip():
                    prev_nonempty = lines[back].strip()
                    break
            if prev_nonempty and not prev_nonempty.startswith("#"):
                issues.append(
                    f"L{i}: loose arrow outside code block (after '{prev_nonempty[:40]}')"
                )

    # unclosed fence
    if in_fence:
        issues.append(f"L{fence_start}: unclosed code block ({fence_lang or 'plain'})")

    # broken json/yaml: opening fence then prose without closing
    content = path.read_text(encoding="utf-8")
    for m in re.finditer(r"```(json|yaml|yml)\n([\s\S]*?)$", content, re.M):
        # if block never closes before EOF or next section wrongly included
        pass

    # check each json block specifically
    pos = 0
    while True:
        start = content.find("```json", pos)
        if start == -1:
            break
        after_open = content.find("\n", start) + 1
        close = content.find("\n```", after_open)
        if close == -1:
            line_no = content[:start].count("\n") + 1
            issues.append(f"L{line_no}: ```json block missing closing fence")
        else:
            body = content[after_open:close]
            # body should end with } or ] for object/array examples
            tail = body.rstrip().splitlines()[-1] if body.strip() else ""
            if tail and not tail.endswith(("}", "]", '"', "'")):
                line_no = content[:after_open].count("\n") + body.count("\n") + 1
                issues.append(
                    f"L{line_no}: ```json block may include prose (missing closing fence?)"
                )
        pos = start + 7

    return issues


def main() -> None:
    base = Path(__file__).resolve().parent
    targets = sorted(base.glob("Project_Athlon_Book_Chapter_*.md"))
    if len(sys.argv) > 1:
        targets = [Path(p) for p in sys.argv[1:]]

    total = 0
    for target in targets:
        issues = audit_file(target)
        if issues:
            print(f"\n{target.name} ({len(issues)} issue(s))")
            for issue in issues:
                print(f"  - {issue.encode('ascii', 'replace').decode('ascii')}")
            total += len(issues)

    if total == 0:
        print("No issues found.")
    else:
        print(f"\nTotal: {total} issue(s) in {len(targets)} file(s)")


if __name__ == "__main__":
    main()
