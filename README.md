# Team Mistake

Team Mistake 是一个 FFXIV / Dalamud 插件，用于本地记录团队成员在副本机制中的失误，并在下一次机制到来前通过 TTS 提醒。

它适合固定队、开荒队和复盘场景：可以自动学习副本时间轴，也可以从 FFLogs 过本记录导入时间轴，再把每个人容易犯错的机制记录下来，后续进入同一副本时自动提醒。

## 插件仓库

在 Dalamud 的自定义插件仓库中添加：

```text
https://raw.githubusercontent.com/skymansoul/dalamud-plugin-repo/main/custom-repo/repo.json
```

插件名：

```text
Team Mistake
```

GitHub 仓库：

```text
https://github.com/skymansoul/dalamud-plugin-repo
```

## 主要功能

- 本地记录团队成员机制失误
- 本地 JSON 存储，不上传团队数据
- 多队伍分组管理
- 一键导入当前小队
- 单人 / 解限时自动导入本机玩家
- 当前副本自动创建时间轴分组
- 进入战斗后自动开始计时
- 自动学习 Boss / NPC 读条时间轴
- 自动学习无读条技能的战斗日志时间点
- 从 FFLogs 报告导入读条和伤害判定事件时间轴
- 手动记录当前战斗秒数为机制
- Boss 读条 `ActionId` 校准时间轴
- 机制前 TTS 播报提醒
- TTS 失败时回退到聊天窗口提示
- 队伍分组 JSON 导入 / 导出

## 使用方式

打开插件：

```text
/wym
```

常用命令：

```text
/wym pull     手动开始计时
/wym stop     停止计时
/wym test     测试 TTS
/wym sync 70  将当前时间轴校准到 70 秒
```

推荐流程：

1. 进入副本后打开 `/wym`
2. 在“队伍分组”中一键导入当前小队
3. 在“时间轴”中使用当前副本创建时间轴分组
4. 选择自动学习，或粘贴完整 FFLogs 战斗链接导入时间轴
5. 整理机制名称、删除不需要提醒的普通技能
6. 在“犯错记录”中记录成员、机制、错误类型和备注
7. 下一次同机制到来前，插件会自动 TTS 提醒

## FFLogs 导入

在“时间轴”页的“FFLogs 时间轴导入”区域粘贴完整战斗链接：

```text
https://www.fflogs.com/reports/7WcX6qxDVJGMvNPk?fight=11&type=damage-done
```

插件会自动解析报告 code 和 `fight` 参数，不需要用户单独填写 fight id，也不需要用户输入 FFLogs Client ID 或 Client Secret。

首次使用 FFLogs 导入前，先复制 FFLogs Access Token，然后点击“导入/更新 FFLogs Token”。插件会把 Token 保存在本地配置中，后续导入会自动复用；如果 Token 失效，插件会清除旧 Token 并提示重新导入。

导入内容只包含敌方事件的技能名、技能 ID 和相对战斗时间，用于生成本地机制时间轴。插件不会导入 DPS、排名、死亡归因或个人表现数据。

## 注意事项

- 插件只做本地记录和本地提醒
- 不自动操作角色
- 不自动按技能
- 不自动移动
- 不向队伍频道发送消息
- FFLogs 官方 API 需要访问令牌；当前界面不会向普通用户暴露 Client ID / Client Secret 输入
- 自动学习和 FFLogs 导入的时间轴可能包含普通技能，需要用户手动删减整理
