# This script generates the master conflict table for 00.2
# C001-C042 authoritative classification

master = [
    # (cid, title, status, category, priority, is_active, needs_ruling, route_task, evidence)
    
    # === Non-Active: Verified Consistent ===
    ("C001", "初始四块地皮勘探状态", "已验证一致", "前期体验", "无", False, False, "", "1.1已包含，B级仅确认"),
    ("C006", "危机保护期时长", "已验证一致", "前期体验", "无", False, False, "", "1.6与前30分钟V2一致(均为前30日)"),
    ("C039", "正式建造时长定义位置", "已验证一致", "文档治理", "无", False, False, "", "2.1引用6.1，引用链正常"),
    ("C040", "昼夜命名约定", "已验证一致", "文档治理", "无", False, False, "", "1.3与前30分钟V2时间格式一致"),
    
    # === Non-Active: Original Judgment Error ===
    # (none currently)
    
    # === Non-Active: Completed Back-Write ===
    # (none currently)
    
    # === Non-Active: Merged ===
    # (none currently)
    
    # === Active: P0 - Implementation Blocking ===
    ("C002", "新存档是否默认暂停", "已裁决待回写", "实现阻断", "P0", True, False, "P0-RW-001", "1.3未定义，B级V2已明确"),
    ("C003", "同种建筑效率惩罚", "已裁决待回写", "实现阻断", "P0", True, False, "P0-RW-002", "2.1维持40%，裁决要求移除"),
    ("C005", "首次建造加速是否已写入", "已裁决待回写", "实现阻断", "P0", True, False, "P0-RW-003", "2.1/6.1未含加速规则"),
    ("C007", "第一夜怪物规则", "已裁决待回写", "实现阻断", "P0", True, False, "P0-RW-004", "7.2允许第一夜有怪，B级要求无怪"),
    ("C008", "初始NPC入住规则", "已裁决待回写", "实现阻断", "P0", True, False, "P0-RW-005", "3.1未定义自动入住"),
    ("C010", "前3座建筑免费重新放置", "已裁决待回写", "实现阻断", "P0", True, False, "P0-RW-006", "2.1仅75%返还，B级要求免费重放"),
    
    # === Active: P1 - Early Experience ===
    ("C009", "首次工作分配显示详细程度", "已裁决待回写", "前期体验", "P1", True, False, "P1-RW-001", "3.3显示7维，B级要求前2项"),
    ("C011", "6.4数值模拟是否使用旧规则", "待核查", "前期体验", "P1", True, False, "P1-RW-007", "6.4可能未同步V2参数"),
    ("C014", "怪物/危机/新手保护概念混淆", "等待设计者裁决", "前期体验", "P1", True, True, "P1-RW-002", "术语混用需区分"),
    ("C016", "效率惩罚与专业化加成矛盾", "已裁决待回写", "前期体验", "P1", True, False, "P1-RW-008", "2.1惩罚vs2.2加成"),
    ("C019", "已定稿文件被后续方案要求修改", "已裁决待回写", "前期体验", "P1", True, False, "P3-RW-003", "16项P0修改覆盖已定稿文件"),
    ("C023", "主线任务前30分钟弹窗规则", "已裁决待回写", "前期体验", "P1", True, False, "P1-RW-002", "10.1弹窗vsB级去重"),
    ("C024", "连续生产进度规则", "已裁决待回写", "前期体验", "P1", True, False, "P1-RW-003", "4.2未定义结算方式"),
    ("C027", "燃料单一来源限制", "已裁决待回写", "前期体验", "P1", True, False, "P1-RW-004", "4.2仅有垃圾场产出"),
    ("C030", "建造面板Tab数量", "已裁决待回写", "前期体验", "P1", True, False, "P1-RW-005", "5.2定义8个Tab，缺推荐标签"),
    ("C031", "教程提示系统状态机缺失", "已裁决待回写", "前期体验", "P1", True, False, "P1-RW-006", "5.8无状态机定义"),
    ("C032", "连续无损失时惩罚性难度增加", "已裁决待回写", "前期体验", "P1", True, False, "P1-RW-013", "7.2惩罚强者，裁决要求反转"),
    ("C036", "前30分钟方案加速规则是否已写入", "已裁决待回写", "前期体验", "P1", True, False, "P0-RW-003", "与C005耦合，未回写"),
    
    # === Active: P2 - Mid-Late Balance ===
    ("C004", "堆叠惩罚与垂直城市核心定位冲突", "等待设计者裁决", "中后期平衡", "P2", True, True, "P2-RW-001", "2.1鼓励vs1.2惩罚"),
    ("C012", "建筑详情/6.1/9.2价格是否一致", "待核查", "中后期平衡", "P2", True, False, "P2-RW-002", "三源数据需核对"),
    ("C013", "NPC每日消耗是否一致", "待核查", "中后期平衡", "P2", True, False, "P2-RW-003", "3.1/4.1/6.2三方"),
    ("C017", "季节活动错过等一年与反FOMO冲突", "等待设计者裁决", "中后期平衡", "P2", True, True, "P2-RW-005", "1.6 vs 裁决反FOMO"),
    ("C020", "同一机制多文件维护完整数值", "待核查", "中后期平衡", "P2", True, False, "P2-RW-002", "6.1+各建筑详情"),
    ("C025", "堆叠规则分散矛盾", "待核查", "中后期平衡", "P2", True, False, "P2-RW-001", "1.2和2.2分散"),
    ("C026", "NPC性格对效率的影响", "等待设计者裁决", "中后期平衡", "P2", True, True, "P2-RW-007", "3.1性格vs3.7好感度"),
    ("C033", "Lv3自动运转规则位置", "待核查", "中后期平衡", "P2", True, False, "P2-RW-006", "2.1和1.3同时维护"),
    
    # === Active: P3 - Document Governance ===
    ("C018", "引用文件名或章节不存在", "待核查", "文档治理", "P3", True, False, "P3-RW-001", "全库需核查引用有效性"),
    ("C021", "拆除规则不一致", "已裁决待回写", "文档治理", "P3", True, False, "P0-RW-006", "2.1与C010耦合"),
    ("C022", "暂停定义和停止范围", "已裁决待回写", "文档治理", "P3", True, False, "P0-RW-001", "与C002耦合"),
    ("C028", "全体NPC死亡后失败条件", "待核查", "文档治理", "P3", True, False, "P3-RW-001", "1.4引用7.1"),
    ("C029", "NPC工作分配规则存放位置", "已裁决待回写", "文档治理", "P3", True, False, "P3-RW-006", "B级修正归属"),
    ("C034", "商品价格在商店和商人一致性", "待核查", "文档治理", "P3", True, False, "P2-RW-008", "9.2 vs 1.6"),
    ("C035", "地皮商店价格定义位置", "待核查", "文档治理", "P3", True, False, "P2-RW-006", "1.1 vs 9.2"),
    ("C038", "旧方案未归档", "未处理", "文档治理", "P3", True, False, "P3-RW-004", "顶层目录遗留"),
    ("C041", "存根文件混淆", "未处理", "文档治理", "P3", True, False, "P3-RW-008", "17个存根21~42B"),
    ("C042", "建筑详情文件缺失状态标记", "未处理", "文档治理", "P3", True, False, "P3-RW-009", "18个文件无status"),
    
    # === Active: DG - Design Gate (waiting for designer ruling) ===
    ("C015", "纯沙盒无目标与任务/里程碑冲突", "等待设计者裁决", "设计裁决门", "DG", True, True, "裁决-C015", "核心设计哲学冲突"),
    ("C037", "传承模式与纯沙盒冲突", "等待设计者裁决", "设计裁决门", "DG", True, True, "裁决-C037", "存档结束概念与沙盒定位"),
]

# Validate
# m[5] is is_active (True/False), m[4] is priority
active = [m for m in master if m[5] == True]
non_active = [m for m in master if m[5] == False]

p0 = [m for m in active if m[4] == "P0"]
p1 = [m for m in active if m[4] == "P1"]
p2 = [m for m in active if m[4] == "P2"]
p3 = [m for m in active if m[4] == "P3"]
dg = [m for m in active if m[4] == "DG"]

# Verify no overlap
all_active_ids = set(m[0] for m in active)
all_non_active_ids = set(m[0] for m in non_active)
assert len(all_active_ids & all_non_active_ids) == 0, "Overlap between active and non-active!"

# Verify exactly 42 unique IDs
all_ids = set(m[0] for m in master)
expected = set(f"C{i:03d}" for i in range(1, 43))
missing_ids = expected - all_ids
extra_ids = all_ids - expected

print(f"=== MASTER TABLE VALIDATION ===")
print(f"Total records: {len(master)}")
print(f"Active: {len(active)}")
print(f"Non-active: {len(non_active)}")
print(f"P0: {len(p0)} -> {[m[0] for m in p0]}")
print(f"P1: {len(p1)} -> {[m[0] for m in p1]}")
print(f"P2: {len(p2)} -> {[m[0] for m in p2]}")
print(f"P3: {len(p3)} -> {[m[0] for m in p3]}")
print(f"DG: {len(dg)} -> {[m[0] for m in dg]}")

# Verify C001-C042 completeness
print(f"\nMissing IDs: {missing_ids}")
print(f"Extra IDs: {extra_ids}")

# Equation checks
eq1 = len(p0) + len(p1) + len(p2) + len(p3) + len(dg)
eq2 = eq1 == len(active)
eq3 = len(active) + len(non_active) == 42
eq4 = len(active) == len(p0) + len(p1) + len(p2) + len(p3) + len(dg)

print(f"\nP0+P1+P2+P3+DG = {eq1} (Active = {len(active)}) -> {'PASS' if eq2 else 'FAIL'}")
print(f"Active + Non-Active = {len(active) + len(non_active)} (42 = 42) -> {'PASS' if eq3 else 'FAIL'}")
print(f"P0+P1+P2+P3+DG == Active count = {eq1}=={len(active)} -> {'PASS' if eq4 else 'FAIL'}")

if missing_ids or extra_ids:
    print(f"C001-C042 COVERAGE: FAIL")
else:
    print(f"C001-C042 COVERAGE: PASS")

print(f"\nNon-active IDs: {[m[0] for m in non_active]}")
print(f"Verified Consistent: {[m[0] for m in master if m[1] == '已验证一致']}")
print(f"Original Judgment Error: {[m[0] for m in master if m[1] == '原判断错误']}")
print(f"Completed Back-Write: {[m[0] for m in master if m[1] == '已完成回写']}")
print(f"Merged: {[m[0] for m in master if m[1] == '已合并']}")

print(f"\nC015 final status: active={[m[3] for m in master if m[0]=='C015'][0]}")
print(f"C037 final status: active={[m[3] for m in master if m[0]=='C037'][0]}")
print(f"C039 final status: active={[m[3] for m in master if m[0]=='C039'][0]}")
print(f"C040 final status: active={[m[3] for m in master if m[0]=='C040'][0]}")

print(f"\n{'='*20}")
if not missing_ids and not extra_ids and eq2 and eq3:
    print("FINAL RESULT: PASS")
else:
    print("FINAL RESULT: FAIL")
print(f"{'='*20}")