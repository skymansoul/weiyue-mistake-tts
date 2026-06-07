namespace WeiyueMistakeTTS.Models;

public sealed class TeamMember
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string World { get; set; } = string.Empty;

    public string Job { get; set; } = string.Empty;

    public string Alias { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(this.Alias) ? this.Name : this.Alias;
}

