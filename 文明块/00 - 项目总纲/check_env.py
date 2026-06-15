import subprocess, os, sys

base = r'd:\超级文档管理\文明块仓库\文明块'
os.chdir(base)

def ru(cmd):
    """Run command, return stdout, ignore encoding errors"""
    r = subprocess.run(cmd, capture_output=True, text=False)
    try:
        out = r.stdout.decode('utf-8', errors='replace')
        err = r.stderr.decode('utf-8', errors='replace')
        return out.strip(), err.strip(), r.returncode
    except:
        return r.stdout.decode('gbk', errors='replace'), r.stderr.decode('gbk', errors='replace'), r.returncode

# Git branch check
out, err, rc = ru(['git', 'branch', '--show-current'])
print(f'BRANCH: {out}')

# Git remote
out, err, rc = ru(['git', 'remote', '-v'])
print(f'REMOTE: {out}')

# Git fetch
out, err, rc = ru(['git', 'fetch', 'origin'])
if rc != 0:
    print(f'FETCH ERROR: {err}')
else:
    print('FETCH: OK')

# Local HEAD
out, err, rc = ru(['git', 'rev-parse', 'HEAD'])
print(f'LOCAL_HEAD: {out}')

# Remote HEAD
out, err, rc = ru(['git', 'rev-parse', 'origin/修改内容'])
print(f'REMOTE_HEAD: {out}')

# Count all .md files
md_files = []
for root, dirs, fs in os.walk(base):
    for f in sorted(fs):
        if f.endswith('.md'):
            rel = os.path.relpath(os.path.join(root, f), base)
            md_files.append(rel)

print(f'\nACTUAL_MD_FILES={len(md_files)}')
for mf in sorted(md_files):
    print(f'  {mf}')