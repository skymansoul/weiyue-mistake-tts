using WeiyueMistakeTTS.Models;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace WeiyueMistakeTTS.Services;

public sealed class TimelineService
{
    private readonly DataStore dataStore;
    private readonly IDataManager dataManager;

    public string? SelectedEncounterId { get; set; }

    public TimelineService(DataStore dataStore, IDataManager dataManager)
    {
        this.dataStore = dataStore;
        this.dataManager = dataManager;
        this.SelectedEncounterId = dataStore.Data.Encounters.FirstOrDefault()?.Id;
    }

    public EncounterDefinition? CurrentEncounter(uint territoryType)
    {
        var selected = this.dataStore.Data.Encounters.FirstOrDefault(x => x.Id == this.SelectedEncounterId);
        if (selected != null && (selected.TerritoryType == 0 || selected.TerritoryType == territoryType))
            return selected;

        return this.dataStore.Data.Encounters.FirstOrDefault(x => x.TerritoryType == territoryType)
            ?? this.dataStore.Data.Encounters.FirstOrDefault(x => x.TerritoryType == 0)
            ?? this.dataStore.Data.Encounters.FirstOrDefault();
    }

    public string GetCurrentTerritoryName(uint territoryType)
    {
        if (territoryType == 0)
            return "未知副本";

        try
        {
            var territory = this.dataManager.GetExcelSheet<TerritoryType>().GetRowOrDefault(territoryType);
            var dutyName = territory?.ContentFinderCondition.ValueNullable?.Name.ExtractText();
            if (!string.IsNullOrWhiteSpace(dutyName))
                return dutyName;

            var placeName = territory?.PlaceName.ValueNullable?.Name.ExtractText();
            if (!string.IsNullOrWhiteSpace(placeName))
                return placeName;
        }
        catch
        {
            // Fall back to the numeric territory id if Lumina data is not ready.
        }

        return $"Territory {territoryType}";
    }

    public MechanicDefinition AddMechanicAtCurrentTime(
        EncounterDefinition encounter,
        string mechanicName,
        double elapsedSeconds,
        double prewarnSeconds)
    {
        var name = string.IsNullOrWhiteSpace(mechanicName)
            ? $"机制 {elapsedSeconds:F1}s"
            : mechanicName.Trim();

        var mechanic = new MechanicDefinition
        {
            Name = name,
            TimeSeconds = Math.Max(0, elapsedSeconds),
            PrewarnSeconds = Math.Max(0, prewarnSeconds),
        };

        encounter.Mechanics.Add(mechanic);
        encounter.Mechanics = encounter.Mechanics
            .OrderBy(x => x.TimeSeconds)
            .ToList();
        this.SelectedEncounterId = encounter.Id;
        this.dataStore.Save();
        return mechanic;
    }

    public MechanicDefinition? LearnCastMechanic(
        EncounterDefinition encounter,
        uint actionId,
        string actionName,
        double elapsedSeconds,
        double prewarnSeconds)
    {
        if (actionId == 0 || string.IsNullOrWhiteSpace(actionName))
            return null;

        var duplicate = encounter.Mechanics.Any(mechanic =>
            mechanic.Triggers.Any(trigger => trigger.ActionId == actionId) &&
            Math.Abs(mechanic.TimeSeconds - elapsedSeconds) <= 2);

        if (duplicate)
            return null;

        var mechanic = new MechanicDefinition
        {
            Name = actionName.Trim(),
            TimeSeconds = Math.Max(0, elapsedSeconds),
            PrewarnSeconds = Math.Max(0, prewarnSeconds),
            Triggers =
            [
                new MechanicTrigger
                {
                    Type = "CastStart",
                    ActionId = actionId,
                    SyncToTimeSeconds = Math.Max(0, elapsedSeconds),
                    FireReminderImmediately = false,
                },
            ],
        };

        encounter.Mechanics.Add(mechanic);
        encounter.Mechanics = encounter.Mechanics
            .OrderBy(x => x.TimeSeconds)
            .ToList();
        this.SelectedEncounterId = encounter.Id;
        this.dataStore.Save();
        return mechanic;
    }

    public MechanicDefinition? LearnLogMechanic(
        EncounterDefinition encounter,
        string mechanicName,
        double elapsedSeconds,
        double prewarnSeconds)
    {
        if (string.IsNullOrWhiteSpace(mechanicName))
            return null;

        var name = mechanicName.Trim();
        var duplicate = encounter.Mechanics.Any(mechanic =>
            mechanic.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(mechanic.TimeSeconds - elapsedSeconds) <= 2);

        if (duplicate)
            return null;

        var mechanic = new MechanicDefinition
        {
            Name = name,
            TimeSeconds = Math.Max(0, elapsedSeconds),
            PrewarnSeconds = Math.Max(0, prewarnSeconds),
            Triggers =
            [
                new MechanicTrigger
                {
                    Type = "BattleLog",
                    SyncToTimeSeconds = Math.Max(0, elapsedSeconds),
                    FireReminderImmediately = false,
                },
            ],
        };

        encounter.Mechanics.Add(mechanic);
        encounter.Mechanics = encounter.Mechanics
            .OrderBy(x => x.TimeSeconds)
            .ToList();
        this.SelectedEncounterId = encounter.Id;
        this.dataStore.Save();
        return mechanic;
    }

    public EncounterDefinition GetOrCreateEncounterForTerritory(uint territoryType)
    {
        var existing = this.dataStore.Data.Encounters.FirstOrDefault(x => x.TerritoryType == territoryType);
        if (existing != null)
        {
            this.SelectedEncounterId = existing.Id;
            return existing;
        }

        var encounter = new EncounterDefinition
        {
            Id = $"territory-{territoryType}",
            Name = this.GetCurrentTerritoryName(territoryType),
            TerritoryType = territoryType,
        };

        this.dataStore.Data.Encounters.Add(encounter);
        this.SelectedEncounterId = encounter.Id;
        this.dataStore.Save();
        return encounter;
    }
}
