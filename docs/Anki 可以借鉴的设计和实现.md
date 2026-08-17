我来深入分析 Anki 可以借鉴的设计和实现。
        
            
toolName: web_search
            
status: success
          
query: Anki SM-2 algorithm implementation spaced repetition Python 2026
          

        
            
toolName: web_search
            
status: success
          
query: Anki architecture design plugin system card template database structure
          

        
            
toolName: web_search
            
status: success
          
query: Anki source code review learning scheduler algorithm explained
          
根据深入研究，Anki 在以下方面可以为你的 LearnTool 项目提供重要借鉴：

## 一、核心算法借鉴（最重要）

### 1. **SM-2 与 FSRS 双引擎设计**

Anki 从 23.10 版本开始提供两种算法，这是非常好的设计思路：

**SM-2 算法核心（传统方案）**
```python
# Anki 的改进版 SM-2 关键逻辑
class SM2Scheduler:
    def __init__(self):
        self.ease_factor = 2.5  # 易度因子（初始值）
    
    def calculate_next_interval(self, quality, current_interval):
        """
        quality: 1=Again, 2=Hard, 3=Good, 4=Easy
        """
        if quality == 1:  # Again - 失败
            # 重置间隔，但不惩罚易度因子（Anki 的改进）
            return 1, self.ease_factor - 0.2
        elif quality == 4:  # Easy - 轻松记住
            # 增加易度因子 + 额外奖励
            new_ease = self.ease_factor + 0.15
            bonus_interval = current_interval * 1.3  # Easy 按钮额外加成
            return bonus_interval * new_ease, new_ease
        else:  # Good/Hard
            new_ease = self.ease_factor + (0.1 - (5-quality) * 0.14)
            return current_interval * new_ease, max(1.3, new_ease)
```

**FSRS 算法核心（机器学习方案）**
```python
# FSRS 三参数模型：DSR（Difficulty, Stability, Retrievability）
class FSRSMemoryState:
    def __init__(self):
        self.difficulty = 0.0   # 卡片难度（0-10）
        self.stability = 0.0    # 记忆稳定性（天数）
        self.retrievability = 1.0  # 回忆概率（0-1）
    
    def predict_next_review(self, days_since_last_review):
        """基于遗忘曲线预测回忆概率"""
        # R = e^(-t/S)  艾宾浩斯公式
        self.retrievability = math.exp(-days_since_last_review / self.stability)
        return self.retrievability
    
    def update_state(self, rating, review_time):
        """根据复习结果更新 DSR 状态（机器学习模型）"""
        # 使用神经网络优化参数
        # 参见 Anki 源码: rslib/src/scheduler/fsrs.rs
```

**借鉴建议：**
- 你的项目可以实现**双引擎设计**：保留现有的简单复习逻辑（类似 SM-2），同时添加可选的 FSRS 机器学习引擎
- 在 `LearningFlowHandler` 中添加算法选择开关，让用户选择 "传统模式" 或 "智能模式"

### 2. **解决 "Ease Hell" 问题**

Anki 传统算法有一个致命缺陷：卡片反复失败会导致易度因子降到最低（130%），陷入短间隔循环。FSRS 通过以下设计解决：

```python
# FSRS 消除 Ease Hell 的核心设计
def update_difficulty_fsrs_style(current_difficulty, rating):
    """
    FSRS 的难度参数具有均值回归特性：
    - 自然趋向平均值（避免极端值）
    - 不会因反复失败而无限恶化
    """
    # 难度参数均值回归公式
    population_mean = 5.0  # 平均难度
    regression_rate = 0.1  # 回归速度
    
    new_difficulty = current_difficulty + \
                     (rating - current_difficulty) * 0.5 + \
                     (population_mean - current_difficulty) * regression_rate
    
    return clamp(new_difficulty, 1, 10)
```

**借鉴建议：**
- 你的 `WrongAnswerService` 应避免"惩罚累积"设计
- 实现均值回归机制：错题难度不应因反复失败而无限增加

### 3. **学习阶段与复习阶段分离**

Anki 的关键创新：
```python
# Anki 学习阶段设计（区别于复习阶段）
class LearningStage:
    """
    学习阶段：新卡片通过短间隔预筛选
    复习阶段：成熟卡片使用长间隔算法
    """
    def __init__(self):
        self.learning_steps = [1, 10]  # 分钟级别：1分钟、10分钟
        self.relearning_steps = [10, 20]  # 失败后的重学步骤
    
    def process_new_card(self, card):
        """新卡片先通过学习阶段"""
        if card.reps == 0:
            # 第一次见：1分钟后复习
            return self.learning_steps[0]
        elif card.reps < len(self.learning_steps):
            # 学习阶段中：逐步延长
            return self.learning_steps[card.reps]
        else:
            # 毕业到复习阶段：进入 SM-2/FSRS 算法
            return self.calculate_review_interval(card)
```

**借鉴建议：**
- 你的学习内容应区分：
  - **新学习阶段**（使用分钟级短间隔）
  - **巩固复习阶段**（使用天级长间隔）
- 在 `ContentEditorPresenter` 中添加"学习阶段状态"字段

---

## 二、数据架构借鉴

### 1. **Note-Card-Template 三层分离**

Anki 的核心数据架构：

```
Note (笔记/内容源)
  ├── 字段: {正面: "apple", 背面: "苹果", 例句: "..."}
  └── 关联多个 Card

Card (复习单元)
  ├── id: 唯一标识
  ├── nid: Note ID
  ├── due: 下次复习时间
  ├── ivl: 当前间隔
  ├── factor: 易度因子
  └── reps/lapses: 复习次数/遗忘次数

Template (模板/呈现规则)
  ├── q_format: 正面模板 "{{正面}}"
  ├── a_format: 背面模板 "{{背面}}"
  └── ord: 模板序号（一个 Note 可生成多张 Card）
```

**数据库结构（SQLite）：**
```sql
-- Anki cards 表设计（借鉴核心字段）
CREATE TABLE cards (
    id INTEGER PRIMARY KEY,
    nid INTEGER NOT NULL,  -- 关联 Note
    did INTEGER NOT NULL,  -- 卡组 ID
    ord INTEGER NOT NULL,  -- 模板序号
    mod INTEGER NOT NULL,  -- 修改时间戳
    type INTEGER NOT NULL, -- 0=new, 1=learning, 2=review
    queue INTEGER NOT NULL, -- 状态队列
    due INTEGER NOT NULL,  -- 下次复习时间
    ivl INTEGER NOT NULL,  -- 当前间隔（天）
    factor INTEGER NOT NULL, -- 易度因子（千分比）
    reps INTEGER NOT NULL,  -- 复习次数
    lapses INTEGER NOT NULL, -- 遗忘次数
    left INTEGER NOT NULL,  -- 剩余学习步骤
    odue INTEGER,           -- 原始到期时间
    flags INTEGER NOT NULL  -- 用户标记
);

-- 复习日志表（关键数据）
CREATE TABLE revlog (
    id INTEGER PRIMARY KEY,
    cid INTEGER NOT NULL,   -- Card ID
    usn INTEGER NOT NULL,   -- 更新序列号
    ease INTEGER NOT NULL,  -- 评分（1-4）
    ivl INTEGER NOT NULL,   -- 间隔
    lastIvl INTEGER,        -- 上次间隔
    factor INTEGER,         -- 易度因子
    time INTEGER NOT NULL,  -- 复习耗时（毫秒）
    type INTEGER NOT NULL   -- 卡片类型
);
```

**借鉴建议：**
- 你的 `LearningContent` 应拆分为：
  - `ContentItem`（类似 Note，存储原始内容）
  - `ReviewCard`（类似 Card，存储复习状态）
  - `ContentTemplate`（类似 Template，定义呈现规则）
- 添加 `ReviewLog` 表记录每次复习详情（用于 FSRS 机器学习）

### 2. **模板渲染引擎**

Anki 的模板系统支持复杂逻辑：

```html
<!-- Anki 模板语法示例 -->
<!-- 正面模板 -->
<div class="card">
    <h1>{{正面}}</h1>
    {{#提示}}<div class="hint">{{提示}}</div>{{/提示}}
    {{^例句}}<div class="empty">无例句</div>{{/例句}}
</div>

<!-- 背面模板 -->
<div class="card back">
    <h1>{{正面}}</h1>
    <hr>
    <div class="answer">{{背面}}</div>
    {{#例句}}<div class="example">{{例句}}</div>{{/例句}}
</div>
```

**过滤器系统：**
```html
{{text:字段}}      <!-- 纯文本格式 -->
{{hint:字段}}      <!-- 提示字段（点击显示） -->
{{type:字段}}      <!-- 打字输入测试 -->
{{cloze:字段}}     <!-- 填空题 -->
{{tts:字段}}       <!-- 文本转语音 -->
```

**借鉴建议：**
- 你的 `ContentEditorForm` 可以支持模板系统：
  - 不同内容类型使用不同模板（识字、单词、成语）
  - 支持条件显示（如：有例句时显示，否则隐藏）
  - 支持音频字段（`{{tts:内容}}`）

---

## 三、插件系统借鉴

### 1. **Hook 和 Filter 架构**

Anki 的扩展系统核心：

```python
# Anki Hook 系统（插件扩展点）
class AnkiHooks:
    """
    Hook: 在特定时机注入自定义代码
    Filter: 修改数据/行为后传递
    """
    
    # 示例 Hook 点
    hooks = {
        'card_will_show': [],     # 卡片显示前
        'review_did_finish': [],  # 复习完成后
        'note_will_save': [],     # Note 保存前
        'card_did_leech': [],     # 水蛭卡片检测
    }
    
    def add_hook(self, hook_name, callback):
        """添加钩子函数"""
        self.hooks[hook_name].append(callback)
    
    def run_hooks(self, hook_name, *args):
        """执行所有钩子"""
        for callback in self.hooks[hook_name]:
            callback(*args)

# 使用示例：添加统计插件
def my_stats_plugin(card, rating):
    """自定义复习统计"""
    print(f"复习卡片 {card.id}, 评分 {rating}")

add_hook('review_did_finish', my_stats_plugin)
```

**借鉴建议：**
- 你的项目已使用 `IEventBus`，可以进一步增强：
  - 添加更多 Hook 点（如：`ContentWillLearn`, `ContentDidWrong`, `PomodoroDidComplete`)
  - 支持 Filter 模式（修改事件数据，如：添加额外奖励）

### 2. **Python-JavaScript 桥接**

Anki 的 Webview 交互机制：

```python
# Anki 的 JS-Python 桥接
class WebViewBridge:
    """
    前端 JS 通过 pycmd() 调用 Python
    Python 通过 webview.eval() 执行 JS
    """
    
    def pycmd(self, command):
        """JS 调用 Python 命令"""
        # command = "func_name:param1,param2"
        func_name, params = command.split(':')
        return getattr(self, func_name)(params)
    
    def eval_js(self, js_code):
        """Python 执行 JS 代码"""
        self.webview.eval(js_code)

# JS 端代码（注入到卡片）
function callPython(action, data) {
    pycmd(action + ":" + JSON.stringify(data));
}

// 示例：点击按钮调用 Python
document.getElementById('btn').onclick = function() {
    callPython('play_audio', {card_id: 123});
};
```

**借鉴建议：**
- 你的 WPF 项目可以使用类似机制：
  - C# 通过 `WebBrowser.InvokeScript()` 执行 JS
  - JS 通过 `window.external.CallCSharpMethod()` 调用 C#
  - 实现动态交互（如：语音朗读、即时测验）

---

## 四、同步机制借鉴

### 1. **增量同步策略**

Anki 的同步设计：

```python
# Anki 增量同步核心逻辑
class AnkiSyncManager:
    """
    使用 USN (Update Sequence Number) 追踪变更
    - 本地 USN < 服务器 USN → 拉取更新
    - 本地 USN = -1 → 推送更新
    """
    
    def sync(self):
        server_usn = self.get_server_usn()
        local_usn = self.get_local_usn()
        
        if local_usn == -1:
            # 有本地修改，推送
            changes = self.get_uncommitted_changes()
            self.push_to_server(changes)
        
        if local_usn < server_usn:
            # 服务器有更新，拉取
            server_changes = self.fetch_server_changes(local_usn)
            self.apply_changes(server_changes)
        
        self.sync_media()  # 同步图片/音频文件
```

**借鉴建议：**
- 你的项目目前使用 JSON + SQLite 双系统，可以改进：
  - 统一使用 SQLite，添加 `usn` 字段
  - 实现增量同步（只传输变更）
  - 支持云端备份（类似 AnkiWeb）

### 2. **冲突处理机制**

```python
# Anki 冲突处理策略
def handle_sync_conflict(local_card, server_card):
    """
    冲突解决策略：
    1. 优先保留更多复习历史的版本
    2. 合并非冲突字段
    3. 标记冲突让用户手动选择
    """
    if local_card.reps > server_card.reps:
        # 本地复习次数更多 → 保留本地
        return local_card
    elif server_card.mod > local_card.mod:
        # 服务器修改时间更晚 → 保留服务器
        return server_card
    else:
        # 无法自动解决 → 创建冲突副本
        return ConflictCard(local_card, server_card)
```

---

## 五、统计分析借鉴

### 1. **记忆保留率计算**

Anki 的统计核心：

```python
# Anki 记忆保留率计算
class RetentionCalculator:
    """
    基于 FSRS 模型计算保留率
    """
    
    def calculate_retention(self, cards, days):
        """
        R = e^(-t/S)
        计算指定天数后的记忆保留率
        """
        retention_sum = 0
        for card in cards:
            R = math.exp(-days / card.stability)
            retention_sum += R
        return retention_sum / len(cards)
    
    def predict_workload(self, target_retention):
        """
        根据目标保留率预测每日复习量
        FSRS 可以精确预测工作量
        """
        # 参见 FSRS Simulator 源码
        pass
```

**借鉴建议：**
- 你的 `LearningAnalyticsService` 可以添加：
  - 实时记忆保留率计算
  - 未来复习工作量预测
  - 个性化难度曲线分析

### 2. **学习效率可视化**

Anki 的统计图表：

```python
# Anki 统计数据结构
class AnkiStatistics:
    def get_daily_stats(self):
        """每日统计"""
        return {
            'reviewed': count,       # 复习卡片数
            'learned': new_cards,    # 新学卡片数
            'time_spent': minutes,   # 复习耗时
            'retention': percentage, # 记忆保留率
            'ease_avg': average,     # 平均易度因子
        }
    
    def get_forgetting_curve(self):
        """遗忘曲线数据"""
        # 基于 revlog 表计算
        pass
    
    def get_heatmap_data(self):
        """学习热力图（52周）"""
        pass
```

**借鉴建议：**
- 你的 `LearningManagementForm` 可以添加：
  - 遗忘曲线图（预测记忆衰减）
  - 学习热力图（可视化学习习惯）
  - 复习效率分析（时间/卡片比例）

---

## 六、具体实施建议

### **阶段一：算法引入（优先级最高）**

1. 在 `Services/Learning` 中添加 `SpacedRepetitionAlgorithm` 接口：
   ```csharp
   public interface ISpacedRepetitionAlgorithm
   {
       DateTime CalculateNextReview(LearningItem item, ReviewRating rating);
       void UpdateMemoryState(LearningItem item, ReviewRating rating);
       double PredictRetention(LearningItem item, int days);
   }
   ```

2. 实现两个算法引擎：
   - `SM2Algorithm.cs`（传统方案，易实现）
   - `FSRSAlgorithm.cs`（智能方案，需训练数据）

3. 在 `LearningFlowHandler` 中集成：
   ```csharp
   public class LearningFlowHandler
   {
       private ISpacedRepetitionAlgorithm _algorithm;
       
       public void SetAlgorithm(string algorithmType)
       {
           _algorithm = algorithmType == "FSRS" 
               ? new FSRSAlgorithm() 
               : new SM2Algorithm();
       }
   }
   ```

### **阶段二：数据结构优化**

1. 添加 `ReviewLog` 表：
   ```sql
   CREATE TABLE ReviewLog (
       Id INTEGER PRIMARY KEY,
       ContentId INTEGER NOT NULL,
       Rating INTEGER NOT NULL,
       Interval INTEGER NOT NULL,
       EaseFactor REAL,
       ReviewTime INTEGER NOT NULL,
       Duration INTEGER,
       AlgorithmType TEXT
   );
   ```

2. 修改 `LearningContent` 结构：
   ```csharp
   public class LearningContent
   {
       // 新增字段
       public double Difficulty { get; set; }     // FSRS 参数
       public double Stability { get; set; }      // FSRS 参数
       public double EaseFactor { get; set; }     // SM-2 参数
       public int LearningStage { get; set; }     // 0=新, 1=学习, 2=复习
   }
   ```

### **阶段三：统计分析增强**

1. 在 `LearningAnalyticsService` 中添加：
   ```csharp
   public class LearningAnalyticsService
   {
       public double CalculateRetentionRate(DateTime targetDate);
       public List<ReviewPrediction> PredictFutureWorkload(int days);
       public ForgettingCurveData GenerateForgettingCurve();
       public HeatmapData GenerateWeeklyHeatmap();
   }
   ```

### **阶段四：插件系统完善**

1. 增强 `IEventBus`：
   ```csharp
   // 添加更多事件点
   public interface ILearningEventBus
   {
       void OnContentWillShow(ContentItem item);     // Hook 点
       void OnReviewDidFinish(ReviewLog log);        // Hook 点
       void OnAlgorithmDidChange(string algorithm);  // Filter 点
   }
   ```

---

## 推荐优先级

**P0（立即实施）：**
- 引入 SM-2 算法核心逻辑
- 添加 `ReviewLog` 表
- 实现学习阶段分离

**P1（近期实施）：**
- 实现 FSRS 算法框架
- 添加统计分析功能（遗忘曲线）
- 优化数据结构

**P2（中期实施）：**
- 完善插件系统
- 实现增量同步机制
- 添加模板引擎支持

通过借鉴 Anki 的这些核心设计，你的 LearnTool 项目将从简单的学习工具升级为**科学化、智能化的学习系统**，特别是在记忆算法、数据分析、扩展性方面会有质的飞跃。