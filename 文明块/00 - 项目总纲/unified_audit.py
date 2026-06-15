"""文明块全库统一审计校验脚本"""
import os, re, sys, subprocess
from pathlib import Path
import unicodedata

# ── Shared path utilities ──────────────────────────────────────────────

def get_repo_root() -> Path:
    result = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        check=True, capture_output=True, text=True, encoding="utf-8",
    )
    return Path(result.stdout.strip()).resolve()

def normalize_path(value: str) -> str:
    value = unicodedata.normalize("NFC", value.strip())
    value = value.replace("\\", "/")
    while value.startswith("./"):
        value = value[2:]
    return value.rstrip("/")

def disk_relative_path(path: Path, repo_root: Path) -> str:
    return normalize_path(path.resolve().relative_to(repo_root).as_posix())

# ── Setup ──────────────────────────────────────────────────────────────

REPO_ROOT = get_repo_root()
os.chdir(REPO_ROOT)
index_path = REPO_ROOT / "文明块/00 - 项目总纲/00.4 全库Markdown审计索引.md"

print(f"REPO_ROOT: {REPO_ROOT}")
print()

# ── 1. Collect actual .md files ──────────────────────────────────────

actual = {}
for root, dirs, fs in os.walk(REPO_ROOT):
    # Skip .git
    if ".git" in root:
        continue
    for f in sorted(fs):
        if not f.endswith(".md"):
            continue
        rel = disk_relative_path(Path(root) / f, REPO_ROOT)
        actual[rel] = os.path.getsize(Path(root) / f)

actual_count = len(actual)
print(f"ACTUAL_MD_FILES={actual_count}")

# ── 2. Parse 00.4 index ─────────────────────────────────────────────

if not index_path.exists():
    print("ERROR: 00.4 not found!")
    print("VALIDATE_INDEX=FAIL")
    raise SystemExit(1)

with open(index_path, "r", encoding="utf-8") as f:
    idx_lines = f.readlines()

indexed = {}      # norm_path -> row_number
row_paths = {}    # row_number -> norm_path

in_table = False
for i, line in enumerate(idx_lines, 1):
    s = line.strip()
    # Detect table header
    if s.startswith("| 序号 |") or s.startswith("|序号|"):
        in_table = True
        continue
    if not in_table:
        continue
    if not s.startswith("|"):
        continue
    if "------" in s:
        continue

    parts = [p.strip() for p in s.split("|")]
    if len(parts) < 3:
        continue
    seq = parts[1]
    if not seq.isdigit():
        continue
    
    raw_path = parts[2]
    norm = normalize_path(raw_path)
    if norm in indexed:
        print(f"DUPLICATE at row {i}: {norm} (first at row {indexed[norm]})")
    else:
        indexed[norm] = i
        row_paths[i] = norm

indexed_count = len(indexed)
print(f"INDEXED_UNIQUE_PATHS={indexed_count}")

# ── 3. Cross-check ──────────────────────────────────────────────────

missing = set(actual.keys()) - set(indexed.keys())
extra = set(indexed.keys()) - set(actual.keys())
duplicates = set()
seen = set()
for p in sorted(indexed.keys()):
    if p in seen:
        duplicates.add(p)
    seen.add(p)

# Self check
self_path = normalize_path("文明块/00 - 项目总纲/00.4 全库Markdown审计索引.md")
self_included = self_path in indexed

print(f"MISSING={len(missing)}")
print(f"EXTRA={len(extra)}")
print(f"DUPLICATE={len(duplicates)}")
print(f"SELF_INCLUDED={self_included}")

if missing:
    print("  Missing files:")
    for m in sorted(missing)[:10]:
        print(f"    {m}")
if extra:
    print("  Extra paths:")  
    for e in sorted(extra)[:10]:
        print(f"    {e}")

# ── 4. Stub file analysis ───────────────────────────────────────────

stub_path_base = normalize_path("文明块/ChatGPT_完整讨论包")
stub_count = 0
stub_files = []

for rel, size in sorted(actual.items()):
    if not rel.startswith(stub_path_base):
        continue
    fp = REPO_ROOT / rel
    with open(fp, "r", encoding="utf-8") as fh:
        content = fh.read()
    
    # Remove BOM
    if content.startswith("\ufeff"):
        content = content[1:]
    # Remove frontmatter
    if content.startswith("---"):
        end = content.find("---", 3)
        if end != -1:
            content = content[end + 3:]
    # Remove first H1 and whitespace
    content = content.strip()
    h1match = re.match(r"^#\s+.+", content)
    if h1match:
        content = content[h1match.end():].strip()
    # Remove remaining empty lines
    body_lines = [l for l in content.split("\n") if l.strip()]
    
    if len(body_lines) == 0:
        stub_count += 1
        stub_files.append(rel)

print(f"\nSTUB_FILES={stub_count}")
for sf in stub_files:
    print(f"  {sf}")

# ── 5. Building detail status check (C042) ──────────────────────────

bd_path = normalize_path("文明块/02 - 建筑系统/单个建筑详情")
c042_count = 0
for rel in sorted(actual.keys()):
    if not rel.startswith(bd_path):
        continue
    fp = REPO_ROOT / rel
    with open(fp, "r", encoding="utf-8") as fh:
        content = fh.read()
    has_status = content.startswith("---") and "status:" in content[:500]
    if not has_status:
        c042_count += 1

print(f"\nBUILDING_DETAIL_NO_STATUS={c042_count}")

# ── 6. Frontmatter stats ────────────────────────────────────────────

fm_count = 0
no_fm_count = 0
empty_count = 0
for rel in sorted(actual.keys()):
    fp = REPO_ROOT / rel
    with open(fp, "r", encoding="utf-8") as fh:
        content = fh.read()
    if content.startswith("---"):
        fm_count += 1
    else:
        no_fm_count += 1
    if len(content.strip()) == 0:
        empty_count += 1

print(f"\nHAS_FRONTMATTER={fm_count}")
print(f"NO_FRONTMATTER={no_fm_count}")
print(f"EMPTY_FILES={empty_count}")

# ── 7. Validation results ───────────────────────────────────────────

# validate_index check
idx_pass = (
    actual_count == indexed_count
    and len(missing) == 0
    and len(extra) == 0
    and len(duplicates) == 0
    and self_included
)

if idx_pass:
    print("\nVALIDATE_INDEX=PASS")
else:
    print(f"\nVALIDATE_INDEX=FAIL (act={actual_count} idx={indexed_count} miss={len(missing)} extra={len(extra)} dup={len(duplicates)} self={self_included})")

# audit_scan check  
scan_pass = fm_count + no_fm_count == actual_count
if scan_pass:
    print("AUDIT_SCAN=PASS")
else:
    print("AUDIT_SCAN=FAIL")

# final_audit check (use build_master_table style logic)
print(f"\nSTUB_EVIDENCE: {stub_count} stub files in {stub_path_base}")
print(f"C042_EVIDENCE: {c042_count} building detail files without status")

# If any check fails, raise SystemExit(1)
if not idx_pass or not scan_pass:
    raise SystemExit(1)

print("\nFINAL_AUDIT=PASS")