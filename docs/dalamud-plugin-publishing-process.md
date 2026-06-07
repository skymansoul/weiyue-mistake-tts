# Dalamud 插件发布流程

> 适用对象：准备发布 `Weiyue Mistake TTS` 或其他 Dalamud 插件的开发者。
> 依据：Dalamud 官方文档当前 API 15 页面。发布前请再次核对官方文档。

## 1. 发布路径选择

Dalamud 插件通常有两种发布路径：

1. **官方插件仓库**
   - 面向普通用户
   - 需要开源
   - 需要提交到 `goatcorp/DalamudPluginsD17`
   - 需要经过插件审核团队审查
   - 新插件需要先走 testing track

2. **自定义插件仓库**
   - 适合内测、小范围分发、自己控制发布节奏
   - 官方支持较少
   - 用户需要手动添加仓库 URL
   - 不代表通过官方审核

如果目标是“真正公开上线”，优先考虑官方仓库。如果只是固定队或个人使用，自定义仓库更快。

## 2. 发布前准备

提交前至少确认：

- 插件可以 Release 构建
- `WeiyueMistakeTTS.json` 元数据完整
- `InternalName` 稳定，后续不要随意改
- `AssemblyVersion` 已递增
- 插件图标准备好
- 本地实机测试通过
- 没有崩溃、异常刷屏、无法卸载的问题
- 没有违反插件限制
- 如果使用了 AI 协助，PR 说明中按官方政策披露

当前项目本地构建命令：

```powershell
dotnet build -c Release -p:DalamudLibPath="C:\Users\lenovo\AppData\Roaming\XIVLauncherCN\addon\Hooks\26-06-03-01\"
```

构建产物：

```text
E:\win_c\Desktop\weiyue\bin\Release\WeiyueMistakeTTS\latest.zip
```

## 3. 官方仓库发布流程

### 3.1 准备公开源码仓库

官方仓库要求插件是开源的。你需要：

- 将当前项目放到 GitHub 公开仓库
- 确保源码能从指定 commit 构建
- 不要依赖构建时下载额外私有代码
- 不要提交 `bin/`、`obj/` 等构建产物

建议仓库内容：

```text
WeiyueMistakeTTS/
  Plugin.cs
  Configuration.cs
  Models/
  Services/
  Windows/
  WeiyueMistakeTTS.csproj
  WeiyueMistakeTTS.json
  README.md
```

### 3.2 准备插件图标

官方提交目录需要 `images/icon.png`。

要求：

- 1:1 比例
- 尺寸在 64x64 到 512x512 之间
- 建议使用手工制作或清晰简单的图标

可选预览图：

```text
images/image1.png
images/image2.png
images/image3.png
images/image4.png
images/image5.png
```

### 3.3 Fork D17 仓库

Fork 官方插件仓库：

```text
https://github.com/goatcorp/DalamudPluginsD17
```

新插件必须提交到 testing track：

```text
testing/live/
```

### 3.4 创建插件提交目录

目录结构：

```text
testing/live/WeiyueMistakeTTS/
  manifest.toml
  images/
    icon.png
```

`manifest.toml` 示例：

```toml
[plugin]
repository = "https://github.com/你的用户名/WeiyueMistakeTTS.git"
commit = "这里填写要提交审核的 commit hash"
owners = ["你的 GitHub 用户名"]
project_path = "."
changelog = "Initial testing release."
```

如果项目在仓库子目录，`project_path` 改成对应子目录。

### 3.5 打开 Pull Request

PR 要点：

- 一个 PR 只提交一个插件
- 一个插件提交使用一个独立分支
- PR 说明中写清楚插件功能、数据读取范围、是否涉及战斗内容
- 如果使用 AI 协助，按官方 AI 政策说明使用程度
- 对这个插件，需要主动说明：
  - 只本地存储
  - 不上传数据
  - 不读取账号 ID
  - 不自动操作角色
  - 不向服务器发送自动化请求
  - TTS 只基于用户手动记录和本地时间轴提醒

## 4. 审核流程

官方审核大致包含：

- 确认插件开源
- 根据提交的 commit hash 构建
- 云构建系统生成代码 diff
- 插件审核团队审查代码
- 新插件需要审核团队投票
- 新插件通常先进入 testing track

新插件审核可能排队超过一周，这很正常。

审核重点通常包括：

- 插件是否符合限制
- 是否能正常工作
- 是否上传个人数据
- 是否有危险行为
- 是否存在安全问题
- 是否有不合规的战斗辅助

## 5. 插件更新流程

更新官方仓库插件时：

1. 在源码仓库提交新代码
2. 确认 `AssemblyVersion` 递增
3. 在 D17 新建分支
4. 修改 `manifest.toml` 中的 `commit`
5. 可选更新 `changelog`
6. 打开新的 PR

官方文档说明，更新已有插件通常只需要一名审核成员批准。

## 6. 从 Testing 转 Stable

新插件先放在：

```text
testing/live/
```

稳定后可以移动到：

```text
stable/
```

切换 track 时通常复制或移动插件 manifest 目录即可，不一定需要版本号变化。但实际提交前仍应检查 D17 仓库 README 和当时规则。

## 7. 自定义仓库发布流程

如果只给固定队或测试用户使用，可以使用自定义仓库。

自定义仓库本质是一个可 HTTP GET 访问的 JSON 文件，内容是插件 store entry 数组。

当前项目已有：

```text
E:\win_c\Desktop\weiyue\repo.json
```

当前构建包：

```text
E:\win_c\Desktop\weiyue\bin\Release\WeiyueMistakeTTS\latest.zip
```

正式自定义仓库需要：

- 把 `latest.zip` 上传到可公开访问的 HTTP 地址
- 把 `repo.json` 上传到可公开访问的 HTTP 地址
- 在 `repo.json` 中配置：
  - `DownloadLinkInstall`
  - `DownloadLinkUpdate`
  - `DownloadLinkTesting`
  - `DalamudApiLevel`
  - `AssemblyVersion`
  - `Punchline`

注意：官方文档说明自定义仓库 URL 需要能通过 HTTP GET 访问，并且不支持认证。

## 8. Weiyue Mistake TTS 发布前检查

这个插件涉及机制前提醒，属于审核敏感区域。提交官方仓库前建议先做以下调整：

- 将 README 明确写成“本地记忆提醒工具”
- 删除或弱化任何“自动判断犯错”“自动解机制”的描述
- 保留手动记录为主
- 说明 Boss 读条只用于校准用户自定义时间轴
- 不内置争议性副本 callout
- 不接入 FFLogs、DPS、伤害统计、ACT 解析
- 不采集 Content ID、Account ID
- 不自动发送聊天消息
- 不自动改变战斗操作

## 9. 官方资料

- Publishing Your Plugin：https://dalamud.dev/plugin-publishing/
- The Submission Process：https://dalamud.dev/plugin-publishing/submission/
- The Approval Process：https://dalamud.dev/plugin-publishing/approval-process/
- Publishing to a Custom Repository：https://dalamud.dev/plugin-publishing/custom-repositories/
- AI Usage Policy：https://dalamud.dev/plugin-publishing/ai-policy/
- Plugin Restrictions：https://dalamud.dev/plugin-publishing/restrictions/

