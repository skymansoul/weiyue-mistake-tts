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

        foreach (var partyMember in this.partyList)
        {
            var name = partyMember.Name.TextValue;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var world = partyMember.World.ValueNullable?.Name.ExtractText() ?? string.Empty;
            var job = partyMember.ClassJob.ValueNullable?.Abbreviation.ExtractText() ?? string.Empty;
            var member = this.dataStore.GetOrCreateMember(name, world, job);
            ids.Add(member.Id);
        }

        return ids;
    }
}

