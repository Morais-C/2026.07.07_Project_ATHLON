"""Convert ```yaml fenced blocks to ```json across manuscript chapters."""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path
from typing import Any

try:
    import yaml
except ImportError:  # pragma: no cover
    yaml = None  # type: ignore

FENCE_OPEN = re.compile(r"^(`{3,}|~{3,})\s*(\w*)?\s*$")
KEY_LINE = re.compile(r"^([A-Za-z][A-Za-z0-9 ]*):\s*(.*)$")
LIST_ITEM = re.compile(r"^-\s+(.*)$")


def compact_block_lines(lines: list[str]) -> list[str]:
    return [line for line in lines if line.strip()]


def parse_loose_yaml(text: str) -> dict[str, Any]:
    """Parse manuscript YAML variants that are not always strict YAML."""
    lines = compact_block_lines(text.splitlines())
    result: dict[str, Any] = {}
    i = 0

    while i < len(lines):
        line = lines[i]
        list_match = LIST_ITEM.match(line)
        if list_match:
            raise ValueError("Top-level list items are not supported in loose parser")

        key_match = KEY_LINE.match(line)
        if not key_match:
            raise ValueError(f"Unrecognized line: {line!r}")

        key, inline_value = key_match.group(1), key_match.group(2).strip()
        i += 1

        if inline_value:
            result[key] = coerce_scalar(inline_value)
            continue

        value_lines: list[str] = []
        while i < len(lines):
            next_line = lines[i]
            if KEY_LINE.match(next_line) and not next_line.startswith(" "):
                break
            if LIST_ITEM.match(next_line):
                break
            value_lines.append(next_line.strip())
            i += 1

        if not value_lines:
            result[key] = None
        elif len(value_lines) == 1:
            result[key] = coerce_scalar(value_lines[0])
        else:
            result[key] = [coerce_scalar(v) for v in value_lines]

    return result


def coerce_scalar(value: str) -> Any:
    if re.fullmatch(r"-?\d+", value):
        return int(value)
    if re.fullmatch(r"-?\d+\.\d+", value):
        return float(value)
    if value.lower() in {"true", "false"}:
        return value.lower() == "true"
    if value.lower() == "null":
        return None
    return value


def yaml_block_to_object(text: str) -> Any:
    compact = "\n".join(compact_block_lines(text.splitlines()))
    if yaml is not None:
        try:
            loaded = yaml.safe_load(compact)
            if loaded is not None:
                return loaded
        except yaml.YAMLError:
            pass
    return parse_loose_yaml(text)


def to_json_text(value: Any) -> str:
    return json.dumps(value, indent=2, ensure_ascii=False)


def convert_markdown(src: str) -> tuple[str, list[str]]:
    lines = src.splitlines()
    out: list[str] = []
    in_fence = False
    fence_lang = ""
    block_lines: list[str] = []
    reports: list[str] = []

    for line in lines:
        if FENCE_OPEN.match(line.strip()):
            if not in_fence:
                in_fence = True
                match = FENCE_OPEN.match(line.strip())
                fence_lang = (match.group(2) or "").lower() if match else ""
                block_lines = []
                out.append(line)
                continue

            if fence_lang in {"yaml", "yml"}:
                block_text = "\n".join(block_lines)
                try:
                    obj = yaml_block_to_object(block_text)
                    json_body = to_json_text(obj)
                    out[ -1 ] = "```json"
                    out.append(json_body)
                    out.append(line)
                    preview = json_body.splitlines()[0]
                    if len(json_body.splitlines()) > 1:
                        preview += " ..."
                    reports.append(f"converted block -> {preview}")
                except Exception as exc:  # noqa: BLE001
                    out.extend(block_lines)
                    reports.append(f"FAILED conversion: {exc}")
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
    return result, reports


def main() -> None:
    base = Path(__file__).resolve().parent
    targets = [Path(arg) for arg in sys.argv[1:]] if len(sys.argv) > 1 else [
        base / "Project_Athlon_Book_Chapter_11_Engineering_Agent_Behavior.md"
    ]

    for target in targets:
        original = target.read_text(encoding="utf-8")
        updated, reports = convert_markdown(original)
        target.write_text(updated, encoding="utf-8")
        print(f"\n{target.name}")
        if reports:
            for report in reports:
                print(f"  - {report}")
        else:
            print("  - no yaml blocks found")


if __name__ == "__main__":
    main()
