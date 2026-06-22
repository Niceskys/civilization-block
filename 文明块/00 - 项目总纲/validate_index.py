import os
import re
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve()
PROJECT_ROOT = SCRIPT_PATH.parents[2]
INDEX_PATH = PROJECT_ROOT / "文明块" / "00 - 项目总纲" / "00.4 全库Markdown审计索引.md"

IGNORED_DIRS = {
    ".git",
    ".obsidian",
    ".appdata",
    ".dotnet_home",
    ".nuget_packages",
    "bin",
    "obj",
}


def iter_actual_markdown(root):
    """Walk the repository and return a set of relative paths to all .md files."""
    paths = set()
    for current_root, dirs, files in os.walk(root):
        dirs[:] = [d for d in dirs if d not in IGNORED_DIRS]
        for filename in files:
            if filename.lower().endswith(".md"):
                full_path = Path(current_root) / filename
                paths.add(full_path.relative_to(root).as_posix())
    return paths


def read_index_paths():
    """Parse the index table and return a list of indexed markdown paths."""
    if not INDEX_PATH.exists():
        print(f"ERROR: Index file not found: {INDEX_PATH}")
        print("Please run generate_index.py first to create the index.")
        raise SystemExit(1)

    indexed = []
    in_table = False
    for line in INDEX_PATH.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if stripped.startswith("| 序号 | 完整相对路径"):
            in_table = True
            continue

        if in_table and (not stripped.startswith("|") or stripped.startswith("---")):
            break

        if not in_table or "------" in stripped:
            continue

        parts = [part.strip() for part in stripped.split("|")]
        if len(parts) < 4:
            continue

        seq = parts[1]
        rel_path = parts[2].replace("\\", "/")
        if seq.isdigit() and rel_path.endswith(".md"):
            indexed.append(rel_path)

    if not in_table:
        print('ERROR: Could not find table header "| 序号 | 完整相对路径" in index file.')
        print("The index file may be malformed or the header format has changed.")
        raise SystemExit(1)

    return indexed


def find_duplicate_paths(paths):
    """Return a set of paths that appear more than once in the given list."""
    seen = set()
    duplicates = set()
    for path in paths:
        if path in seen:
            duplicates.add(path)
        seen.add(path)
    return duplicates


def check_status_for_building_details():
    """Check that all files in 单个建筑详情 have a status field in frontmatter."""
    building_dir = PROJECT_ROOT / "文明块" / "02 - 建筑系统" / "单个建筑详情"
    if not building_dir.exists():
        return []

    missing = []
    for path in sorted(building_dir.glob("*.md")):
        content = path.read_text(encoding="utf-8", errors="replace").lstrip("\ufeff")
        if not content.startswith("---") or "status:" not in content[:500]:
            missing.append(path.name)
    return missing


def main():
    """Validate the index against actual markdown files in the repository."""
    actual = iter_actual_markdown(PROJECT_ROOT)
    indexed = read_index_paths()
    indexed_set = set(indexed)
    duplicates = find_duplicate_paths(indexed)

    missing = actual - indexed_set
    extra = indexed_set - actual
    self_path = "文明块/00 - 项目总纲/00.4 全库Markdown审计索引.md"
    self_in = self_path in indexed_set

    print(f"ACTUAL_MD_FILES={len(actual)}")
    print(f"INDEXED_PATHS={len(indexed)}")
    print(f"MISSING={len(missing)} extra={len(extra)} duplicate={len(duplicates)}")

    if missing:
        for path in sorted(missing):
            print(f"  MISSING: {path}")

    if extra:
        for path in sorted(extra):
            print(f"  EXTRA: {path}")

    if duplicates:
        for path in sorted(duplicates):
            print(f"  DUPLICATE: {path}")

    print(f"00.4_INCLUDES_SELF={self_in}")

    no_status = check_status_for_building_details()
    print("\nBUILDING_DETAIL_STATUS_CHECK:")
    print(f"  Files without status: {len(no_status)}")
    for filename in no_status:
        print(f"    {filename}")

    passed = not missing and not extra and not duplicates and self_in
    print("\n=== INDEX CHECK COMPLETE ===")
    print(f"FINAL_RESULT={'PASS' if passed else 'FAIL'}")

    if not passed:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
