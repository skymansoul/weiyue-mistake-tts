namespace WeiyueMistakeTTS.Models;

public sealed class MistakeRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string EncounterId { get; set; } = string.Empty;

    public string MechanicId { get; set; } = string.Empty;

    public string MemberId { get; set; } = string.Empty;

    public string MistakeType { get; set; } = "自定义";

    public string Note { get; set; } = string.Empty;

    public int Count { get; set; } = 1;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    public bool Enabled { get; set; } = true;
}

