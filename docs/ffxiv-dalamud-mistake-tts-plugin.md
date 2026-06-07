# FFXIV / Dalamud 本地犯错记录与 TTS 提醒插件技术文档

## 1. 项目定位

本插件用于在本地记录团队成员在副本机制中的常见失误，并在下一次同一机制到来前通过 TTS 播报提醒。

当前范围限定为：

- **平台**：FFXIV / Dalamud 插件
- **模式**：本地模式，不做云端同步，不做队伍间网络共享
- **记录方式**：第一版以手动记录为主，后续可加入半自动识别
- **提醒方式**：基于副本时间轴或触发器，在机制前 TTS 播报
- **存储方式**：本地 JSON 配置与数据文件

插件目标不是替代指挥，也不是自动解机制，而是做一个“团队记忆本”：谁在哪个机制犯过什么错，下次机制前提前提醒。

## 2. 合规与边界

Dalamud 官方插件限制中，对战斗相关插件比较敏感。根据官方限制，插件不应自动操作游戏，也不应直接替玩家做决策或执行动作。这个插件应坚持以下边界：

- 不自动按技能
- 不自动移动角色
- 不读取或发送非必要敏感信息
- 不向队伍聊天自动刷屏
- 不根据隐藏信息做超出游戏正常可见范围的判断
- TTS 内容只基于本地人工记录、队伍成员、时间轴和公开可见事件

如果未来想提交官方仓库，需要重新审视机制提醒是否符合官方审核标准。若只是自用或小范围本地测试，也应尽量保持“辅助记忆”而不是“自动解题”的方向。

## 3. MVP 功能

第一版建议只做以下功能：

- 插件主窗口
- 团队成员列表
- 机制时间轴列表
- 犯错记录新增、编辑、删除
- 本地 JSON 保存
- 开战后计时
- 机制前 N 秒播报
- TTS 开关、音量、语速、播报模板
- 命令入口，例如 `/wym`

不建议第一版就做：

- 云同步
- ACT 日志深度解析
- 自动判定所有机制失误
- 复杂统计图表
- 自动导入所有副本时间轴

## 4. 推荐项目结构

```text
WeiyueMistakeTTS/
  WeiyueMistakeTTS.csproj
  WeiyueMistakeTTS.json
  Plugin.cs
  Configuration.cs
  Services/
    MistakeStore.cs
    TimelineService.cs
    ReminderService.cs
    TtsService.cs
    PartyService.cs
    EncounterClock.cs
  Models/
    MistakeRecord.cs
    TeamMember.cs
    MechanicDefinition.cs
    EncounterDefinition.cs
    ReminderRule.cs
  Windows/
    MainWindow.cs
    SettingsWindow.cs
    TimelineWindow.cs
  Utils/
    TimeFormatter.cs
    TextTemplateRenderer.cs
```

说明：

- `Plugin.cs`：Dalamud 插件入口，负责服务注入、命令注册和窗口注册
- `Configuration.cs`：Dalamud 插件配置
- `Services/`：核心服务，避免把业务堆进窗口类
- `Models/`：数据结构
- `Windows/`：ImGui 窗口
- `Utils/`：文本模板、时间格式等工具

## 5. Dalamud 入口设计

Dalamud 插件通常以 `IDalamudPlugin` 作为入口，并通过构造函数注入所需服务。常用服务包括：

- `IDalamudPluginInterface`：插件接口、配置读写、UI Builder
- `ICommandManager`：注册聊天命令
- `IChatGui`：输出聊天提示
- `IClientState`：当前角色、地图、区域等状态
- `IPartyList`：当前小队成员
- `IFramework`：每帧更新，用于计时和触发提醒
- `ICondition`：检测是否处于战斗等状态

入口职责：

- 加载配置
- 初始化服务
- 注册 `/wym` 命令
- 注册窗口绘制事件
- 在 `Dispose` 中解绑命令、事件和 TTS 资源

伪代码：

```csharp
public sealed class Plugin : IDalamudPlugin
{
    public string Name => "Weiyue Mistake TTS";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly IFramework framework;
    private readonly WindowSystem windowSystem = new("WeiyueMistakeTTS");

    private Configuration config;
    private MistakeStore mistakeStore;
    private ReminderService reminderService;
    private TtsService ttsService;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IFramework framework,
        IChatGui chatGui,
        IClientState clientState,
        IPartyList partyList)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.framework = framework;

        this.config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        this.ttsService = new TtsService(this.config);
        this.mistakeStore = new MistakeStore(pluginInterface.ConfigDirectory);
        this.reminderService = new ReminderService(this.config, this.mistakeStore, this.ttsService);

        this.commandManager.AddHandler("/wym", new CommandInfo(this.OnCommand)
        {
            HelpMessage = "打开卫月犯错记录插件"
        });

        this.framework.Update += this.OnFrameworkUpdate;
        this.pluginInterface.UiBuilder.Draw += this.DrawUi;
        this.pluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
    }

    public void Dispose()
    {
        this.framework.Update -= this.OnFrameworkUpdate;
        this.pluginInterface.UiBuilder.Draw -= this.DrawUi;
        this.pluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
        this.commandManager.RemoveHandler("/wym");
        this.ttsService.Dispose();
    }
}
```

## 6. 数据模型

### 6.1 团队成员

```csharp
public sealed class TeamMember
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string World { get; set; } = "";
    public string Job { get; set; } = "";
    public string Alias { get; set; } = "";
}
```

建议：

- `Id` 可以用 `Name@World`
- `Alias` 用于 TTS 播报昵称，例如“MT”“贤者”“阿月”

### 6.2 犯错记录

```csharp
public sealed class MistakeRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string EncounterId { get; set; } = "";
    public string MechanicId { get; set; } = "";
    public string MemberId { get; set; } = "";
    public string MistakeType { get; set; } = "";
    public string Note { get; set; } = "";
    public int Count { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    public bool Enabled { get; set; } = true;
}
```

示例：

```json
{
  "encounterId": "p12s-phase1",
  "mechanicId": "superchain-theory-1",
  "memberId": "Alice@Chocobo",
  "mistakeType": "站位错误",
  "note": "上次分摊离人群太远",
  "count": 2
}
```

### 6.3 机制定义

```csharp
public sealed class MechanicDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double TimeSeconds { get; set; }
    public double PrewarnSeconds { get; set; } = 8;
    public bool Enabled { get; set; } = true;
}
```

### 6.4 副本时间轴

```csharp
public sealed class EncounterDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public uint TerritoryType { get; set; }
    public List<MechanicDefinition> Mechanics { get; set; } = new();
}
```

第一版可以手动维护时间轴，后续再支持导入。

## 7. 本地存储设计

建议把配置和业务数据分开：

- Dalamud 配置：保存窗口开关、TTS 设置、提醒设置
- 独立数据文件：保存犯错记录、成员别名、时间轴

推荐文件：

```text
ConfigDirectory/
  mistakes.json
  timelines.json
  members.json
```

写入策略：

- 修改后立即保存，或 1 秒防抖保存
- 保存前写临时文件，再替换正式文件，降低损坏风险
- 加载失败时保留错误日志，不覆盖原文件

`Configuration.cs` 只保存全局设置：

```csharp
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool TtsEnabled { get; set; } = true;
    public int TtsVolume { get; set; } = 80;
    public int TtsRate { get; set; } = 0;
    public double DefaultPrewarnSeconds { get; set; } = 8;
    public bool OnlyWarnEnabledRecords { get; set; } = true;
    public string ReminderTemplate { get; set; } = "{name}注意，马上是{mechanic}，上次问题：{note}";
}
```

## 8. 开战计时方案

本地模式有两个可选方案。

### 8.1 手动开始

命令：

```text
/wym pull
/wym stop
/wym reset
```

优点：

- 稳定
- 不依赖复杂战斗检测
- 适合固定队排练

缺点：

- 需要手动操作

### 8.2 自动开始

通过 `ICondition` 检测从非战斗进入战斗，配合当前 `TerritoryType` 匹配副本时间轴。

建议逻辑：

1. 检测当前区域是否存在时间轴
2. 监听 `InCombat` 状态从 `false` 变成 `true`
3. 记录 `pullStartTime`
4. 每帧计算 `elapsedSeconds`
5. 对即将到来的机制触发提醒

第一版建议同时支持：

- 自动开始：默认开启
- 手动开始：作为修正和调试工具

## 9. 提醒触发逻辑

提醒条件：

- 当前副本匹配某个 `EncounterDefinition`
- 当前战斗计时有效
- 机制开启
- 当前时间到达 `mechanic.TimeSeconds - mechanic.PrewarnSeconds`
- 该机制本轮尚未提醒过
- 该机制下存在启用的犯错记录

伪代码：

```csharp
foreach (var mechanic in encounter.Mechanics)
{
    var remindAt = mechanic.TimeSeconds - mechanic.PrewarnSeconds;

    if (elapsedSeconds >= remindAt && !notifiedMechanics.Contains(mechanic.Id))
    {
        var records = mistakeStore.GetRecords(encounter.Id, mechanic.Id)
            .Where(x => x.Enabled)
            .ToList();

        if (records.Count > 0)
        {
            reminderService.SpeakMechanicReminder(mechanic, records);
        }

        notifiedMechanics.Add(mechanic.Id);
    }
}
```

## 10. 混合模式机制识别

混合模式由两部分组成：

- **时间轴预测**：根据开战后的经过时间，提前判断机制快到了
- **事件校准**：通过 Boss 读条、阶段转换、区域变化、战斗状态变化修正当前时间轴

这样可以兼顾两个需求：

- 没有触发事件时，时间轴仍然能正常提醒
- Boss 时间轴发生漂移时，事件可以把提醒拉回正确位置

### 10.1 推荐数据结构

机制定义中增加触发器字段：

```csharp
public sealed class MechanicDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double TimeSeconds { get; set; }
    public double PrewarnSeconds { get; set; } = 8;
    public bool Enabled { get; set; } = true;
    public List<MechanicTrigger> Triggers { get; set; } = new();
}
```

触发器定义：

```csharp
public sealed class MechanicTrigger
{
    public string Type { get; set; } = "";
    public uint ActionId { get; set; }
    public string SourceName { get; set; } = "";
    public double SyncToTimeSeconds { get; set; }
    public bool FireReminderImmediately { get; set; }
}
```

`Type` 可选：

- `CastStart`：Boss 开始读条
- `CastEnd`：Boss 读条结束
- `PhaseStart`：阶段开始
- `ManualSync`：手动校准
- `CombatStart`：战斗开始

### 10.2 时间轴预测

时间轴预测仍然是主逻辑。

例如：

```json
{
  "id": "tower-1",
  "name": "第一次踩塔",
  "timeSeconds": 78,
  "prewarnSeconds": 10
}
```

开战后第 `68` 秒，插件准备播报：

```text
Alice注意，马上第一次踩塔，上次漏踩左上塔。
```

### 10.3 Boss 读条校准

如果机制前有明确 Boss 读条，可以把该读条配置成触发器。

示例：

```json
{
  "id": "tower-1",
  "name": "第一次踩塔",
  "timeSeconds": 78,
  "prewarnSeconds": 10,
  "triggers": [
    {
      "type": "CastStart",
      "actionId": 123456,
      "syncToTimeSeconds": 70,
      "fireReminderImmediately": true
    }
  ]
}
```

含义：

- 看到 Boss 开始读 `ActionId = 123456`
- 插件认为当前时间轴应校准到 `70` 秒
- 如果这个机制有关联的犯错记录，立刻播报或按剩余时间播报

### 10.4 校准策略

建议采用“温和校准”，不要每帧强行改时间：

```csharp
var diff = trigger.SyncToTimeSeconds - encounterClock.ElapsedSeconds;

if (Math.Abs(diff) >= 1.5)
{
    encounterClock.Adjust(diff);
}
```

建议规则：

- 偏差小于 `1.5` 秒，不校准
- 偏差在 `1.5` 到 `8` 秒之间，直接校准
- 偏差超过 `8` 秒，认为可能是阶段错位，提示用户确认或进入新阶段

### 10.5 阶段切换

有些副本不是单一时间轴，而是 P1、P2、门神、本体等多个阶段。建议使用阶段化时间轴：

```csharp
public sealed class EncounterPhase
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<MechanicDefinition> Mechanics { get; set; } = new();
    public List<MechanicTrigger> StartTriggers { get; set; } = new();
}
```

当检测到某个阶段开始触发器：

1. 切换当前阶段
2. 重置阶段计时
3. 清空该阶段已提醒机制
4. 继续按阶段时间轴提醒

### 10.6 事件来源

第一版建议使用低风险事件来源：

- `ICondition`：判断是否进入战斗
- `IClientState.TerritoryType`：判断当前副本区域
- `IPartyList`：读取当前队伍成员
- `IFramework.Update`：每帧更新计时与提醒
- `ObjectTable` / 战斗对象信息：扫描 Boss 是否正在读条

不建议第一版直接做复杂网络包解析。读条扫描已经能覆盖很多机制，而且开发风险更低。

### 10.7 混合模式触发优先级

建议优先级如下：

1. 手动命令，例如 `/wym sync p2`
2. 明确阶段触发器
3. Boss 读条触发器
4. 时间轴预测
5. 手动 `/wym pull`

也就是说，时间轴永远是兜底，事件负责修正。

### 10.8 防重复提醒

混合模式必须防重复。否则同一个机制可能被时间轴提醒一次，又被读条触发提醒一次。

建议使用提醒 key：

```csharp
var reminderKey = $"{encounterId}:{phaseId}:{mechanicId}:{pullId}";
```

同一轮战斗中，同一个 `reminderKey` 只能播报一次。

如果用户手动重置或进入新阶段，再清空对应缓存。

## 11. TTS 设计

本地模式优先使用 Windows 本机 TTS。

推荐实现：

- Windows：`System.Speech.Synthesis.SpeechSynthesizer`
- 非 Windows 或 TTS 初始化失败：回退到聊天窗口提示

TTS 服务应支持：

- 开关
- 音量
- 语速
- 队列
- 防重复
- 打断当前播报
- 测试播报

示例：

```csharp
public sealed class TtsService : IDisposable
{
    private readonly SpeechSynthesizer synthesizer = new();
    private readonly Configuration config;

    public TtsService(Configuration config)
    {
        this.config = config;
        this.synthesizer.Volume = Math.Clamp(config.TtsVolume, 0, 100);
        this.synthesizer.Rate = Math.Clamp(config.TtsRate, -10, 10);
    }

    public void Speak(string text)
    {
        if (!this.config.TtsEnabled || string.IsNullOrWhiteSpace(text))
            return;

        this.synthesizer.SpeakAsyncCancelAll();
        this.synthesizer.SpeakAsync(text);
    }

    public void Dispose()
    {
        this.synthesizer.Dispose();
    }
}
```

注意：

- `System.Speech` 是 Windows 语音 API，发布前要确认目标框架和依赖打包
- 播报应尽量短，避免机制期间声音过长
- 同一机制多条记录应合并播报，避免 TTS 队列爆炸

## 12. 播报内容生成

建议提供模板：

```text
{name}注意，马上是{mechanic}，上次问题：{note}
```

可用变量：

- `{name}`：成员昵称或角色名
- `{job}`：职业
- `{mechanic}`：机制名
- `{type}`：错误类型
- `{note}`：备注
- `{count}`：犯错次数

多条记录合并策略：

```text
马上是超级链理论一。Alice注意分摊别出人群；Bob注意踩塔。
```

建议限制：

- 单次播报不超过 80 个中文字符
- 同一成员同一机制只播报最高优先级或最近一次
- 可配置“只提醒次数大于等于 N 的记录”

## 13. UI 设计

主窗口建议分为四个标签页。

### 13.1 今日队伍

功能：

- 从当前小队读取成员
- 给成员设置昵称
- 手动添加不在队伍中的固定队成员
- 标记常用职业

### 13.2 犯错记录

功能：

- 选择副本
- 选择机制
- 选择成员
- 选择错误类型
- 填写备注
- 增加次数
- 启用或禁用记录

建议快捷按钮：

- “+1”
- “本机制记录”
- “只看当前队伍”
- “隐藏已禁用”

### 13.3 时间轴

功能：

- 当前副本时间轴列表
- 新增机制
- 编辑机制时间
- 设置提前提醒秒数
- 启用或禁用机制
- 测试播报

### 13.4 设置

功能：

- TTS 开关
- 音量
- 语速
- 默认提前秒数
- 播报模板
- 自动开战检测开关
- 导入/导出 JSON

## 14. 命令设计

建议命令：

```text
/wym
/wym open
/wym add
/wym pull
/wym stop
/wym test
/wym export
/wym import
```

示例：

```text
/wym add Alice 超级链理论一 站位错误 上次分摊出人群
/wym pull
/wym test Alice 超级链理论一
```

命令解析建议保持简单。复杂操作交给 UI。

## 15. 机制时间轴示例

示例数据：

```json
{
  "id": "example-raid",
  "name": "示例副本",
  "territoryType": 0,
  "mechanics": [
    {
      "id": "stack-1",
      "name": "第一次分摊",
      "timeSeconds": 45,
      "prewarnSeconds": 8,
      "enabled": true
    },
    {
      "id": "tower-1",
      "name": "第一次踩塔",
      "timeSeconds": 78,
      "prewarnSeconds": 10,
      "enabled": true
    }
  ]
}
```

第一版不需要内置所有副本。可以先允许用户手动维护时间轴，等插件稳定后再补常用副本模板。

## 16. 服务职责划分

### 16.1 MistakeStore

负责：

- 加载犯错记录
- 新增、编辑、删除记录
- 按副本、机制、成员筛选
- 保存 JSON

不负责：

- UI 绘制
- TTS 播报
- 战斗计时

### 16.2 TimelineService

负责：

- 加载副本时间轴
- 根据 `TerritoryType` 匹配副本
- 查询即将到来的机制
- 编辑时间轴

### 16.3 EncounterClock

负责：

- 开战时间
- 当前战斗经过秒数
- 重置提醒状态
- 手动和自动开始

### 16.4 ReminderService

负责：

- 判断某机制是否需要提醒
- 聚合同机制下的记录
- 生成播报文本
- 调用 TTS

### 16.5 TtsService

负责：

- 初始化 TTS
- 设置音量、语速
- 播报、停止、测试
- 异常回退

## 17. 错误类型建议

内置错误类型可以先做这些：

- 站位错误
- 分摊错误
- 分散错误
- 漏踩塔
- 多踩塔
- 吃错球
- 引导错误
- 面向错误
- 减伤遗漏
- 奶轴遗漏
- 过早移动
- 过晚移动
- 死亡
- 自定义

不同固定队可以自行改名。

## 18. 迭代路线

### V0.1 原型

- `/wym` 打开窗口
- 手动维护成员
- 手动维护机制
- 手动新增犯错记录
- 手动 `/wym pull`
- 机制前 TTS 播报

### V0.2 本地体验完善

- 自动读取当前小队
- 自动战斗开始检测
- 当前副本自动匹配时间轴
- 导入/导出 JSON
- 播报模板编辑

### V0.3 可用性增强

- 当前机制快捷记录
- 最近战斗记录
- 同成员同机制合并
- 统计次数
- 按当前队伍过滤

### V0.4 半自动识别

- 监听可见战斗事件
- 记录死亡时间点
- 将死亡关联到最近机制
- 提供“确认是否记录”的 UI

## 19. 风险与注意事项

- 时间轴偏移：Boss 转阶段、停手、动画锁可能导致时间轴不准，需要支持手动校准
- 播报过多：应合并提醒，并设置最小间隔
- 同名角色：成员 ID 建议使用 `Name@World`
- TTS 依赖：Windows 语音包缺失时要降级到聊天提示
- 数据损坏：保存 JSON 时建议使用临时文件替换
- 官方审核：涉及战斗提醒时需要严格遵守 Dalamud 插件限制

## 20. 推荐开发顺序

1. 搭 Dalamud 插件模板
2. 做 `Configuration`
3. 做主窗口和 `/wym` 命令
4. 做 `MistakeStore`
5. 做 `TimelineService`
6. 做 `EncounterClock`
7. 做 `TtsService`
8. 做 `ReminderService`
9. 接入当前小队读取
10. 增加导入/导出和测试播报

## 21. 参考资料

- Dalamud 官方文档：https://dalamud.dev/
- Dalamud 插件开发入门：https://dalamud.dev/plugin-development/getting-started/
- Dalamud 项目结构说明：https://dalamud.dev/plugin-development/project-layout
- Dalamud 插件发布说明：https://dalamud.dev/plugin-publishing/
- Dalamud 插件限制：https://dalamud.dev/plugin-publishing/restrictions
- `IDalamudPluginInterface` API：https://dalamud.dev/api/Dalamud.Plugin/Interfaces/IDalamudPluginInterface
- `ICommandManager` API：https://dalamud.dev/api/Dalamud.Plugin.Services/Interfaces/ICommandManager/
- `IUiBuilder` API：https://dalamud.dev/api/Dalamud.Interface/Interfaces/IUiBuilder/
- `System.Speech.Synthesis` API：https://learn.microsoft.com/en-us/dotnet/api/system.speech.synthesis
- `System.Speech` NuGet：https://www.nuget.org/packages/System.Speech
