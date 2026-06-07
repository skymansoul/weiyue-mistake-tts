namespace WeiyueMistakeTTS.Models;

public sealed class MechanicTrigger
{
    public string Type { get; set; } = "CastStart";

    public uint ActionId { get; set; }

    public string SourceName { get; set; } = string.Empty;

    public double SyncToTimeSeconds { get; set; }

    public bool FireReminderImmediately { get; set; } = true;
}

