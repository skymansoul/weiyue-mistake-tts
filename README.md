# Team Mistake

Team Mistake 是一个 FFXIV / Dalamud 插件，用于本地记录团队成员在副本机制中的失误，并在下一次机制到来前通过 TTS 提醒。

它适合固定队、开荒队和复盘场景：先让插件自动学习副本时间轴，再把每个人容易犯错的机制记录下来，后续进入同一副本时自动提醒。

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
- 本地 JSON 存储，不上传数据
- 多队伍分组管理
- 一键导入当前小队
- 单人 / 解限时自动导入本机玩家
- 当前副本自动创建时间轴分组
- 进入战斗后自动开始计时
- 自动学习 Boss / NPC 读条时间轴
- 自动学习无读条技能的战斗日志时间点
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
3. 开启“自动检测进入战斗”和“自动学习时间轴”
4. 打一把副本，让插件自动记录读条和战斗日志技能
5. 在“时间轴”中整理机制名称和提醒时间
6. 在“犯错记录”中记录成员、机制、错误类型和备注
7. 下次同机制到来前，插件会自动 TTS 提醒

## 注意事项

- 插件只做本地记录和本地提醒
- 不自动操作角色
- 不自动按技能
- 不自动移动
- 不向队伍频道发送消息
- 自动学习的时间轴可能包含普通技能，需要用户手动删减整理

