---
name: winform-designer-std
description: 生成兼容VS设计视图的标准WinForm InitializeComponent代码；禁止手写完整窗体/用户控件类，仅输出标准Designer.cs规范初始化代码，解决设计器无法预览、解析失败问题
---

# Trae Skill：WinForm标准Designer初始化生成规范
## 文件名：`.trae/skills/winform-designer-std/SKILL.md`
```yaml
# Trae Skill 元数据
name: winform-designer-std
version: 1.0.0
description: 生成兼容VS设计视图的标准WinForm InitializeComponent代码；禁止手写完整窗体/用户控件类，仅输出标准Designer.cs规范初始化代码，解决设计器无法预览、解析失败问题
trigger_keywords:
  - WinForm初始化
  - InitializeComponent
  - 修复设计视图无法预览
  - 标准窗体控件初始化
  - SplitContainer Panel布局代码重构
tags: [csharp, winforms, designer, ui-layout, dotnet]
```

## 一、核心强制约束（必遵守，违反直接输出错误）
### ❌ 绝对禁止行为
1. **禁止生成完整Form/UserControl业务类**
   - 不输出`public partial XxxForm : Form`完整业务代码、构造函数、业务事件实现
   - 只输出**`.Designer.cs`标准`InitializeComponent()`主体代码** + 顶层控件私有字段声明
2. **禁止在InitializeComponent内部写匿名Lambda、内联事件订阅**
   设计器解析匿名委托直接崩溃、白屏无法预览
3. **禁止将控件声明为局部变量**
   SplitContainer/Panel/Button等所有UI控件必须是`partial`类私有字段，不能`var sc = new SplitContainer()`局部实例
4. **禁止手动拆分、打乱VS标准初始化顺序**
   不允许自定义嵌套顺序、不允许在布局代码中插入业务逻辑
5. **禁止自定义封装布局工具类嵌入InitializeComponent**
   只能使用原生System.Windows.Forms控件原生属性赋值
6. **禁止在初始化方法内部写业务判断、循环、分支**
   InitializeComponent只能纯静态控件实例化+属性赋值+容器嵌套

### ✅ 强制标准规则（全部落地）
#### 1. 控件字段规范（输出代码首段必须包含）
所有UI控件统一声明为partial类私有字段，前缀`private System.Windows.Forms.XXX 控件名;`，无局部控件
示例：
```csharp
#region 设计器标准控件字段（仅允许此格式）
private SplitContainer splitContainerMain;
private Panel panelSearchBar;
private TextBox textBoxSearch;
private Button buttonNewNote;
#endregion
```

#### 2. InitializeComponent固定执行顺序（严格按此流程）
1. `this.SuspendLayout();` 挂起布局
2. 窗体顶层基础属性赋值（Text/Size/BackColor/Font/MinimumSize等）
3. **一次性全部实例化所有控件**：`控件名 = new 控件类型();` 集中批量new，不能分散
4. 逐个控件属性赋值（Location/Size/Dock/BackColor/Font/FlatStyle等）
5. 容器嵌套逻辑：子控件Add到父Panel/SplitContainer.Panel1/Panel2
6. 顶层容器添加到窗体`this.Controls.Add(顶层容器);`
7. 事件绑定**全部抽离外部方法**，初始化内不写`xxx.Click += (s,e)=>{}`
   - 窗体Load/SizeChanged绑定普通命名方法，禁止lambda
8. `this.ResumeLayout(false);` 收尾

#### 3. 事件绑定规范
- 所有控件事件订阅**移出InitializeComponent**，单独提供`BindControlEvents()`方法统一挂载
- 窗体生命周期事件（Load/SizeChanged）绑定独立命名方法`Form_Load`/`Form_SizeChanged`
- 不允许任何匿名委托、内联回调在初始化块

#### 4. SplitContainer分割线兼容规则
分割距离设置逻辑**不能写在InitializeComponent**，统一放在`Form_Load`独立方法，提供`SafeSetSplitterDistance`辅助方法做边界钳制，防止超出MinSize导致布局错乱

#### 5. 资源规范
Font、Color仅直接赋值，不在InitializeComponent做资源释放、复用逻辑；不新增静态字体全局对象（业务层自行处理GDI泄漏）

## 二、输出代码固定模板（严格遵循此结构输出）
### 输出结构三段式：
1. 【控件私有字段区】#region 设计器标准控件字段
2. 【InitializeComponent标准初始化方法】纯布局无业务、无匿名委托
3. 【外部事件绑定+生命周期辅助方法】分离所有事件订阅逻辑

### 标准输出模板示例
```csharp
#region 设计器标准控件字段
private SplitContainer splitContainerMain;
private SplitContainer splitContainerLeft;
private Panel panelCategory;
private Label labelCategoryTitle;
private ListBox listBoxCategories;
#endregion

private void InitializeComponent()
{
    // 1. 挂起布局
    this.SuspendLayout();

    // 窗体顶层属性
    this.Text = "窗体标题";
    this.Size = new Size(1100, 650);
    this.StartPosition = FormStartPosition.CenterParent;
    this.BackColor = Color.FromArgb(245, 245, 250);
    this.Font = new Font("微软雅黑", 9F);
    this.MinimumSize = new Size(900, 550);

    // 2. 集中实例化全部控件（必须一次性new所有）
    splitContainerMain = new SplitContainer();
    splitContainerLeft = new SplitContainer();
    panelCategory = new Panel();
    labelCategoryTitle = new Label();
    listBoxCategories = new ListBox();

    // 3. 逐个控件属性赋值
    splitContainerMain.Dock = DockStyle.Fill;
    splitContainerMain.Orientation = Orientation.Vertical;
    splitContainerMain.SplitterWidth = 1;
    splitContainerMain.Panel1MinSize = 180;

    labelCategoryTitle.Text = "📂 分类";
    labelCategoryTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
    labelCategoryTitle.Location = new Point(12, 12);
    labelCategoryTitle.AutoSize = true;

    listBoxCategories.Location = new Point(12, 40);
    listBoxCategories.Size = new Size(240, 250);
    listBoxCategories.BorderStyle = BorderStyle.None;

    // 4. 容器嵌套添加子控件
    panelCategory.Controls.Add(labelCategoryTitle);
    panelCategory.Controls.Add(listBoxCategories);
    splitContainerLeft.Panel1.Controls.Add(panelCategory);
    splitContainerMain.Panel1.Controls.Add(splitContainerLeft);

    // 5. 顶层容器挂载到窗体
    this.Controls.Add(splitContainerMain);

    // 6. 布局恢复
    this.ResumeLayout(false);
}

/// <summary>统一绑定所有控件事件，剥离初始化内匿名委托，保证设计器可预览</summary>
private void BindControlEvents()
{
    listBoxCategories.SelectedIndexChanged += ListBoxCategories_SelectedIndexChanged;
    buttonNewNote.Click += ButtonNewNote_Click;
}

private void Form_Load(object sender, EventArgs e)
{
    SafeSetSplitterDistance(splitContainerMain, 220);
}

private void Form_SizeChanged(object sender, EventArgs e)
{
    if (splitContainerMain.IsHandleCreated)
        SafeSetSplitterDistance(splitContainerMain, splitContainerMain.SplitterDistance);
}

/// <summary>分割线安全赋值，防止超出最小尺寸报错</summary>
private void SafeSetSplitterDistance(SplitContainer sc, int dist)
{
    if (dist < sc.Panel1MinSize) dist = sc.Panel1MinSize;
    int maxDist = sc.Width - sc.Panel2MinSize - sc.SplitterWidth;
    if (dist > maxDist) dist = maxDist;
    sc.SplitterDistance = dist;
}

// 业务事件方法占位（仅声明空方法，不实现业务逻辑）
private void ListBoxCategories_SelectedIndexChanged(object sender, EventArgs e) { }
private void ButtonNewNote_Click(object sender, EventArgs e) { }
```

## 三、输入转换处理规则（用户传入自定义布局代码时）
1. 识别用户代码中**局部控件变量**，全部提升为类私有字段，删除局部new
2. 提取所有内联匿名Lambda事件，移动到`BindControlEvents`统一绑定
3. 把Load/SizeChanged内联匿名回调改为独立命名方法
4. 拆分分散的控件new语句，集中到初始化顶部批量实例化
5. 校验SplitContainer.SplitterDistance赋值，移至Form_Load，补充SafeSetSplitterDistance钳制逻辑
6. 剔除InitializeComponent内所有业务代码、判断、循环、资源处理

## 四、输出校验清单（生成后自动自检，不符合重新输出）
输出代码必须全部满足：
- [ ] 所有UI控件均为类私有字段，无局部变量
- [ ] InitializeComponent无任何`xxx += (s,e)=>`匿名委托
- [ ] 控件实例化集中在方法前半段，不分散
- [ ] 事件绑定全部在外部BindControlEvents方法
- [ ] Split分割距离逻辑不在InitializeComponent内部
- [ ] 不包含完整Form/UserControl类、构造函数、业务逻辑
- [ ] 容器嵌套顺序自上而下、从父到子，符合VS自动生成规范
- [ ] 仅输出Designer标准初始化相关代码，不掺杂业务功能实现

## 五、使用说明
1. 在Trae对话输入 `/winform-designer-std` 触发技能
2. 粘贴现有手写布局代码，或描述布局需求（左右分割、上下面板、搜索栏、分页、编辑器等）
3. 技能仅返回标准可预览的Designer初始化代码片段，不生成完整窗体业务类
4. 将输出代码粘贴至`XxxForm.Designer.cs`对应partial类内，窗体业务逻辑保留在XxxForm.cs中

## 六、负面示例（禁止生成的错误写法，识别后自动重构）
### 错误1：局部SplitContainer（设计器无法识别控件，白屏）
```csharp
// 禁止
SplitContainer splitRight = new SplitContainer
{
    Dock = DockStyle.Fill
};
splitRight.Panel1.Controls.Add(panelMiddle);
```
### 错误2：内联Lambda订阅事件（设计视图加载失败）
```csharp
// 禁止
this.Load += (s, e) =>
{
    splitContainerMain.SplitterDistance = 220;
};
```
### 错误3：控件分散实例化（VS自动生成规范不允许）
```csharp
// 禁止
panelSearchBar = new Panel();
panelSearchBar.Dock = DockStyle.Top;
textBoxSearch = new TextBox();
textBoxSearch.Location = new Point(12,12);
```
### 错误4：InitializeComponent内部处理分割距离
```csharp
// 禁止
splitContainerMain.SplitterDistance = 220;
```
```

## 使用说明
1. 将以上完整`SKILL.md`放入项目路径：`项目根目录/.trae/skills/winform-designer-std/SKILL.md`
2. Trae对话输入指令 `/winform-designer-std` 即可激活该规范生成代码
3. 粘贴你现有的WinForm布局代码，技能会自动重构为VS设计器可正常预览的标准Designer初始化代码
4. 输出仅包含控件字段+InitializeComponent+事件绑定辅助方法，**不会生成完整窗体业务类**，严格规避设计视图解析报错问题