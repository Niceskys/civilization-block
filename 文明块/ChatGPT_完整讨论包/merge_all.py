# -*- coding: utf-8 -*-
import os, shutil

# Current directory is: d:\超级文档管理\文明块仓库\文明块
# Source files are in: 文明块\01 - 世界底层规则\...
# Target: ChatGPT_完整讨论包\

src_base = os.path.join(os.getcwd(), '文明块')
dst_base = os.path.join(os.getcwd(), 'ChatGPT_完整讨论包')

os.makedirs(dst_base, exist_ok=True)

def merge(files, outname):
    content = []
    content.append(f'# 合并文件: {outname}')
    content.append('')
    for f in files:
        path = os.path.join(src_base, f)
        if os.path.exists(path):
            with open(path, 'r', encoding='utf-8') as fp:
                txt = fp.read()
                content.append(f'## 【来源文档】{f}')
                content.append('')
                content.append(txt)
                content.append('')
                content.append('---')
                content.append('')
        else:
            print(f'WARNING: {path} not found!')
    outpath = os.path.join(dst_base, outname)
    with open(outpath, 'w', encoding='utf-8') as f:
        f.write('\n'.join(content))
    print(f'Created: {outname}')

print(f'Current dir: {os.getcwd()}')
print(f'Src base: {src_base}')
print(f'Dst base: {dst_base}')

# File 1
merge([
    '01 - 世界底层规则\\1.0 世界生成与生物群系规则.md',
    '01 - 世界底层规则\\1.1 地皮基础规则.md',
    '01 - 世界底层规则\\1.2 建筑堆叠规则.md',
], '01_世界底层规则(上)_世界生成+地皮+堆叠.md')

# File 2
merge([
    '01 - 世界底层规则\\1.3 时间系统规则.md',
    '01 - 世界底层规则\\1.4 胜负与目标规则.md',
    '01 - 世界底层规则\\1.5 相机与3D视角规则.md',
    '01 - 世界底层规则\\1.6 随机事件系统.md',
    '01 - 世界底层规则\\1.7 地下挖掘与探索系统.md',
], '02_世界底层规则(下)_时间+胜负+相机+事件+地下.md')

# File 3
merge([
    '02 - 建筑系统\\2.1 建筑通用规则.md',
    '02 - 建筑系统\\2.2 堆叠交互规则.md',
    '02 - 建筑系统\\2.3 建筑3D视觉规范.md',
    '02 - 建筑系统\\2.4 工具制造系统.md',
], '03_建筑通用规则+堆叠交互+3D视觉+工具系统.md')

# File 4
merge([
    '02 - 建筑系统\\单个建筑详情\\房屋.md',
    '02 - 建筑系统\\单个建筑详情\\农田.md',
    '02 - 建筑系统\\单个建筑详情\\水井.md',
    '02 - 建筑系统\\单个建筑详情\\树场.md',
    '02 - 建筑系统\\单个建筑详情\\采掘场.md',
    '02 - 建筑系统\\单个建筑详情\\熔炉房.md',
    '02 - 建筑系统\\单个建筑详情\\太阳灯.md',
    '02 - 建筑系统\\单个建筑详情\\农舍.md',
    '02 - 建筑系统\\单个建筑详情\\垃圾处理场.md',
], '04_建筑详情手册(上)_居住+生产+功能类.md')

# File 5
merge([
    '02 - 建筑系统\\单个建筑详情\\仓库.md',
    '02 - 建筑系统\\单个建筑详情\\哨塔.md',
    '02 - 建筑系统\\单个建筑详情\\陷阱.md',
    '02 - 建筑系统\\单个建筑详情\\研究站.md',
    '02 - 建筑系统\\单个建筑详情\\管道.md',
    '02 - 建筑系统\\单个建筑详情\\疗愈所.md',
    '02 - 建筑系统\\单个建筑详情\\城墙.md',
    '02 - 建筑系统\\单个建筑详情\\工坊.md',
    '02 - 建筑系统\\单个建筑详情\\矿脉挖掘点.md',
], '05_建筑详情手册(下)_防御+科技+功能类.md')

# File 6
merge([
    '03-NPC 系统\\3.1 属性与生命周期.md',
    '03-NPC 系统\\3.2 生育规则.md',
    '03-NPC 系统\\3.3 工作分配逻辑.md',
    '03-NPC 系统\\3.4 恐惧值系统.md',
], '06_NPC系统(上)_属性+生育+工作+恐惧.md')

# File 7
merge([
    '03-NPC 系统\\3.5 职业系统通用规则.md',
    '03-NPC 系统\\3.6 职业详情清单.md',
    '03-NPC 系统\\3.7 NPC好感度系统.md',
    '03-NPC 系统\\3.8 NPC社交事件系统.md',
], '07_NPC系统(下)_职业+好感度+社交事件.md')

# File 8
merge([
    '04 - 资源与生产链\\4.1 全资源清单.md',
    '04 - 资源与生产链\\4.2 生产链路总览.md',
    '04 - 资源与生产链\\4.3 存储规则.md',
    '04 - 资源与生产链\\4.4 垃圾机制.md',
], '08_资源与生产链.md')

# File 9
merge([
    '05-UI 与交互设计\\5.1 全局界面.md',
    '05-UI 与交互设计\\5.2 建造系统 UI.md',
    '05-UI 与交互设计\\5.3 建筑详情 UI.md',
], '09_UI设计(上)_全局界面+建造+建筑详情.md')

# File 10
merge([
    '05-UI 与交互设计\\5.4 NPC 管理 UI.md',
    '05-UI 与交互设计\\5.5 装饰与自定义系统.md',
    '05-UI 与交互设计\\5.6 UI原型设计蓝图.md',
], '10_UI设计(中)_NPC管理+装饰+原型蓝图.md')

# File 11
merge([
    '05-UI 与交互设计\\5.7 玩家留存与分享系统.md',
    '05-UI 与交互设计\\5.8 UI交互体验优化规范.md',
], '11_UI玩家留存+交互优化规范.md')

# File 12
merge([
    '06 - 全局数值总表\\6.1 建筑数值表.md',
    '06 - 全局数值总表\\6.2 NPC 数值表.md',
    '06 - 全局数值总表\\6.3 难度选项.md',
], '12_全局数值总表_建筑+NPC+难度.md')

# File 13
merge([
    '07 - 边界与异常规则\\7.1 极端情况处理.md',
    '07 - 边界与异常规则\\7.2 怪物与防御系统.md',
], '13_边界异常规则+怪物与防御系统.md')

# File 14
merge([
    '08 - 长线进阶内容\\8.1 后续更新规划.md',
    '08 - 长线进阶内容\\8.2 科技树系统.md',
    '08 - 长线进阶内容\\8.3 挑战模式与成就解锁系统.md',
], '14_长线进阶(上)_规划+科技树+挑战成就.md')

# File 15
merge([
    '08 - 长线进阶内容\\8.4 外部世界与贸易路线.md',
    '08 - 长线进阶内容\\8.5 传承模式.md',
    '08 - 长线进阶内容\\8.6 世界观与隐藏剧情.md',
], '15_长线进阶(下)_贸易+传承+世界观剧情.md')

# File 16
merge([
    '09-商店系统\\9.1 商店通用规则.md',
    '09-商店系统\\9.2 商品清单.md',
    '09-商店系统\\9.3 商店UI设计.md',
], '16_商店系统_通用规则+商品+UI.md')

# File 17
merge([
    '10-任务系统\\10.1 任务系统通用规则.md',
    '10-任务系统\\10.2 任务清单.md',
    '10-任务系统\\10.3 任务UI设计.md',
], '17_任务系统_通用规则+清单+UI.md')

# File 18
shutil.copy2(
    os.path.join(src_base, '01 - 世界底层规则\\1.8 城市视觉规划系统.md'),
    os.path.join(dst_base, '18_城市视觉规划系统.md')
)
print('Created: 18_城市视觉规划系统.md')

# File 19
shutil.copy2(
    os.path.join(src_base, '99 - 美术与UI制作指南\\99.1 全流程美术制作手册.md'),
    os.path.join(dst_base, '19_全流程美术制作手册.md')
)
print('Created: 19_全流程美术制作手册.md')

# File 20 - copy from existing summary document 
summary_src = os.path.join(os.getcwd(), '99 - 美术与UI制作指南\\ChatGPT5.5_UI设计需求完整文档.md')
if os.path.exists(summary_src):
    shutil.copy2(summary_src, os.path.join(dst_base, '20_ChatGPT5.5_UI设计需求完整文档.md'))
    print('Created: 20_ChatGPT5.5_UI设计需求完整文档.md')
else:
    print(f'WARNING: {summary_src} not found')

# File 21 - 建筑设计需求文档
summary_src2 = os.path.join(os.getcwd(), '99 - 美术与UI制作指南\\ChatGPT5.5_建筑设计需求完整文档.md')
if os.path.exists(summary_src2):
    shutil.copy2(summary_src2, os.path.join(dst_base, '21_ChatGPT5.5_建筑设计需求完整文档.md'))
    print('Created: 21_ChatGPT5.5_建筑设计需求完整文档.md')
else:
    print(f'WARNING: {summary_src2} not found')

print('\n=== ALL DONE ===')
files = [f for f in os.listdir(dst_base) if f != 'merge_all.py']
print(f'Total files: {len(files)}')
total_size = 0
for f in sorted(files):
    sz = os.path.getsize(os.path.join(dst_base, f))
    total_size += sz
    print(f'  {f} ({sz//1024}KB)')
print(f'Total size: {total_size//1024}KB')