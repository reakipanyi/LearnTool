#!/usr/bin/env python3
"""验证 LearningForm.cs 清理后的文件：
1. 统计总行数
2. 检查所有方法定义是否唯一
3. 检查文件首尾内容是否完整
4. 检查保留的独特方法（Badges, Challenges, Level, MiniGame 等）是否存在
"""
import re
from collections import defaultdict

file_path = '/workspace/LearningAssistant/Forms/LearningForm.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

print(f"文件总行数: {len(lines)}")
print()

# 找出所有方法定义
method_pattern = re.compile(
    r'^\s*(private|public|internal|protected)\s+(static\s+)?(void|string|List[^<]*<[^>]*>|Dictionary[^<]*<[^>]*>|bool|int|long|double|float|DateTime|TimeSpan|Color|object|IEnumerable[^<]*<[^>]*>|[\w<>]+)\s+(\w+)\s*\('
)

methods = []
for i, line in enumerate(lines):
    m = method_pattern.match(line)
    if m:
        methods.append((i + 1, m.group(4), line.strip()))

print(f"找到 {len(methods)} 个方法定义")

method_map = defaultdict(list)
for (ln, name, sig) in methods:
    method_map[name].append(ln)

duplicates = {n: locs for n, locs in method_map.items() if len(locs) > 1}

if duplicates:
    print(f"\n⚠️  警告：仍然有 {len(duplicates)} 个重复方法：")
    for n in sorted(duplicates.keys()):
        print(f"  {n}: 行 {duplicates[n]}")
else:
    print(f"\n✅  确认：所有 {len(method_map)} 个方法都是唯一的！")

# 检查应该保留的独特方法
expected_unique = [
    'LoadBadges', 'SaveBadges', 'CheckBadgeUnlock', 'TryUnlockBadge',
    'UnlockBadge', 'ShowBadgeNotification', 'UpdateBadgesDisplay', 'Badge_Click',
    'LoadChallenges', 'SaveChallenges', 'UpdateChallengesDisplay', 'ClaimChallenge',
    'UpdateChallengesProgress', 'CheckLevelUp', 'UpdateLevelDisplay',
    'StartMiniGame', 'NextGameQuestion', 'ButtonGameSubmit_Click', 'GameTimer_Tick',
    'UpdateEncouragement'
]

print(f"\n--- 检查独特方法是否存在 ---")
for m in expected_unique:
    if m in method_map:
        print(f"  ✅ {m}: 行 {method_map[m]}")
    else:
        print(f"  ❌ {m}: 缺失！")

# 检查基础方法
base_methods = [
    'LoadStudyStats', 'SaveStudyStats', 'UpdateStatsDisplay',
    'StudyTimer_Tick', 'IncrementScore', 'UpdateListStatus',
    'ListBoxItems_DrawItem', 'ListBoxItems_SelectedIndexChanged',
    'PanelContent_Paint', 'labelAI_Click', 'mainTableLayoutPanel_Paint',
    'LabelContent_Click', 'ListBoxDisplay_Click', 'ToggleDetail',
    'FormatDisplayText', 'UpdateDetailContent', 'UpdateLearningList',
    'UpdateLearningListSelection', 'EnableListHighlighting',
    'RadioStudyMode_CheckedChanged', 'ButtonKnown_Click', 'ButtonNext_Click',
    'CheckBoxShowDetail_CheckedChanged', 'StartConfetti', 'ConfettiTimer_Tick',
    'DrawConfetti', 'ButtonFavorite_Click', 'SaveFavorite', 'RemoveFavorite',
    'ButtonNote_Click', 'LoadNotes', 'SaveNotes', 'RichTextBoxNotes_TextChanged',
    'LearningForm_KeyDown', 'ButtonQuizMode_Click', 'ButtonRevealAnswer_Click',
    'HideAnswer', 'ShowAnswer', 'ButtonThemeToggle_Click',
    'InitializeComponent', 'InitializeEnhancedFeatures',
    'ApplyTheme', 'ApplyThemeToControl', 'LearningForm_Load', 'LearningForm_FormClosing',
    'LoadSettings', 'SaveSettings', 'ApplySettings', 'AdjustFontSizeBasedOnContent',
    'ResetDetailState', 'ResetFavoriteState', 'UpdateFavoriteButton'
]

print(f"\n--- 检查基础方法是否存在 ---")
missing = []
for m in base_methods:
    if m in method_map:
        print(f"  ✅ {m}: 行 {method_map[m]}")
    else:
        print(f"  ⚠️  {m}: 缺失（可能是设计器事件或委托）")
        missing.append(m)

# 检查文件首尾
print(f"\n--- 检查文件结构 ---")
print(f"  第一行: {lines[0].strip()}")
print(f"  最后一行: {lines[-1].strip()}")

# 检查类是否正确闭合
class_open = sum(1 for l in lines if 'public partial class LearningForm' in l)
print(f"  类声明数量: {class_open}")

# 检查 namespace
ns_count = sum(1 for l in lines if l.strip() == 'namespace LearningAssistant.Forms')
print(f"  namespace 声明: {ns_count}")

# 计算主要的大括号
all_open = sum(1 for l in lines for ch in l if ch == '{')
all_close = sum(1 for l in lines for ch in l if ch == '}')
print(f"  大括号 {{{all_open}}} vs }}{{{all_close}}}")

print("\n=== 清理验证完成 ===")
