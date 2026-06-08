# Team Mistake

FFXIV / Dalamud 本地犯错记录与机制前 TTS 提醒插件。

## 功能

- 本地记录团队成员在指定机制中的失误
- 本地 JSON 存储，不做联网同步
- 支持手动 `/wym pull` 开始时间轴
- 支持自动进入战斗后开始计时
- 支持时间轴提前提醒
- 支持读取当前副本名
- 支持用当前副本一键创建时间轴分组
- 支持进入副本战斗后自动创建/选择当前副本时间轴并开始计时
- 支持单人/解限时导入本机玩家
- 支持将当前战斗秒数一键记录为机制时间点
- 支持自动学习时间轴：进入战斗后自动记录 Boss/NPC 读条技能名、出现秒数和 ActionId
- 支持自动学习无读条技能：从战斗日志/伤害结算消息中记录 Boss/NPC 技能名和出现秒数
- 支持 Boss 读条 `ActionId` 触发器校准时间轴
- 支持 Windows TTS 播报，失败时回退到聊天窗口提示
- 支持多个队伍分组
- 支持一键导入当前小队到指定分组
- 支持队伍分组 JSON 导入 / 导出

## 命令

```text
/wym       打开主窗口
/wym pull  手动开始计时
/wym stop  停止计时
/wym test  测试 TTS
/wym sync 70  把当前时间轴校准到 70 秒
```

## 开发环境

需要安装：

- .NET 10 SDK
- XIVLauncher / Dalamud 开发环境
- Dalamud API 15 对应的开发库

默认情况下，`Dalamud.NET.Sdk` 会从下面路径查找开发库：

```text
%AppData%\XIVLauncher\addon\Hooks\dev\
```

如果你的 Dalamud 开发库在其他路径，可以设置环境变量：

```powershell
$env:DALAMUD_HOME = "D:\Path\To\Dalamud\Hooks\dev"
dotnet build
```

## 构建

```powershell
dotnet build -c Release
```

构建成功后，DalamudPackager 会在输出目录生成可供本地仓库使用的包。

当前构建包位置：

```text
E:\win_c\Desktop\weiyue\bin\Release\WeiyueMistakeTTS\latest.zip
```

## 本地安装

方式一：开发插件加载

1. 打开 Dalamud 设置
2. 开启实验性功能里的开发插件加载
3. 添加插件输出目录：

```text
E:\win_c\Desktop\weiyue\bin\Release
```

方式二：本地自定义仓库

仓库清单在：

```text
E:\win_c\Desktop\weiyue\repo.json
```

如果 Dalamud 不接受 `file:///` 本地链接，可以在这个目录启动一个本地静态服务器，然后把 `repo.json` 里的 `DownloadLinkInstall`、`DownloadLinkUpdate`、`DownloadLinkTesting` 改成 HTTP 地址。

## 使用建议

第一版建议这样使用：

1. 进入副本后打开 `/wym`
2. 在“成员”里添加固定队成员，或等待插件读取当前小队
3. 在“队伍分组”里创建分组，并一键导入当前小队
4. 在“时间轴”里配置机制出现时间和提前提醒秒数
5. 在“犯错记录”里记录成员、机制、错误类型和备注
6. 开战前使用 `/wym pull`，或开启自动进入战斗检测
7. 开启“自动学习时间轴”后打一把副本，插件会自动把 Boss/NPC 读条和战斗日志技能记录进时间轴
8. 到机制前，插件自动 TTS 播报本地记录

## 注意

这个插件不会自动操作角色，不会自动按技能，不会向队伍频道发送信息。它只做本地记录和本地提醒。
