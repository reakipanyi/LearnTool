#!/usr/bin/env python3
"""
清理 LearningForm.cs 中的重复方法。
策略：找到所有方法定义，比较方法签名，删除重复的定义。
对于重复方法，保留第一个定义（即行号较小的那个）。
"""

import re
import os

file_path = '/workspace/LearningAssistant/Forms/LearningForm.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

total_lines = len(lines)
print(f"原文件总行数: {total_lines}")

# 1. 找出所有方法定义
# 匹配形如：private void MethodName(...) 或 private string MethodName(...) 等
method_pattern = re.compile(
    r'^\s*(private|public|internal|protected)\s+(static\s+)?(void|string|List[^<]*<[^>]*>|Dictionary[^<]*<[^>]*>|bool|int|long|double|float|DateTime|TimeSpan|Color|object|IEnumerable[^<]*<[^>]*>|[\w<>]+)\s+(\w+)\s*\('
)

# 找到每个方法的起始行（行号从1开始）
methods = []  # (line_index_0based, method_name, signature)

for i, line in enumerate(lines):
    m = method_pattern.match(line)
    if m:
        method_name = m.group(4)
        # 排除 get/set 等属性访问器（在单独的行上）
        # 排除类构造函数（类名）
        signature = line.strip()
        # 只记录能唯一标识方法的信息：方法名 + 前一行的可见标识
        methods.append((i, method_name, signature))

print(f"\n找到 {len(methods)} 个方法定义")

# 2. 找出重复的方法名
from collections import defaultdict
method_map = defaultdict(list)
for (idx, name, sig) in methods:
    method_map[name].append((idx, sig))

duplicates = {name: locs for name, locs in method_map.items() if len(locs) > 1}
print(f"\n重复方法 {len(duplicates)} 个：")
for name in sorted(duplicates.keys()):
    locs = duplicates[name]
    print(f"  {name}: 出现 {len(locs)} 次，位置行: {[l[0]+1 for l in locs]}")

# 3. 现在确定要删除的方法
# 规则：对每个重复方法，保留第一个（最小行号的），删除后面的
# 但要注意：我们需要精确找到每个方法体的开始和结束（匹配大括号）

def find_method_end(lines, start_idx):
    """
    找到方法体结束的行索引（0-based，包含）。
    方法从 start_idx 行开始，寻找匹配的大括号。
    """
    # 先找到方法的开头 '{'
    brace_count = 0
    found_open = False
    i = start_idx
    while i < len(lines):
        line = lines[i]
        for ch in line:
            if ch == '{':
                brace_count += 1
                found_open = True
            elif ch == '}':
                brace_count -= 1
                if found_open and brace_count == 0:
                    return i
        i += 1
    return len(lines) - 1

# 记录要删除的行范围 [start, end]（包含）
ranges_to_delete = []

for name, locs in sorted(duplicates.items(), key=lambda x: x[1][0][0]):
    # 保留第一个（locs[0]），删除从 locs[1] 开始的
    for (idx, sig) in locs[1:]:
        end_idx = find_method_end(lines, idx)
        # 还要删除方法前的注释（如果有）
        start_idx = idx
        # 向上搜索空行或注释分隔
        j = idx - 1
        while j >= 0:
            stripped = lines[j].strip()
            if stripped.startswith('///') or stripped.startswith('//'):
                start_idx = j
                j -= 1
            elif stripped == '':
                start_idx = j
                j -= 1
            else:
                break
        ranges_to_delete.append((start_idx, end_idx, name))
        print(f"  删除 {name}: 行 {start_idx+1} - {end_idx+1}")

# 4. 合并相邻的范围并排序
ranges_to_delete.sort(key=lambda x: x[0])

# 合并重叠或相邻的范围
merged = []
for start, end, name in ranges_to_delete:
    if merged and start <= merged[-1][1] + 1:
        merged[-1] = (merged[-1][0], max(end, merged[-1][1]), merged[-1][2] + ", " + name)
    else:
        merged.append((start, end, name))

print(f"\n合并后有 {len(merged)} 个删除区域")
total_delete = sum(e - s + 1 for s, e, _ in merged)
print(f"将删除约 {total_delete} 行")

# 5. 构建新文件（保留不在删除范围内的行）
delete_set = set()
for start, end, _ in merged:
    for i in range(start, end + 1):
        delete_set.add(i)

new_lines = [line for i, line in enumerate(lines) if i not in delete_set]

# 6. 写入新文件
backup_path = file_path + '.py_backup'
with open(backup_path, 'w', encoding='utf-8') as f:
    f.writelines(lines)

with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print(f"\n完成！原文件备份在: {backup_path}")
print(f"新文件行数: {len(new_lines)}")
print(f"删除: {total_lines - len(new_lines)} 行")
