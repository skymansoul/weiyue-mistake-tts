namespace WeiyueMistakeTTS.Models;

public sealed class MechanicDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public double TimeSeconds { get; set; }

    public double PrewarnSeconds { get; set; } = 8;

    public bool Enabled { get; set; } = true;

    public List<MechanicTrigger> Triggers { get; set; } = [];
}

