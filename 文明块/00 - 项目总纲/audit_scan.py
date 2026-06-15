import os, re

base = r'd:\超级文档管理\文明块仓库\文明块'
results = {}

for r,_,fs in os.walk(base):
    for f in sorted(fs):
        if not f.endswith('.md'):
            continue
        fp = os.path.join(r,f)
        rel = os.path.relpath(fp, base)
        
        try:
            with open(fp, 'r', encoding='utf-8') as fh:
                content = fh.read()
        except:
            try:
                with open(fp, 'r', encoding='gbk') as fh:
                    content = fh.read()
            except:
                content = 'READ_ERROR'
        
        lines = content.split('\n')
        size = len(content)
        nlines = len(lines)
        
        # Get H1
        h1s = [l.strip() for l in lines if l.strip().startswith('# ')]
        h1 = h1s[0] if h1s else '(no H1)'
        
        # Frontmatter analysis
        has_fm = content.startswith('---')
        fm_text = ''
        if has_fm:
            fm_end = content.find('---', 3)
            if fm_end != -1:
                fm_text = content[3:fm_end].strip()
        
        status = 'none'
        version = 'none'
        tags = []
        if fm_text:
            m = re.search(r'status:\s*(\S+)', fm_text)
            if m: status = m.group(1)
            m = re.search(r'version:\s*([\d.]+)', fm_text)
            if m: version = m.group(1)
            m = re.findall(r'-\s*(.+?)\n', 
                fm_text[fm_text.find('tags:'):] if 'tags:' in fm_text else '')
            tags = m
        
        # Count lines with numbers
        num_lines = sum(1 for l in lines if re.search(r'\d+\.?\d*\s*[%倍个块日/分时秒局]', l))
        
        # Empty check
        is_empty = nlines <= 1 and size < 50
        is_symlink = nlines == 1 and '<symlink' in content.lower()
        
        results[rel] = {
            'size': size, 'lines': nlines, 'h1': h1,
            'has_fm': has_fm, 'status': status, 'version': version,
            'num_lines': num_lines, 'empty': is_empty, 'symlink': is_symlink
        }

# Print CSV-style output
print("SEQ|PATH|SIZE|LINES|H1|HAS_FM|STATUS|VERSION|NUM_LINES|EMPTY")
seq = 0
for rel in sorted(results.keys()):
    seq += 1
    r = results[rel]
    h1_clean = r['h1'].replace('|', '/')
    print(f"{seq}|{rel}|{r['size']}|{r['lines']}|{h1_clean}|{r['has_fm']}|{r['status']}|{r['version']}|{r['num_lines']}|{r['empty']}")

print(f"\nTOTAL_MD_FILES={len(results)}")
empty_count = sum(1 for r in results.values() if r['empty'])
print(f"EMPTY_FILES={empty_count}")
fm_count = sum(1 for r in results.values() if r['has_fm'])
print(f"HAS_FRONTMATTER={fm_count}")