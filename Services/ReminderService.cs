using WeiyueMistakeTTS.Models;

namespace WeiyueMistakeTTS.Services;

public sealed class ReminderService
{
    private readonly Configuration config;
    private readonly DataStore dataStore;
    private readonly EncounterClock clock;
    private readonly TtsService ttsService;
    private readonly HashSet<string> remindedKeys = [];

    public ReminderService(
        Configuration config,
        DataStore dataStore,
        EncounterClock clock,
        TtsService ttsService)
    {
        this.config = config;
        this.dataStore = dataStore;
        this.clock = clock;
        this.ttsService = ttsService;
    }

    public void Reset()
    {
        this.remindedKeys.Clear();
    }

    public void Tick(EncounterDefinition? encounter, IReadOnlySet<string>? currentPartyMemberIds)
    {
        if (encounter == null || !this.clock.IsRunning)
            return;

        var elapsed = this.clock.ElapsedSeconds;
        foreach (var mechanic in encounter.Mechanics.Where(x => x.Enabled))
        {
            var remindAt = mechanic.TimeSeconds - mechanic.PrewarnSeconds;
            if (elapsed < remindAt)
                continue;

            this.SpeakForMechanic(encounter, mechanic, currentPartyMemberIds, false);
        }
    }

    public void SpeakForMechanic(
        EncounterDefinition encounter,
        MechanicDefinition mechanic,
        IReadOnlySet<string>? currentPartyMemberIds,
        bool force)
    {
        var key = $"{encounter.Id}:{mechanic.Id}:{this.clock.PullId}";
        if (!force && !this.remindedKeys.Add(key))
            return;

        if (force)
            this.remindedKeys.Add(key);

        var records = this.dataStore.Data.Mistakes
            .Where(x => x.Enabled)
            .Where(x => x.EncounterId == encounter.Id)
            .Where(x => x.MechanicId == mechanic.Id)
            .Where(x => x.Count >= this.config.MinMistakeCountToSpeak)
            .ToList();

        if (this.config.OnlyCurrentParty && currentPartyMemberIds != null)
            records = records.Where(x => currentPartyMemberIds.Contains(x.MemberId)).ToList();

        if (records.Count == 0)
            return;

        var texts = records
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.UpdatedAt)
            .Take(3)
            .Select(record =>
            {
                var member = this.dataStore.Data.Members.FirstOrDefault(x => x.Id == record.MemberId);
                return TextTemplateRenderer.Render(this.config.ReminderTemplate, member, mechanic, record);
            })
            .ToList();

        this.ttsService.Speak(TrimText(string.Join("；", texts), 120));
    }

    private static string TrimText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text[..maxLength];
    }
}

