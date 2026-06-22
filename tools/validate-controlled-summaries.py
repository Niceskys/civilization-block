#!/usr/bin/env python3
"""只读校验器：验证建筑详情受控摘要与6.1权威表的一致性。

校验项：
1. 建造成本、正常施工时长、工人上限、基础产出、耐久度、承重值
2. 星辉解锁价格（初始解锁 vs 商店N星辉）

退出码：全部一致 0，任何不一致或缺失 1。
"""

import os
import re
import sys


# ── path helpers ────────────────────────────────────────────────────────────

def find_project_root():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    d = script_dir
    while True:
        parent = os.path.dirname(d)
        if parent == d:
            break
        if os.path.isdir(os.path.join(d, '文明块')):
            return d
        d = parent
    return script_dir


ROOT = find_project_root()
WMB = os.path.join(ROOT, '文明块')
TABLE61 = os.path.join(WMB, '06 - 全局数值总表', '6.1 建筑数值表.md')
TABLE92 = os.path.join(WMB, '09-商店系统', '9.2 商品清单.md')
DETAIL_DIR = os.path.join(WMB, '02 - 建筑系统', '单个建筑详情')


# ── normalization ───────────────────────────────────────────────────────────

def norm(s):
    """基础规范化：全角→半角，统一空格，去除首尾空白。"""
    if s is None:
        return None
    s = s.strip()
    s = s.replace('‘', "'").replace('’', "'")
    s = s.replace('“', '"').replace('”', '"')
    s = s.replace('（', '(').replace('）', ')')
    s = s.replace('：', ':').replace('，', ',')
    s = s.replace('—', '-').replace('－', '-')
    s = re.sub(r'\s*\+\s*', '+', s)
    s = re.sub(r'\s*,\s*', ',', s)
    s = re.sub(r'\s+', ' ', s)
    return s


def norm_time(s):
    """规范化施工时长：去除括号内补充说明，提取 X游戏日 数值。"""
    if s is None:
        return None
    s = norm(s)
    s = re.sub(r'\([^)]*\)', '', s).strip()
    return s


def norm_workers(s):
    """规范化工人数量：无工人→-，X人→X，-保持-。"""
    if s is None:
        return None
    s = norm(s)
    if re.search(r'无工人', s):
        return '-'
    m = re.search(r'(\d+)\s*人?', s)
    if m:
        return m.group(1)
    if s in ('-', '—', ''):
        return '-'
    return s


def norm_load(s):
    """规范化承重值：无(…)→-，纯数字保留。"""
    if s is None:
        return None
    s = norm(s)
    if s.startswith('无') or s in ('-', '—', ''):
        return '-'
    m = re.search(r'(\d+)', s)
    if m:
        return m.group(1)
    return s


def norm_output(s):
    """规范化产出文本：半角括号，统一空格，/日→/游戏日。"""
    if s is None:
        return None
    s = norm(s)
    s = re.sub(r'一(\s*)次', r'1\1次', s)   # 一次性→1次性
    s = re.sub(r'/(?=日)', '/游戏', s)       # /日→/游戏日
    # strip parenthetical notes for comparison
    return s


def strip_parens(s):
    """去除括号注释部分，用于宽松比较。"""
    if s is None:
        return None
    s = re.sub(r'\([^)]*\)', '', s)
    s = re.sub(r'（[^）]*）', '', s)
    return s.strip()


# ── parsers ─────────────────────────────────────────────────────────────────

def parse_61_table(path):
    """解析6.1 数值总览表，返回 {建筑名: {field: value}, ...}。

    从"数值总览"标题后的第一个表格解析。
    """
    with open(path, 'r', encoding='utf-8') as f:
        text = f.read()

    # 找到 数值总览 之后的第一个表格
    idx = text.find('数值总览')
    if idx == -1:
        raise SystemExit('6.1: 找不到"数值总览"标题')
    tail = text[idx:]

    # 找表格行
    lines = tail.split('\n')
    in_table = False
    rows = []
    for line in lines:
        line = line.strip()
        if line.startswith('|') and line.endswith('|'):
            if not in_table:
                in_table = True
                continue  # skip header
            # skip separator line
            if re.match(r'^\|[\s\-:|]+\|$', line):
                continue
            cells = [c.strip() for c in line.split('|')[1:-1]]
            rows.append(cells)
        elif in_table:
            break

    result = {}
    for cells in rows:
        if len(cells) < 9:
            continue
        name = cells[0]
        result[name] = {
            'cost': cells[1],
            'time': cells[2],
            'workers': cells[5],
            'output': cells[6],
            'durability': cells[7],
            'load': cells[8],
        }
    return result


def parse_92_prices(path):
    """解析9.2 建筑解锁价格，返回 {商品名: 价格(星辉), ...}。"""
    with open(path, 'r', encoding='utf-8') as f:
        text = f.read()

    # 找 建筑解锁类商品 之后的表格
    idx = text.find('建筑解锁类商品')
    if idx == -1:
        raise SystemExit('9.2: 找不到"建筑解锁类商品"标题')
    tail = text[idx:]

    lines = tail.split('\n')
    in_table = False
    result = {}
    for line in lines:
        line = line.strip()
        if line.startswith('|') and line.endswith('|'):
            if not in_table:
                in_table = True
                continue
            if re.match(r'^\|[\s\-:|]+\|$', line):
                continue
            cells = [c.strip() for c in line.split('|')[1:-1]]
            if len(cells) >= 2:
                name = cells[0]
                try:
                    price = int(cells[1])
                except ValueError:
                    continue
                result[name] = price
        elif in_table:
            break
    return result


def parse_frontmatter(text):
    """解析YAML frontmatter，返回dict。"""
    if not text.startswith('---'):
        return {}
    end = text.find('---', 3)
    if end == -1:
        return {}
    fm = text[3:end].strip()
    result = {}
    for line in fm.split('\n'):
        line = line.strip()
        if ':' in line:
            key, _, val = line.partition(':')
            result[key.strip()] = val.strip()
    return result


def parse_detail_summary(text):
    """从建筑详情文本解析 快速摘要 中的字段。

    返回 {field: value} dict，其中 field 为：
    cost, time, workers, output, durability, load
    """
    idx = text.find('快速摘要')
    if idx == -1:
        return {}

    tail = text[idx:]
    # 取到下一个 ## 或 ### 标题
    end = re.search(r'\n#{2,3}\s', tail)
    if end:
        tail = tail[:end.start()]

    result = {}
    raw_fields = {}
    bullets = tail.split('\n')
    for line in bullets:
        # Find every inline **key**: value pair, stopping at the next pipe.
        matches = re.findall(r'\*\*([^*]+)\*\*[：:]\s*([^|\n]+)', line)
        for k, v in matches:
            raw_fields[k.strip()] = v.strip()

    # 映射字段名到标准键
    key_map = {
        '建造成本': 'cost',
        '建造时长': 'time',
        '工人上限': 'workers_raw',
        '基础产出': 'output_raw',
        '产出': 'output_raw',
        '效果': 'output_raw',
        '居住容量': 'output_raw',
        '处理模式': 'output_raw',
        '耐久度': 'durability',
        '承重值': 'load_raw',
    }

    for cn_key, en_key in key_map.items():
        if cn_key in raw_fields:
            result[en_key] = raw_fields[cn_key]

    # 处理 无工人 情况（可能在单独一行或内联在运行消耗行）
    # 检查是否有 "无工人" 字样
    for line in bullets:
        if re.search(r'\*\*无工人[^*]*\*\*', line):
            result['workers_raw'] = '无工人'
            break
        # 也处理非粗体的 无工人
        if '无工人' in line and 'workers_raw' not in result:
            result['workers_raw'] = '无工人'

    return result


def parse_detail_time(raw_time):
    """从detail的建造时长文本中提取规范值。"""
    if raw_time is None:
        return None
    return norm_time(raw_time)


def parse_detail_workers(raw_workers):
    """从detail的工人文本中提取规范值。"""
    if raw_workers is None:
        return None
    return norm_workers(raw_workers)


def parse_detail_load(raw_load):
    """从detail的承重值文本中提取规范值。"""
    if raw_load is None:
        return None
    return norm_load(raw_load)


def parse_detail_output(raw_output):
    """从detail的产出文本中提取规范值。"""
    if raw_output is None:
        return None
    return norm_output(raw_output)


# ── comparison helpers ──────────────────────────────────────────────────────

def cmp_text(a, b, label):
    """比较两个规范化文本。

    返回 (status, detail) — status: PASS/FAIL/SKIP, detail: 说明。
    """
    if b is None:
        return 'SKIP', f'{label}: 详情未展示'
    if a is None:
        return 'SKIP', f'{label}: 6.1未提供'

    na = norm(a) if a else ''
    nb = norm(b) if b else ''

    if na == nb:
        return 'PASS', f'{label}: 一致 ({na})'
    # 尝试宽松：去除括号后比较
    sa = strip_parens(na)
    sb = strip_parens(nb)
    if sa == sb:
        return 'PASS', f'{label}: 去除括号后一致 ({sa})'
    return 'FAIL', f'{label}: 不一致 → 6.1:「{a}」vs 详情:「{b}」'


def output_equivalent(building_name, authority, summary):
    """Compare output fields with explicit structural rules for prose variants."""
    a = norm_output(authority)
    b = norm_output(summary)
    if a == b or strip_parens(a) == strip_parens(b):
        return True

    if building_name == '房屋':
        ma = re.search(r'(\d+)\s*床位', a)
        mb = re.search(r'(\d+)\s*床位', b)
        return bool(ma and mb and ma.group(1) == mb.group(1))

    if building_name == '垃圾处理场':
        ma = re.search(r'(\d+)\s*模式', a)
        mb = re.search(r'(\d+)\s*种', b)
        return bool(ma and mb and ma.group(1) == mb.group(1))

    if building_name == '哨塔':
        ma = re.search(r'(\d+)\s*格', a)
        mb = re.search(r'(\d+)\s*格', b)
        return bool(ma and mb and ma.group(1) == mb.group(1) and '怪物' in a and '怪物' in b)

    # For other prose summaries, matching numeric tokens plus containment after
    # parenthetical notes is sufficient, but a changed number always fails.
    nums_a = re.findall(r'\d+(?:\.\d+)?', a)
    nums_b = re.findall(r'\d+(?:\.\d+)?', b)
    sa = strip_parens(a)
    sb = strip_parens(b)
    return nums_a == nums_b and bool(sa and sb and (sa in sb or sb in sa))


def cmp_numeric(a, b, label, parser_fn=None):
    """比较两个数值字段，带规范化。"""
    if b is None:
        return 'SKIP', f'{label}: 详情未展示'
    if a is None:
        return 'SKIP', f'{label}: 6.1未提供'

    if parser_fn:
        na = parser_fn(a)
        nb = parser_fn(b)
    else:
        na = norm(a)
        nb = norm(b)

    if na is None or nb is None:
        return 'SKIP', f'{label}: 无法解析'

    if na == nb:
        return 'PASS', f'{label}: 一致 ({na})'
    return 'FAIL', f'{label}: 不一致 → 6.1:「{na}」vs 详情:「{nb}」'


# ── name mapping ────────────────────────────────────────────────────────────

# 6.1正式名称 → 详情文件名(不含.md)
NAME_61_TO_DETAIL = {
    '房屋': '房屋',
    '农田': '农田',
    '水井': '水井',
    '树场': '树场',
    '采掘场': '矿场',       # 6.1正式名→详情文件名
    '熔炉房': '熔炉房',
    '太阳灯': '太阳灯',
    '农舍': '农舍',
    '垃圾处理场': '垃圾处理场',
    '仓库': '仓库',
    '哨塔': '哨塔',
    '陷阱': '陷阱',
    '研究站': '研究站',
    '管道': '管道',
    '疗愈所': '疗愈所',
    '城墙': '城墙',
    '矿脉挖掘点': '矿脉挖掘点',
    '工坊': '工坊',
}

# 详情文件名 → 9.2商品名
DETAIL_TO_92_NAME = {
    '矿场': '矿场',
    '熔炉房': '熔炉房',
    '太阳灯': '太阳灯',
    '农舍': '农舍',
    '垃圾处理场': '垃圾处理场',
    '仓库': '仓库',
    '哨塔': '哨塔',
    '陷阱': '陷阱',
    '研究站': '研究站',
    '管道': '管道',
    '疗愈所': '疗愈所',
    '城墙': '城墙',
    '工坊': '工坊',
    '矿脉挖掘点': '矿脉挖掘点',
}

# 初始解锁建筑（6.1 §初始拥有）
INITIAL_UNLOCK = {'房屋', '农田', '水井', '树场'}


# ── main validation ─────────────────────────────────────────────────────────

def main():
    issues = []
    all_pass = True

    # 1. 解析6.1
    if not os.path.exists(TABLE61):
        raise SystemExit(f'找不到6.1: {TABLE61}')
    table61 = parse_61_table(TABLE61)
    if len(table61) != 18:
        print(f'错误: 6.1解析到{len(table61)}座建筑，预期18座')
        issues.append(f'6.1建筑数量异常: {len(table61)}')
        all_pass = False

    # 2. 解析9.2
    if not os.path.exists(TABLE92):
        raise SystemExit(f'找不到9.2: {TABLE92}')
    prices92 = parse_92_prices(TABLE92)

    # 3. 逐个校验
    results = []

    for name61, detail_file in NAME_61_TO_DETAIL.items():
        if name61 not in table61:
            print(f'错误: 6.1中找不到「{name61}」')
            issues.append(f'6.1缺少建筑: {name61}')
            all_pass = False
            continue

        ref = table61[name61]
        detail_path = os.path.join(DETAIL_DIR, f'{detail_file}.md')

        if not os.path.exists(detail_path):
            print(f'错误: 详情文件不存在: {detail_path}')
            issues.append(f'缺少详情: {detail_file}.md')
            all_pass = False
            continue

        with open(detail_path, 'r', encoding='utf-8') as f:
            detail_text = f.read()

        fm = parse_frontmatter(detail_text)
        summary = parse_detail_summary(detail_text)

        building_label = f'{name61}({detail_file}.md)'
        field_results = []

        # --- 建造成本 ---
        detail_cost = summary.get('cost')
        status, msg = cmp_text(ref['cost'], detail_cost, '建造成本')
        field_results.append((status, msg))
        if status == 'FAIL':
            all_pass = False

        # --- 正常施工时长 ---
        detail_time_raw = summary.get('time')
        if detail_time_raw is None:
            field_results.append(('SKIP', '施工时长: 详情未展示'))
        else:
            detail_time = norm_time(detail_time_raw)
            ref_time = norm_time(ref['time'])
            if detail_time == ref_time:
                field_results.append(('PASS', f'施工时长: 一致 ({detail_time})'))
            else:
                field_results.append(('FAIL', f'施工时长: 不一致 → 6.1:「{ref_time}」vs 详情:「{detail_time}」'))
                all_pass = False

        # --- 工人上限 ---
        detail_workers_raw = summary.get('workers_raw')
        detail_workers = norm_workers(detail_workers_raw) if detail_workers_raw else None
        ref_workers = norm_workers(ref['workers'])
        status, msg = cmp_numeric(ref['workers'], detail_workers_raw, '工人上限',
                                  parser_fn=norm_workers)
        if status == 'SKIP':
            msg = '工人上限: 详情未展示'
        field_results.append((status, msg if '工人上限' in msg else f'工人上限: {msg}'))
        if status == 'FAIL':
            all_pass = False

        # --- 基础产出 ---
        detail_output = summary.get('output_raw')
        if detail_output is None:
            field_results.append(('SKIP', '基础产出: 详情未展示'))
        else:
            no = norm_output(detail_output)
            ro = norm_output(ref['output'])
            if output_equivalent(name61, ref['output'], detail_output):
                field_results.append(('PASS', '基础产出: 结构化比较一致'))
            else:
                field_results.append(('FAIL', f'基础产出: 不一致 → 6.1:「{ref["output"]}」vs 详情:「{detail_output}」'))
                all_pass = False

        # --- 耐久度 ---
        detail_dur = summary.get('durability')
        status, msg = cmp_text(ref['durability'], detail_dur, '耐久度')
        field_results.append((status, msg))
        if status == 'FAIL':
            all_pass = False

        # --- 承重值 ---
        detail_load_raw = summary.get('load_raw')
        detail_load = norm_load(detail_load_raw) if detail_load_raw else None
        ref_load = norm_load(ref['load'])
        if detail_load is None:
            field_results.append(('SKIP', '承重值: 详情未展示'))
        elif detail_load == ref_load:
            field_results.append(('PASS', f'承重值: 一致 ({detail_load})'))
        else:
            field_results.append(('FAIL', f'承重值: 不一致 → 6.1:「{ref_load}」vs 详情:「{detail_load}」({detail_load_raw})'))
            all_pass = False

        # --- 星辉解锁价 ---
        fm_unlock = fm.get('unlock', '')
        if name61 in INITIAL_UNLOCK:
            if '初始解锁' in fm_unlock:
                field_results.append(('PASS', f'解锁: 初始解锁'))
            else:
                field_results.append(('FAIL', f'解锁: 应为初始解锁，实际: {fm_unlock}'))
                all_pass = False
        else:
            # 商店解锁建筑
            m = re.search(r'商店(\d+)星辉', fm_unlock)
            if m:
                fm_price = int(m.group(1))
                name92 = DETAIL_TO_92_NAME.get(detail_file, detail_file)
                price92 = prices92.get(name92)
                if price92 is None:
                    field_results.append(('FAIL', f'解锁: 9.2中找不到「{name92}」的价格'))
                    all_pass = False
                elif fm_price == price92:
                    field_results.append(('PASS', f'解锁: 商店{fm_price}星辉 (与9.2一致)'))
                else:
                    field_results.append(('FAIL', f'解锁: 详情{fm_price}星辉 ≠ 9.2({price92}星辉)'))
                    all_pass = False
            else:
                field_results.append(('FAIL', f'解锁: 无法解析解锁价格: {fm_unlock}'))
                all_pass = False

        results.append((building_label, field_results))

    # 4. 输出
    print('=' * 60)
    print('建筑详情受控摘要一致性校验')
    print('=' * 60)

    pass_count = 0
    fail_count = 0
    skip_count = 0

    for label, fields in results:
        print(f'\n### {label}')
        for status, msg in fields:
            flag = {'PASS': '[PASS]', 'FAIL': '[FAIL]', 'SKIP': '[SKIP]'}[status]
            print(f'  {flag} {msg}')
            if status == 'PASS':
                pass_count += 1
            elif status == 'FAIL':
                fail_count += 1
            else:
                skip_count += 1

    print(f'\n{"=" * 60}')
    print(f'总计: {pass_count} PASS, {fail_count} FAIL, {skip_count} SKIP')
    print(f'建筑数: {len(results)}/18')

    if issues:
        print(f'\n全局问题:')
        for i in issues:
            print(f'  - {i}')

    if not all_pass or fail_count > 0 or len(results) < 18:
        print(f'\n结果: 不一致 ({fail_count} FAIL, {18 - len(results)} 缺失)')
        sys.exit(1)
    else:
        print(f'\n结果: 全部一致')
        sys.exit(0)


if __name__ == '__main__':
    main()
