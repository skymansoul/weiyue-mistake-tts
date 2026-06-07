namespace WeiyueMistakeTTS.Models;

public sealed class PersistedData
{
    public List<TeamMember> Members { get; set; } = [];

    public List<EncounterDefinition> Encounters { get; set; } = [];

    public List<MistakeRecord> Mistakes { get; set; } = [];
}

