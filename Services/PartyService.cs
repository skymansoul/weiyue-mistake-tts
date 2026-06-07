using Dalamud.Plugin.Services;
using WeiyueMistakeTTS.Models;

namespace WeiyueMistakeTTS.Services;

public sealed class PartyService
{
    private readonly IPartyList partyList;
    private readonly DataStore dataStore;

    public PartyService(IPartyList partyList, DataStore dataStore)
    {
        this.partyList = partyList;
        this.dataStore = dataStore;
    }

    public IReadOnlySet<string> SyncCurrentParty()
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var member in this.GetCurrentPartyMembers())
            ids.Add(member.Id);

        return ids;
    }

    public IReadOnlyList<TeamMember> GetCurrentPartyMembers()
    {
        var members = new List<TeamMember>();

        foreach (var partyMember in this.partyList)
        {
            var name = partyMember.Name.TextValue;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var world = partyMember.World.ValueNullable?.Name.ExtractText() ?? string.Empty;
            var job = partyMember.ClassJob.ValueNullable?.Abbreviation.ExtractText() ?? string.Empty;
            var member = this.dataStore.GetOrCreateMember(name, world, job);
            members.Add(member);
        }

        return members;
    }

    public int ImportCurrentPartyToGroup(TeamGroup group)
    {
        var partyMembers = this.GetCurrentPartyMembers();
        group.MemberIds = partyMembers
            .Select(x => x.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        group.UpdatedAt = DateTimeOffset.Now;
        this.dataStore.Save();
        return group.MemberIds.Count;
    }
}
