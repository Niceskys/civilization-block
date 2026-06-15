import os, sys, re

base = r'd:\超级文档管理\文明块仓库\文明块'
os.chdir(base)

# 1. Count all actual .md files
actual_md = {}
for root, dirs, fs in os.walk(base):
    for f in sorted(fs):
        if f.endswith('.md'):
            rel = os.path.relpath(os.path.join(root, f), base)
            actual_md[rel] = os.path.getsize(os.path.join(root, f))

print(f'ACTUAL_MD_FILES={len(actual_md)}')

# 2. Read 00.4 index and extract paths
index_path = r'文明块\00 - 项目总纲\00.4 全库Markdown审计索引.md'
indexed_paths = {}
if os.path.exists(index_path):
    with open(index_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    in_table = False
    for line in lines:
        if line.strip().startswith('| 序号 | 完整相对路径'):
            in_table = True
            continue
        if in_table and line.strip().startswith('|') and '------' not in line:
            parts = [p.strip() for p in line.split('|')]
            if len(parts) >= 3:
                seq = parts[1]
                path = parts[2]
                if seq.isdigit():
                    indexed_paths[path] = seq

print(f'INDEXED_PATHS={len(indexed_paths)}')

# 3. Find missing and extra
missing = set(actual_md.keys()) - set(indexed_paths.keys())
extra = set(indexed_paths.keys()) - set(actual_md.keys())
duplicate = [p for p, cnt in [(p, list(indexed_paths.keys()).count(p)) for p in set(indexed_paths.keys())] if cnt > 1]

print(f'MISSING={len(missing)} extra={len(extra)} duplicate={len(duplicate)}')
if missing:
    for m in sorted(missing):
        print(f'  MISSING: {m}')
if extra:
    for e in sorted(extra):
        print(f'  EXTRA: {e}')

# 4. Check if 00.4 includes itself
self_in = '文明块\\00 - 项目总纲\\00.4 全库Markdown审计索引.md' in indexed_paths
print(f'00.4_INCLUDES_SELF={self_in}')

# 5. Check C041 and C042: are they mentioned anywhere?
for check in ['C041', 'C042']:
    found = False
    for root, dirs, fs in os.walk(base):
        for f in fs:
            if f.endswith('.md') and '00' in os.path.join(root, f):
                fp = os.path.join(root, f)
                with open(fp, 'r', encoding='utf-8', errors='replace') as fh:
                    content = fh.read()
                if check in content:
                    found = True
                    print(f'{check}_FOUND_IN={os.path.relpath(fp, base)}')
                    break
        if found:
            break
    if not found:
        print(f'{check}: NOT FOUND in any 00 file')

# 6. Check building detail files for status
print('\nBUILDING_DETAIL_STATUS_CHECK:')
bd_dir = r'文明块\02 - 建筑系统\单个建筑详情'
no_status = []
if os.path.exists(bd_dir):
    for f in sorted(os.listdir(bd_dir)):
        if f.endswith('.md'):
            fp = os.path.join(bd_dir, f)
            with open(fp, 'r', encoding='utf-8') as fh:
                content = fh.read()
            has_status = 'status:' in content[:500] if content.startswith('---') else False
            if not has_status:
                no_status.append(f)

print(f'  Files without status: {len(no_status)}')
for f in no_status:
    print(f'    {f}')

# 7. Check if 文明块\ChatGPT_完整讨论包 has stub files
print('\nSTUB_FILE_CHECK:')
stub_path = r'文明块\ChatGPT_完整讨论包'
stubs = []
if os.path.exists(stub_path):
    for f in sorted(os.listdir(stub_path)):
        if f.endswith('.md'):
            fp = os.path.join(stub_path, f)
            sz = os.path.getsize(fp)
            if sz < 50:
                stubs.append((f, sz))

print(f'  Stub files (<50B): {len(stubs)}')
for f, sz in stubs:
    print(f'    {f} ({sz}B)')

print('\n=== ALL CHECKS COMPLETE ===')