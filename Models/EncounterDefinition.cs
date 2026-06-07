namespace WeiyueMistakeTTS.Models;

public sealed class EncounterDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public uint TerritoryType { get; set; }

    public List<MechanicDefinition> Mechanics { get; set; } = [];
}

