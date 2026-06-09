using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using WeiyueMistakeTTS.Models;
using WeiyueMistakeTTS.Services;

namespace WeiyueMistakeTTS.Windows;

public sealed class MainWindow : Window
{
    private static readonly string[] MistakeTypes =
    [
        "站位错误",
        "分摊错误",
        "分散错误",
        "漏踩塔",
        "多踩塔",
        "吃错球",
        "引导错误",
        "面向错误",
        "减伤遗漏",
        "奶轴遗漏",
        "过早移动",
        "过晚移动",
        "死亡",
        "自定义",
    ];

    private readonly Configuration config;
    private readonly DataStore dataStore;
    private readonly TimelineService timelineService;
    private readonly EncounterClock clock;
    private readonly TtsService ttsService;
    private readonly PartyService partyService;
    private readonly FflogsImportService fflogsImportService;
    private readonly IClientState clientState;
    private readonly Action saveConfig;

    private string newMemberName = string.Empty;
    private string newMemberWorld = string.Empty;
    private string newMemberJob = string.Empty;
    private string newMemberAlias = string.Empty;
    private string selectedGroupId = "default-group";
    private string newGroupName = string.Empty;
    private string groupImportExportText = string.Empty;
    private string groupImportStatus = string.Empty;

    private string selectedEncounterId = "default";
    private string selectedMechanicId = "stack-1";
    private string selectedMemberId = string.Empty;
    private string selectedMistakeType = MistakeTypes[0];
    private string mistakeNote = string.Empty;

    private string newEncounterName = string.Empty;
    private int newEncounterTerritory;
    private string newMechanicName = string.Empty;
    private float newMechanicTime = 60;
    private float newMechanicPrewarn = 8;
    private int newTriggerActionId;
    private float newTriggerSyncTime = 0;
    private string timelineRecordStatus = string.Empty;
    private string fflogsReportInput = string.Empty;
    private string fflogsImportStatus = string.Empty;
    private Task? fflogsImportTask;
    private Task? fflogsTokenTask;

    public MainWindow(
        Configuration config,
        DataStore dataStore,
        TimelineService timelineService,
        EncounterClock clock,
        TtsService ttsService,
        PartyService partyService,
        FflogsImportService fflogsImportService,
        IClientState clientState,
        Action saveConfig)
        : base("Team Mistake###WeiyueMistakeTTS")
    {
        this.config = config;
        this.dataStore = dataStore;
        this.timelineService = timelineService;
        this.clock = clock;
        this.ttsService = ttsService;
        this.partyService = partyService;
        this.fflogsImportService = fflogsImportService;
        this.clientState = clientState;
        this.saveConfig = saveConfig;

        this.Size = new Vector2(880, 620);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.selectedEncounterId = this.dataStore.Data.Encounters.FirstOrDefault()?.Id ?? string.Empty;
        this.selectedMechanicId = this.SelectedEncounter?.Mechanics.FirstOrDefault()?.Id ?? string.Empty;
        this.selectedGroupId = this.dataStore.Data.Groups.FirstOrDefault()?.Id ?? string.Empty;
    }

    private EncounterDefinition? SelectedEncounter =>
        this.dataStore.Data.Encounters.FirstOrDefault(x => x.Id == this.selectedEncounterId)
        ?? this.dataStore.Data.Encounters.FirstOrDefault();

    private MechanicDefinition? SelectedMechanic =>
        this.SelectedEncounter?.Mechanics.FirstOrDefault(x => x.Id == this.selectedMechanicId)
        ?? this.SelectedEncounter?.Mechanics.FirstOrDefault();

    private TeamGroup? SelectedGroup =>
        this.dataStore.Data.Groups.FirstOrDefault(x => x.Id == this.selectedGroupId)
        ?? this.dataStore.Data.Groups.FirstOrDefault();

    public override void Draw()
    {
        ImGui.TextUnformatted($"状态：{(this.clock.IsRunning ? $"战斗计时 {this.clock.ElapsedSeconds:F1}s" : "未计时")}");
        ImGui.SameLine();
        if (ImGui.Button("开始 / Pull"))
            this.clock.Start();
        ImGui.SameLine();
        if (ImGui.Button("停止"))
            this.clock.Stop();
        ImGui.SameLine();
        if (ImGui.Button("测试 TTS"))
            this.ttsService.Speak("Team Mistake 测试");

        if (ImGui.BeginTabBar("main-tabs"))
        {
            if (ImGui.BeginTabItem("犯错记录"))
            {
                this.DrawMistakesTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("成员"))
            {
                this.DrawMembersTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("队伍分组"))
            {
                this.DrawGroupsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("时间轴"))
            {
                this.DrawTimelineTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("设置"))
            {
                this.DrawSettingsTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawMistakesTab()
    {
        this.DrawEncounterCombo();
        this.DrawMechanicCombo();
        this.DrawMemberCombo();

        DrawStringCombo("错误类型", ref this.selectedMistakeType, MistakeTypes);
        ImGui.InputTextMultiline("备注", ref this.mistakeNote, 512, new Vector2(-1, 70));

        if (ImGui.Button("新增记录") && this.SelectedEncounter != null && this.SelectedMechanic != null && !string.IsNullOrWhiteSpace(this.selectedMemberId))
        {
            this.dataStore.Data.Mistakes.Add(new MistakeRecord
            {
                EncounterId = this.SelectedEncounter.Id,
                MechanicId = this.SelectedMechanic.Id,
                MemberId = this.selectedMemberId,
                MistakeType = this.selectedMistakeType,
                Note = this.mistakeNote.Trim(),
                Count = 1,
            });
            this.mistakeNote = string.Empty;
            this.dataStore.Save();
        }

        ImGui.Separator();

        if (ImGui.BeginTable("mistakes-table", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("启用");
            ImGui.TableSetupColumn("成员");
            ImGui.TableSetupColumn("副本");
            ImGui.TableSetupColumn("机制");
            ImGui.TableSetupColumn("类型");
            ImGui.TableSetupColumn("备注");
            ImGui.TableSetupColumn("操作");
            ImGui.TableHeadersRow();

            foreach (var record in this.dataStore.Data.Mistakes.ToList())
            {
                var encounter = this.dataStore.Data.Encounters.FirstOrDefault(x => x.Id == record.EncounterId);
                var mechanic = encounter?.Mechanics.FirstOrDefault(x => x.Id == record.MechanicId);
                var member = this.dataStore.Data.Members.FirstOrDefault(x => x.Id == record.MemberId);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var enabled = record.Enabled;
                if (ImGui.Checkbox($"##enabled-{record.Id}", ref enabled))
                {
                    record.Enabled = enabled;
                    record.UpdatedAt = DateTimeOffset.Now;
                    this.dataStore.Save();
                }

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(member?.DisplayName ?? record.MemberId);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(encounter?.Name ?? record.EncounterId);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(mechanic?.Name ?? record.MechanicId);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{record.MistakeType} x{record.Count}");
                ImGui.TableNextColumn();
                ImGui.TextWrapped(record.Note);
                ImGui.TableNextColumn();
                if (ImGui.SmallButton($"+1##{record.Id}"))
                {
                    record.Count++;
                    record.UpdatedAt = DateTimeOffset.Now;
                    this.dataStore.Save();
                }

                ImGui.SameLine();
                if (ImGui.SmallButton($"删##{record.Id}"))
                {
                    this.dataStore.Data.Mistakes.Remove(record);
                    this.dataStore.Save();
                }
            }

            ImGui.EndTable();
        }
    }

    private void DrawGroupsTab()
    {
        this.DrawGroupCombo();

        ImGui.InputText("新分组名称", ref this.newGroupName, 64);
        if (ImGui.Button("创建分组") && !string.IsNullOrWhiteSpace(this.newGroupName))
        {
            var group = this.dataStore.GetOrCreateGroup(this.newGroupName);
            this.selectedGroupId = group.Id;
            this.newGroupName = string.Empty;
            this.dataStore.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("一键导入当前小队") && this.SelectedGroup != null)
        {
            var count = this.partyService.ImportCurrentPartyToGroup(this.SelectedGroup);
            this.groupImportStatus = $"已导入 {count} 名成员到 {this.SelectedGroup.Name}";
        }

        ImGui.SameLine();
        if (ImGui.Button("删除当前分组") && this.SelectedGroup != null && this.dataStore.Data.Groups.Count > 1)
        {
            var group = this.SelectedGroup;
            this.dataStore.Data.Groups.Remove(group);
            this.selectedGroupId = this.dataStore.Data.Groups.FirstOrDefault()?.Id ?? string.Empty;
            this.dataStore.Save();
        }

        if (!string.IsNullOrWhiteSpace(this.groupImportStatus))
            ImGui.TextUnformatted(this.groupImportStatus);

        ImGui.Separator();

        var selectedGroup = this.SelectedGroup;
        if (selectedGroup == null)
            return;

        if (ImGui.BeginTable("group-members-table", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("角色");
            ImGui.TableSetupColumn("服务器");
            ImGui.TableSetupColumn("职业");
            ImGui.TableSetupColumn("昵称");
            ImGui.TableSetupColumn("操作");
            ImGui.TableHeadersRow();

            foreach (var memberId in selectedGroup.MemberIds.ToList())
            {
                var member = this.dataStore.Data.Members.FirstOrDefault(x => x.Id == memberId);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(member?.Name ?? memberId);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(member?.World ?? string.Empty);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(member?.Job ?? string.Empty);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(member?.Alias ?? string.Empty);
                ImGui.TableNextColumn();
                if (ImGui.SmallButton($"移除##group-member-{memberId}"))
                {
                    selectedGroup.MemberIds.Remove(memberId);
                    selectedGroup.UpdatedAt = DateTimeOffset.Now;
                    this.dataStore.Save();
                }
            }

            ImGui.EndTable();
        }

        ImGui.Separator();
        ImGui.TextUnformatted("分组配置导入 / 导出");
        if (ImGui.Button("生成当前分组 JSON"))
            this.groupImportExportText = this.ExportGroup(selectedGroup);

        ImGui.SameLine();
        if (ImGui.Button("复制 JSON"))
            ImGui.SetClipboardText(this.groupImportExportText);

        ImGui.SameLine();
        if (ImGui.Button("从下方 JSON 导入"))
            this.ImportGroup(this.groupImportExportText);

        ImGui.InputTextMultiline("分组 JSON", ref this.groupImportExportText, 8192, new Vector2(-1, 130));
    }

    private void DrawMembersTab()
    {
        ImGui.InputText("角色名", ref this.newMemberName, 64);
        ImGui.InputText("服务器", ref this.newMemberWorld, 64);
        ImGui.InputText("职业", ref this.newMemberJob, 32);
        ImGui.InputText("播报昵称", ref this.newMemberAlias, 64);

        if (ImGui.Button("添加成员") && !string.IsNullOrWhiteSpace(this.newMemberName))
        {
            var member = this.dataStore.GetOrCreateMember(this.newMemberName, this.newMemberWorld, this.newMemberJob);
            member.Alias = this.newMemberAlias.Trim();
            this.selectedMemberId = member.Id;
            this.newMemberName = string.Empty;
            this.newMemberWorld = string.Empty;
            this.newMemberJob = string.Empty;
            this.newMemberAlias = string.Empty;
            this.dataStore.Save();
        }

        ImGui.Separator();

        if (ImGui.BeginTable("members-table", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("角色");
            ImGui.TableSetupColumn("服务器");
            ImGui.TableSetupColumn("职业");
            ImGui.TableSetupColumn("昵称");
            ImGui.TableSetupColumn("操作");
            ImGui.TableHeadersRow();

            foreach (var member in this.dataStore.Data.Members.ToList())
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(member.Name);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(member.World);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(member.Job);
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var alias = member.Alias;
                if (ImGui.InputText($"##alias-{member.Id}", ref alias, 64))
                {
                    member.Alias = alias;
                    this.dataStore.Save();
                }

                ImGui.TableNextColumn();
                if (ImGui.SmallButton($"删除##member-{member.Id}"))
                {
                    this.dataStore.Data.Members.Remove(member);
                    this.dataStore.Save();
                }
            }

            ImGui.EndTable();
        }
    }

    private void DrawTimelineTab()
    {
        this.DrawEncounterCombo();
        var currentTerritory = (int)this.clientState.TerritoryType;
        var currentTerritoryName = this.timelineService.GetCurrentTerritoryName(this.clientState.TerritoryType);
        ImGui.TextUnformatted($"当前副本：{currentTerritoryName} ({currentTerritory})");

        if (ImGui.Button("用当前副本创建时间轴分组"))
        {
            var encounter = this.timelineService.GetOrCreateEncounterForTerritory(this.clientState.TerritoryType);
            this.selectedEncounterId = encounter.Id;
            this.selectedMechanicId = encounter.Mechanics.FirstOrDefault()?.Id ?? string.Empty;
            this.newEncounterName = string.Empty;
            this.newEncounterTerritory = 0;
        }

        ImGui.InputText("新副本名称", ref this.newEncounterName, 128);
        ImGui.InputInt("TerritoryType，0 表示通用", ref this.newEncounterTerritory);
        if (ImGui.Button("新增副本") && !string.IsNullOrWhiteSpace(this.newEncounterName))
        {
            var encounter = new EncounterDefinition
            {
                Name = this.newEncounterName.Trim(),
                TerritoryType = (uint)Math.Max(0, this.newEncounterTerritory),
            };
            this.dataStore.Data.Encounters.Add(encounter);
            this.selectedEncounterId = encounter.Id;
            this.timelineService.SelectedEncounterId = encounter.Id;
            this.newEncounterName = string.Empty;
            this.dataStore.Save();
        }

        ImGui.Separator();

        ImGui.InputText("新机制名称", ref this.newMechanicName, 128);
        ImGui.InputFloat("出现时间秒", ref this.newMechanicTime);
        ImGui.InputFloat("提前提醒秒", ref this.newMechanicPrewarn);
        if (ImGui.Button("新增机制") && this.SelectedEncounter != null && !string.IsNullOrWhiteSpace(this.newMechanicName))
        {
            var mechanic = new MechanicDefinition
            {
                Name = this.newMechanicName.Trim(),
                TimeSeconds = Math.Max(0, this.newMechanicTime),
                PrewarnSeconds = Math.Max(0, this.newMechanicPrewarn),
            };
            this.SelectedEncounter.Mechanics.Add(mechanic);
            this.selectedMechanicId = mechanic.Id;
            this.newMechanicName = string.Empty;
            this.dataStore.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("记录当前战斗时间为机制") && this.SelectedEncounter != null)
        {
            var elapsed = this.clock.IsRunning ? this.clock.ElapsedSeconds : Math.Max(0, this.newMechanicTime);
            var mechanic = this.timelineService.AddMechanicAtCurrentTime(
                this.SelectedEncounter,
                this.newMechanicName,
                elapsed,
                this.newMechanicPrewarn);
            this.selectedMechanicId = mechanic.Id;
            this.newMechanicName = string.Empty;
            this.newMechanicTime = (float)Math.Round(elapsed, 1);
            this.timelineRecordStatus = $"已记录：{mechanic.Name} @ {mechanic.TimeSeconds:F1}s";
        }

        if (!string.IsNullOrWhiteSpace(this.timelineRecordStatus))
            ImGui.TextUnformatted(this.timelineRecordStatus);

        this.DrawMechanicCombo();
        ImGui.InputInt("读条 ActionId", ref this.newTriggerActionId);
        ImGui.InputFloat("校准到时间秒", ref this.newTriggerSyncTime);
        if (ImGui.Button("给当前机制添加 CastStart 触发器") && this.SelectedMechanic != null && this.newTriggerActionId > 0)
        {
            this.SelectedMechanic.Triggers.Add(new MechanicTrigger
            {
                ActionId = (uint)this.newTriggerActionId,
                SyncToTimeSeconds = Math.Max(0, this.newTriggerSyncTime),
                FireReminderImmediately = true,
            });
            this.newTriggerActionId = 0;
            this.dataStore.Save();
        }

        this.DrawFflogsImportSection();

        ImGui.Separator();

        if (this.SelectedEncounter == null)
            return;

        if (ImGui.BeginTable("mechanics-table", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("启用");
            ImGui.TableSetupColumn("机制");
            ImGui.TableSetupColumn("时间");
            ImGui.TableSetupColumn("提前");
            ImGui.TableSetupColumn("触发器");
            ImGui.TableSetupColumn("测试");
            ImGui.TableSetupColumn("删除");
            ImGui.TableHeadersRow();

            foreach (var mechanic in this.SelectedEncounter.Mechanics.OrderBy(x => x.TimeSeconds).ToList())
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var enabled = mechanic.Enabled;
                if (ImGui.Checkbox($"##mechanic-enabled-{mechanic.Id}", ref enabled))
                {
                    mechanic.Enabled = enabled;
                    this.dataStore.Save();
                }

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(mechanic.Name);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{mechanic.TimeSeconds:F1}s");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{mechanic.PrewarnSeconds:F1}s");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(string.Join(", ", mechanic.Triggers.Select(x => $"{x.Type}:{x.ActionId}")));
                ImGui.TableNextColumn();
                if (ImGui.SmallButton($"播报##mechanic-{mechanic.Id}"))
                    this.ttsService.Speak($"马上是{mechanic.Name}");
                ImGui.TableNextColumn();
                if (ImGui.SmallButton($"删除##mechanic-{mechanic.Id}"))
                {
                    this.SelectedEncounter.Mechanics.Remove(mechanic);
                    this.dataStore.Save();
                }
            }

            ImGui.EndTable();
        }
    }

    private void DrawFflogsImportSection()
    {
        ImGui.Separator();
        ImGui.TextUnformatted("FFLogs 时间轴导入");

        ImGui.InputText("FFLogs 战斗链接", ref this.fflogsReportInput, 256);

        var importCasts = this.config.FflogsImportCasts;
        if (ImGui.Checkbox("导入读条事件", ref importCasts))
        {
            this.config.FflogsImportCasts = importCasts;
            this.saveConfig();
        }

        ImGui.SameLine();
        var importDamage = this.config.FflogsImportDamageEvents;
        if (ImGui.Checkbox("导入伤害判定事件", ref importDamage))
        {
            this.config.FflogsImportDamageEvents = importDamage;
            this.saveConfig();
        }

        var tokenState = string.IsNullOrWhiteSpace(this.config.FflogsAccessToken) ? "未导入" : "已导入";
        ImGui.TextUnformatted($"FFLogs Token：{tokenState}");
        var clientId = this.config.FflogsClientId;
        if (ImGui.InputText("FFLogs Client ID", ref clientId, 256))
        {
            this.config.FflogsClientId = clientId.Trim();
            this.config.FflogsAccessToken = string.Empty;
            this.config.FflogsAccessTokenExpiresAtUnix = 0;
            this.saveConfig();
        }

        var clientSecret = this.config.FflogsClientSecret;
        if (ImGui.InputText("FFLogs Client Secret", ref clientSecret, 256, ImGuiInputTextFlags.Password))
        {
            this.config.FflogsClientSecret = clientSecret.Trim();
            this.config.FflogsAccessToken = string.Empty;
            this.config.FflogsAccessTokenExpiresAtUnix = 0;
            this.saveConfig();
        }

        var isTokenUpdating = this.fflogsTokenTask is { IsCompleted: false };
        if (isTokenUpdating)
        {
            ImGui.TextUnformatted("正在获取 FFLogs Token...");
        }
        else if (ImGui.Button("获取/更新 FFLogs Token"))
        {
            this.fflogsTokenTask = this.RefreshFflogsTokenAsync();
        }

        ImGui.SameLine();
        if (ImGui.Button("从剪贴板导入 Access Token"))
            this.ImportFflogsTokenFromClipboard();

        ImGui.SameLine();
        if (ImGui.Button("清除 FFLogs Token"))
        {
            this.config.FflogsAccessToken = string.Empty;
            this.config.FflogsAccessTokenExpiresAtUnix = 0;
            this.saveConfig();
            this.fflogsImportStatus = "已清除 FFLogs Token。";
        }

        var isImporting = this.fflogsImportTask is { IsCompleted: false };
        if (isImporting)
        {
            ImGui.TextUnformatted("正在导入 FFLogs 时间轴...");
        }
        else if (ImGui.Button("从 FFLogs 导入到当前时间轴"))
        {
            this.fflogsImportTask = this.ImportFflogsTimelineAsync();
        }

        if (!string.IsNullOrWhiteSpace(this.fflogsImportStatus))
            ImGui.TextWrapped(this.fflogsImportStatus);
    }

    private async Task RefreshFflogsTokenAsync()
    {
        try
        {
            this.fflogsImportStatus = "正在获取 FFLogs Token...";
            await this.fflogsImportService.RefreshAccessTokenAsync().ConfigureAwait(false);
            this.fflogsImportStatus = "FFLogs Token 获取成功，后续会自动复用并在过期时刷新。";
        }
        catch (Exception ex)
        {
            this.fflogsImportStatus = $"FFLogs Token 获取失败：{ex.Message}";
        }
    }

    private void ImportFflogsTokenFromClipboard()
    {
        var token = ImGui.GetClipboardText().Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            this.fflogsImportStatus = "剪贴板为空，请先复制 FFLogs Access Token。";
            return;
        }

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token[7..].Trim();

        this.config.FflogsAccessToken = token;
        this.config.FflogsAccessTokenExpiresAtUnix = 0;
        this.saveConfig();
        this.fflogsImportStatus = "已导入 FFLogs Token，后续会自动复用直到失效。";
    }

    private async Task ImportFflogsTimelineAsync()
    {
        var encounterId = this.SelectedEncounter?.Id;
        if (string.IsNullOrWhiteSpace(encounterId))
        {
            this.fflogsImportStatus = "请先选择一个时间轴分组。";
            return;
        }

        try
        {
            this.fflogsImportStatus = "正在请求 FFLogs...";
            var result = await this.fflogsImportService
                .ImportTimelineAsync(this.fflogsReportInput)
                .ConfigureAwait(false);

            var encounter = this.dataStore.Data.Encounters.FirstOrDefault(x => x.Id == encounterId);
            if (encounter == null)
            {
                this.fflogsImportStatus = "导入失败：当前时间轴分组不存在。";
                return;
            }

            var imported = this.timelineService.ImportMechanics(
                encounter,
                result.Events,
                this.config.DefaultPrewarnSeconds);

            this.selectedEncounterId = encounter.Id;
            this.timelineService.SelectedEncounterId = encounter.Id;
            this.selectedMechanicId = encounter.Mechanics.FirstOrDefault()?.Id ?? string.Empty;

            var fightName = string.IsNullOrWhiteSpace(result.FightName) ? $"fight {result.FightId}" : result.FightName;
            this.fflogsImportStatus = $"FFLogs 导入完成：{fightName}，读取 {result.Events.Count} 条事件，新增 {imported} 条机制。";
        }
        catch (UnauthorizedAccessException ex)
        {
            this.config.FflogsAccessToken = string.Empty;
            this.config.FflogsAccessTokenExpiresAtUnix = 0;
            this.saveConfig();
            this.fflogsImportStatus = $"FFLogs 导入失败：{ex.Message}";
        }
        catch (Exception ex)
        {
            this.fflogsImportStatus = $"FFLogs 导入失败：{ex.Message}";
        }
    }

    private void DrawSettingsTab()
    {
        var ttsEnabled = this.config.TtsEnabled;
        if (ImGui.Checkbox("启用 TTS", ref ttsEnabled))
        {
            this.config.TtsEnabled = ttsEnabled;
            this.saveConfig();
        }

        var autoPull = this.config.AutoPullDetection;
        if (ImGui.Checkbox("自动检测进入战斗", ref autoPull))
        {
            this.config.AutoPullDetection = autoPull;
            this.saveConfig();
        }

        var autoLearn = this.config.AutoLearnTimeline;
        if (ImGui.Checkbox("自动学习时间轴（记录 Boss 读条）", ref autoLearn))
        {
            this.config.AutoLearnTimeline = autoLearn;
            this.saveConfig();
        }

        var autoLearnBattleLog = this.config.AutoLearnBattleLogTimeline;
        if (ImGui.Checkbox("自动学习无读条技能（记录战斗日志技能名）", ref autoLearnBattleLog))
        {
            this.config.AutoLearnBattleLogTimeline = autoLearnBattleLog;
            this.saveConfig();
        }

        var onlyCurrentParty = this.config.OnlyCurrentParty;
        if (ImGui.Checkbox("只播报当前队伍成员", ref onlyCurrentParty))
        {
            this.config.OnlyCurrentParty = onlyCurrentParty;
            this.saveConfig();
        }

        var volume = this.config.TtsVolume;
        if (ImGui.SliderInt("音量", ref volume, 0, 100))
        {
            this.config.TtsVolume = volume;
            this.ttsService.ApplySettings();
            this.saveConfig();
        }

        var rate = this.config.TtsRate;
        if (ImGui.SliderInt("语速", ref rate, -10, 10))
        {
            this.config.TtsRate = rate;
            this.ttsService.ApplySettings();
            this.saveConfig();
        }

        var minCount = this.config.MinMistakeCountToSpeak;
        if (ImGui.InputInt("最少次数才播报", ref minCount))
        {
            this.config.MinMistakeCountToSpeak = Math.Max(1, minCount);
            this.saveConfig();
        }

        var template = this.config.ReminderTemplate;
        if (ImGui.InputTextMultiline("播报模板", ref template, 256, new Vector2(-1, 80)))
        {
            this.config.ReminderTemplate = template;
            this.saveConfig();
        }

        ImGui.TextUnformatted("可用变量：{name} {job} {mechanic} {type} {note} {count}");
    }

    private void DrawEncounterCombo()
    {
        var current = this.SelectedEncounter?.Name ?? "未选择";
        if (!ImGui.BeginCombo("副本", current))
            return;

        foreach (var encounter in this.dataStore.Data.Encounters)
        {
            var selected = encounter.Id == this.selectedEncounterId;
            if (ImGui.Selectable($"{encounter.Name}##{encounter.Id}", selected))
            {
                this.selectedEncounterId = encounter.Id;
                this.timelineService.SelectedEncounterId = encounter.Id;
                this.selectedMechanicId = encounter.Mechanics.FirstOrDefault()?.Id ?? string.Empty;
            }

            if (selected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.EndCombo();
    }

    private void DrawMechanicCombo()
    {
        var current = this.SelectedMechanic?.Name ?? "未选择";
        if (!ImGui.BeginCombo("机制", current))
            return;

        foreach (var mechanic in this.SelectedEncounter?.Mechanics ?? [])
        {
            var selected = mechanic.Id == this.selectedMechanicId;
            if (ImGui.Selectable($"{mechanic.Name}##{mechanic.Id}", selected))
                this.selectedMechanicId = mechanic.Id;

            if (selected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.EndCombo();
    }

    private void DrawMemberCombo()
    {
        if (string.IsNullOrWhiteSpace(this.selectedMemberId) && this.dataStore.Data.Members.Count > 0)
            this.selectedMemberId = this.dataStore.Data.Members[0].Id;

        var current = this.dataStore.Data.Members.FirstOrDefault(x => x.Id == this.selectedMemberId)?.DisplayName ?? "未选择";
        if (!ImGui.BeginCombo("成员", current))
            return;

        foreach (var member in this.dataStore.Data.Members)
        {
            var selected = member.Id == this.selectedMemberId;
            if (ImGui.Selectable($"{member.DisplayName}##{member.Id}", selected))
                this.selectedMemberId = member.Id;

            if (selected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.EndCombo();
    }

    private void DrawGroupCombo()
    {
        if (string.IsNullOrWhiteSpace(this.selectedGroupId) && this.dataStore.Data.Groups.Count > 0)
            this.selectedGroupId = this.dataStore.Data.Groups[0].Id;

        var current = this.SelectedGroup?.Name ?? "未选择";
        if (!ImGui.BeginCombo("队伍分组", current))
            return;

        foreach (var group in this.dataStore.Data.Groups)
        {
            var selected = group.Id == this.selectedGroupId;
            if (ImGui.Selectable($"{group.Name}##{group.Id}", selected))
                this.selectedGroupId = group.Id;

            if (selected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.EndCombo();
    }

    private string ExportGroup(TeamGroup group)
    {
        var package = new TeamGroupPackage
        {
            Group = new TeamGroup
            {
                Id = group.Id,
                Name = group.Name,
                MemberIds = [.. group.MemberIds],
                UpdatedAt = group.UpdatedAt,
            },
            Members = group.MemberIds
                .Select(id => this.dataStore.Data.Members.FirstOrDefault(x => x.Id == id))
                .Where(member => member != null)
                .Select(member => member!)
                .ToList(),
        };

        return JsonSerializer.Serialize(package, JsonOptions);
    }

    private void ImportGroup(string json)
    {
        try
        {
            var package = JsonSerializer.Deserialize<TeamGroupPackage>(json, JsonOptions);
            if (package?.Group == null)
            {
                this.groupImportStatus = "导入失败：JSON 中没有队伍分组";
                return;
            }

            foreach (var member in package.Members)
            {
                var existing = this.dataStore.Data.Members.FirstOrDefault(x => x.Id.Equals(member.Id, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    this.dataStore.Data.Members.Add(member);
                    continue;
                }

                existing.Name = member.Name;
                existing.World = member.World;
                existing.Job = member.Job;
                existing.Alias = member.Alias;
            }

            var importedGroup = package.Group;
            if (string.IsNullOrWhiteSpace(importedGroup.Id))
                importedGroup.Id = Guid.NewGuid().ToString("N");

            var existingGroup = this.dataStore.Data.Groups.FirstOrDefault(x =>
                x.Id.Equals(importedGroup.Id, StringComparison.OrdinalIgnoreCase) ||
                x.Name.Equals(importedGroup.Name, StringComparison.OrdinalIgnoreCase));

            if (existingGroup == null)
            {
                importedGroup.UpdatedAt = DateTimeOffset.Now;
                this.dataStore.Data.Groups.Add(importedGroup);
                this.selectedGroupId = importedGroup.Id;
            }
            else
            {
                existingGroup.Name = importedGroup.Name;
                existingGroup.MemberIds = importedGroup.MemberIds
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                existingGroup.UpdatedAt = DateTimeOffset.Now;
                this.selectedGroupId = existingGroup.Id;
            }

            this.dataStore.Save();
            this.groupImportStatus = $"已导入分组：{this.SelectedGroup?.Name ?? importedGroup.Name}";
        }
        catch (Exception ex)
        {
            this.groupImportStatus = $"导入失败：{ex.Message}";
        }
    }

    private static void DrawStringCombo(string label, ref string value, IReadOnlyList<string> options)
    {
        if (!ImGui.BeginCombo(label, value))
            return;

        foreach (var option in options)
        {
            var selected = option == value;
            if (ImGui.Selectable(option, selected))
                value = option;

            if (selected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.EndCombo();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed class TeamGroupPackage
    {
        public int Version { get; set; } = 1;

        public TeamGroup? Group { get; set; }

        public List<TeamMember> Members { get; set; } = [];
    }
}
