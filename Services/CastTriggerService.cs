using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using WeiyueMistakeTTS.Models;
using LuminaAction = Lumina.Excel.Sheets.Action;

namespace WeiyueMistakeTTS.Services;

public sealed class CastTriggerService
{
    private readonly IObjectTable objectTable;
    private readonly IDataManager dataManager;
    private readonly Configuration config;
    private readonly EncounterClock clock;
    private readonly TimelineService timelineService;
    private readonly ReminderService reminderService;
    private readonly HashSet<string> activeCastKeys = [];

    public CastTriggerService(
        IObjectTable objectTable,
        IDataManager dataManager,
        Configuration config,
        EncounterClock clock,
        TimelineService timelineService,
        ReminderService reminderService)
    {
        this.objectTable = objectTable;
        this.dataManager = dataManager;
        this.config = config;
        this.clock = clock;
        this.timelineService = timelineService;
        this.reminderService = reminderService;
    }

    public void Reset()
    {
        this.activeCastKeys.Clear();
    }

    public void Tick(EncounterDefinition? encounter, IReadOnlySet<string>? currentPartyMemberIds)
    {
        if (encounter == null)
            return;

        var currentCastKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var obj in this.objectTable)
        {
            if (obj is not IBattleChara battleChara)
                continue;

            if (battleChara.ObjectKind != ObjectKind.BattleNpc || !battleChara.IsCasting)
                continue;

            var actionId = battleChara.CastActionId;
            if (actionId == 0)
                continue;

            var castKey = $"{battleChara.GameObjectId:X}:{actionId}";
            currentCastKeys.Add(castKey);
            if (!this.activeCastKeys.Add(castKey))
                continue;

            if (this.config.AutoLearnTimeline && this.clock.IsRunning)
            {
                var actionName = this.GetActionName(actionId);
                this.timelineService.LearnCastMechanic(
                    encounter,
                    actionId,
                    actionName,
                    this.clock.ElapsedSeconds,
                    this.config.DefaultPrewarnSeconds);
            }

            foreach (var mechanic in encounter.Mechanics)
            {
                var trigger = mechanic.Triggers.FirstOrDefault(x =>
                    x.Type.Equals("CastStart", StringComparison.OrdinalIgnoreCase) &&
                    x.ActionId == actionId);

                if (trigger == null)
                    continue;

                var diff = trigger.SyncToTimeSeconds - this.clock.ElapsedSeconds;
                if (!this.clock.IsRunning || Math.Abs(diff) >= 1.5)
                    this.clock.AdjustTo(trigger.SyncToTimeSeconds);

                if (trigger.FireReminderImmediately)
                    this.reminderService.SpeakForMechanic(encounter, mechanic, currentPartyMemberIds, true);
            }
        }

        this.activeCastKeys.IntersectWith(currentCastKeys);
    }

    private string GetActionName(uint actionId)
    {
        try
        {
            var action = this.dataManager.GetExcelSheet<LuminaAction>().GetRowOrDefault(actionId);
            var name = action?.Name.ExtractText();
            if (!string.IsNullOrWhiteSpace(name))
                return name;
        }
        catch
        {
            // Fall back below when Lumina data is not available.
        }

        return $"Action {actionId}";
    }
}
