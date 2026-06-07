namespace WeiyueMistakeTTS.Models;

public sealed class TeamGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "默认队伍";

    public List<string> MemberIds { get; set; } = [];

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
}

