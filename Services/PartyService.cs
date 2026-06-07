using Dalamud.Plugin.Services;
using WeiyueMistakeTTS.Models;

namespace WeiyueMistakeTTS.Services;

public sealed class PartyService
{
    private readonly IPartyList partyList;
    private readonly DataStore dataStore;
    private readonly IObjectTable objectTable;

    public PartyService(IPartyList partyList, DataStore dataStore, IObjectTable objectTable)
    {
        this.partyList = partyList;
        this.dataStore = dataStore;
        this.objectTable = objectTable;
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
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var partyMember in this.partyList)
        {
            var name = partyMember.Name.TextValue;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var world = partyMember.World.ValueNullable?.Name.ExtractText() ?? string.Empty;
            var job = partyMember.ClassJob.ValueNullable?.Abbreviation.ExtractText() ?? string.Empty;
            var member = this.dataStore.GetOrCreateMember(name, world, job);
            if (seenIds.Add(member.Id))
                members.Add(member);
        }

        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer != null)
        {
            var name = localPlayer.Name.TextValue;
            if (!string.IsNullOrWhiteSpace(name))
            {
                var world = localPlayer.HomeWorld.ValueNullable?.Name.ExtractText()
                    ?? localPlayer.CurrentWorld.ValueNullable?.Name.ExtractText()
                    ?? string.Empty;
                var job = localPlayer.ClassJob.ValueNullable?.Abbreviation.ExtractText() ?? string.Empty;
                var member = this.dataStore.GetOrCreateMember(name, world, job);
                if (seenIds.Add(member.Id))
                    members.Add(member);
            }
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
