using System.Text.Json;
using System.Text.Json.Serialization;
using WeiyueMistakeTTS.Models;

namespace WeiyueMistakeTTS.Services;

public sealed class DataStore
{
    private readonly string dataPath;
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public PersistedData Data { get; private set; } = new();

    public DataStore(string configDirectory)
    {
        Directory.CreateDirectory(configDirectory);
        this.dataPath = Path.Combine(configDirectory, "weiyue-mistake-data.json");
        this.Load();
    }

    public void Load()
    {
        if (!File.Exists(this.dataPath))
        {
            this.Data = CreateDefaultData();
            this.Save();
            return;
        }

        var json = File.ReadAllText(this.dataPath);
        this.Data = JsonSerializer.Deserialize<PersistedData>(json, this.jsonOptions) ?? new PersistedData();

        if (this.Data.Encounters.Count == 0)
            this.Data.Encounters.Add(CreateDefaultEncounter());

        if (this.Data.Groups.Count == 0)
            this.Data.Groups.Add(CreateDefaultGroup());
    }

    public void Save()
    {
        var tempPath = this.dataPath + ".tmp";
        var json = JsonSerializer.Serialize(this.Data, this.jsonOptions);
        File.WriteAllText(tempPath, json);
        File.Copy(tempPath, this.dataPath, true);
        File.Delete(tempPath);
    }

    public TeamMember GetOrCreateMember(string name, string world = "", string job = "")
    {
        var id = MakeMemberId(name, world);
        var member = this.Data.Members.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (member != null)
            return member;

        member = new TeamMember
        {
            Id = id,
            Name = name,
            World = world,
            Job = job,
        };

        this.Data.Members.Add(member);
        this.Save();
        return member;
    }

    public TeamGroup GetOrCreateGroup(string name)
    {
        var normalized = string.IsNullOrWhiteSpace(name) ? "默认队伍" : name.Trim();
        var group = this.Data.Groups.FirstOrDefault(x => x.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (group != null)
            return group;

        group = new TeamGroup
        {
            Name = normalized,
        };
        this.Data.Groups.Add(group);
        this.Save();
        return group;
    }

    public static string MakeMemberId(string name, string world)
    {
        return string.IsNullOrWhiteSpace(world) ? name.Trim() : $"{name.Trim()}@{world.Trim()}";
    }

    private static PersistedData CreateDefaultData()
    {
        return new PersistedData
        {
            Groups = [CreateDefaultGroup()],
            Encounters = [CreateDefaultEncounter()],
        };
    }

    private static TeamGroup CreateDefaultGroup()
    {
        return new TeamGroup
        {
            Id = "default-group",
            Name = "默认队伍",
        };
    }

    private static EncounterDefinition CreateDefaultEncounter()
    {
        return new EncounterDefinition
        {
            Id = "default",
            Name = "默认时间轴",
            TerritoryType = 0,
            Mechanics =
            [
                new MechanicDefinition
                {
                    Id = "stack-1",
                    Name = "第一次分摊",
                    TimeSeconds = 45,
                    PrewarnSeconds = 8,
                },
                new MechanicDefinition
                {
                    Id = "tower-1",
                    Name = "第一次踩塔",
                    TimeSeconds = 78,
                    PrewarnSeconds = 10,
                },
            ],
        };
    }
}
