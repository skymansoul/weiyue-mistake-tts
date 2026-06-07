using WeiyueMistakeTTS.Models;

namespace WeiyueMistakeTTS.Services;

public sealed class TimelineService
{
    private readonly DataStore dataStore;

    public string? SelectedEncounterId { get; set; }

    public TimelineService(DataStore dataStore)
    {
        this.dataStore = dataStore;
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
}

