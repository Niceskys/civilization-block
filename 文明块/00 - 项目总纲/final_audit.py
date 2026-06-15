import subprocess, os, re, sys

base = r'd:\超级文档管理\文明块仓库\文明块'
os.chdir(base)

def run(cmd_list):
    r = subprocess.run(cmd_list, capture_output=True, text=False)
    out = r.stdout.decode('utf-8', errors='replace')
    err = r.stderr.decode('utf-8', errors='replace')
    return out.strip(), err.strip(), r.returncode

# === Git status ===
print('=== GIT STATUS ===')
for cmd in [
    ['git', 'status', '-sb'],
    ['git', 'branch', '--show-current'],
    ['git', 'rev-parse', 'HEAD'],
    ['git', 'log', '--oneline', '-3'],
]:
    out, err, rc = run(cmd)
    print(f'$ {" ".join(cmd)}')
    print(out)
    if err and 'fatal' in err.lower():
        print('STDERR:', err)
    print()

# Try ls-remote
out, err, rc = run(['git', 'ls-remote', 'origin', 'refs/heads/修改内容'])
print('$ git ls-remote origin refs/heads/修改内容')
print(out if out else '(no output, likely network error)')
if err: print('STDERR:', err)
print()

# === Count actual .md files ===
actual_md = {}
for root, dirs, fs in os.walk(base):
    for f in sorted(fs):
        if f.endswith('.md'):
            rel = os.path.relpath(os.path.join(root, f), base)
            rel = rel.replace('\\', '/')
            actual_md[rel] = os.path.getsize(os.path.join(root, f))

print(f'=== ACTUAL MD FILES: {len(actual_md)} ===')
for mf in sorted(actual_md):
    print(f'  {mf}')
print()

# === Parse 00.4 index ===
idx_path = os.path.join(base, '文明块/00 - 项目总纲/00.4 全库Markdown审计索引.md')
if os.path.exists(idx_path):
    with open(idx_path, 'r', encoding='utf-8') as f:
        idx_lines = f.readlines()
    
    indexed_paths = {}
    seq_row_map = {}
    in_table = False
    table_start = 0
    
    for i, line in enumerate(idx_lines):
        s = line.strip()
        if s.startswith('| 序号 | 完整相对路径'):
            in_table = True
            table_start = i
            continue
        if in_table and s.startswith('|') and '------' not in s:
            parts = [p.strip() for p in s.split('|')]
            if len(parts) >= 3:
                seq = parts[1]
                path = parts[2]
                if seq.isdigit():
                    # Normalize path
                    path_normalized = path.replace('\\', '/')
                    if path_normalized in indexed_paths:
                        print(f'DUPLICATE at row {i+1}: {path_normalized} (first seen at row {seq_row_map[path_normalized]})')
                    indexed_paths[path_normalized] = seq
                    seq_row_map[path_normalized] = i + 1
    
    print(f'=== INDEXED PATHS: {len(indexed_paths)} ===')
    
    # Check self
    self_path = '文明块/00 - 项目总纲/00.4 全库Markdown审计索引.md'
    if self_path in indexed_paths:
        print(f'SELF INCLUDED: YES (row {seq_row_map[self_path]})')
    else:
        print(f'SELF INCLUDED: NO')
    
    # Missing, extra, duplicate
    missing = set(actual_md.keys()) - set(indexed_paths.keys())
    extra = set(indexed_paths.keys()) - set(actual_md.keys())
    
    print(f'MISSING: {len(missing)}')
    for m in sorted(missing):
        print(f'  {m}')
    print(f'EXTRA: {len(extra)}')
    for e in sorted(extra):
        print(f'  {e}')
    
    # Check duplicates
    path_counts = {}
    for p in indexed_paths:
        path_counts[p] = path_counts.get(p, 0) + 1
    dups = {p: c for p, c in path_counts.items() if c > 1}
    print(f'DUPLICATE: {len(dups)}')
    for d, c in sorted(dups.items()):
        print(f'  {d} appears {c}x')
    
    # Validation result
    if len(missing) == 0 and len(extra) == 0 and len(dups) == 0:
        print('\n*** INDEX VALIDATION: PASS ***')
        print(f'Actual={len(actual_md)}, Indexed={len(indexed_paths)}, All match')
    else:
        print(f'\n*** INDEX VALIDATION: FAIL ***')
        print(f'Actual={len(actual_md)}, Indexed={len(indexed_paths)}')
        if missing: print(f'Missing count={len(missing)}')
        if extra: print(f'Extra count={len(extra)}')
else:
    print(f'00.4 not found at {idx_path}')

# === Check C041/C042 evidence ===
print('\n=== C041/C042 EVIDENCE CHECK ===')
# Stub files
stub_path = os.path.join(base, '文明块/ChatGPT_完整讨论包')
stubs = []
if os.path.exists(stub_path):
    for f in sorted(os.listdir(stub_path)):
        if f.endswith('.md'):
            fp = os.path.join(stub_path, f)
            sz = os.path.getsize(fp)
            if sz < 50:
                stubs.append((f, sz))
print(f'Stub files (<50B) in 文明块/ChatGPT_完整讨论包: {len(stubs)}')
for f, sz in stubs:
    print(f'  {f} ({sz}B)')

# Building detail status
bd_path = os.path.join(base, '文明块/02 - 建筑系统/单个建筑详情')
no_status = []
if os.path.exists(bd_path):
    for f in sorted(os.listdir(bd_path)):
        if f.endswith('.md'):
            fp = os.path.join(bd_path, f)
            with open(fp, 'r', encoding='utf-8') as fh:
                content = fh.read()
            has_status = content.startswith('---') and 'status:' in content[:500]
            if not has_status:
                no_status.append(f)
print(f'\nBuilding detail files without status: {len(no_status)}')
for f in no_status:
    print(f'  {f}')

# === Build master conflict table from 00.2 ===
print('\n=== CONFLICT MASTER TABLE ===')
c02_path = os.path.join(base, '文明块/00 - 项目总纲/00.2 全库规则冲突清单.md')
all_c_records = {}

if os.path.exists(c02_path):
    with open(c02_path, 'r', encoding='utf-8') as f:
        c02_lines = f.readlines()
    
    # Parse all conflict tables
    in_table = False
    for line in c02_lines:
        if '| C' in line and '|' in line:
            parts = [p.strip() for p in line.split('|')]
            for p in parts:
                if p.startswith('C') and len(p) >= 4 and p[1:].isdigit():
                    cid = p
                    if cid not in all_c_records:
                        all_c_records[cid] = {'table': 'unknown'}
    
    print(f'Total unique C# references found in 00.2: {len(all_c_records)}')
    for cid in sorted(all_c_records.keys()):
        print(f'  {cid}')

print('\n=== DONE ===')