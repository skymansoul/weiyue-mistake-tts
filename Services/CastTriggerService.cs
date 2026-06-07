using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using WeiyueMistakeTTS.Models;

namespace WeiyueMistakeTTS.Services;

public sealed class CastTriggerService
{
    private readonly IObjectTable objectTable;
    private readonly EncounterClock clock;
    private readonly ReminderService reminderService;
    private readonly HashSet<uint> seenActionCasts = [];

    public CastTriggerService(
        IObjectTable objectTable,
        EncounterClock clock,
        ReminderService reminderService)
    {
        this.objectTable = objectTable;
        this.clock = clock;
        this.reminderService = reminderService;
    }

    public void Reset()
    {
        this.seenActionCasts.Clear();
    }

    public void Tick(EncounterDefinition? encounter, IReadOnlySet<string>? currentPartyMemberIds)
    {
        if (encounter == null)
            return;

        foreach (var obj in this.objectTable)
        {
            if (obj is not IBattleChara battleChara)
                continue;

            if (battleChara.ObjectKind != ObjectKind.BattleNpc || !battleChara.IsCasting)
                continue;

            var actionId = battleChara.CastActionId;
            if (actionId == 0 || !this.seenActionCasts.Add(actionId))
                continue;

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
    }
}

