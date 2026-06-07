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
