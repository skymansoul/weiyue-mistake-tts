using Dalamud.Game.Chat;
using WeiyueMistakeTTS.Models;

namespace WeiyueMistakeTTS.Services;

public sealed class BattleLogLearningService
{
    private readonly Configuration config;
    private readonly EncounterClock clock;
    private readonly TimelineService timelineService;
    private readonly HashSet<string> seenLogKeys = [];

    public BattleLogLearningService(
        Configuration config,
        EncounterClock clock,
        TimelineService timelineService)
    {
        this.config = config;
        this.clock = clock;
        this.timelineService = timelineService;
    }

    public void Reset()
    {
        this.seenLogKeys.Clear();
    }

    public void Learn(EncounterDefinition? encounter, ILogMessage message)
    {
        if (!this.config.AutoLearnTimeline || !this.config.AutoLearnBattleLogTimeline)
            return;

        if (!this.clock.IsRunning || encounter == null)
            return;

        var source = message.SourceEntity;
        if (source == null || source.IsPlayer)
            return;

        var sourceName = source.Name.ToString();
        var targetName = message.TargetEntity?.Name.ToString() ?? string.Empty;

        foreach (var candidate in this.GetStringCandidates(message))
        {
            if (!IsLikelyMechanicName(candidate, sourceName, targetName))
                continue;

            var key = $"{message.LogMessageId}:{sourceName}:{candidate}:{Math.Floor(this.clock.ElapsedSeconds)}";
            if (!this.seenLogKeys.Add(key))
                continue;

            this.timelineService.LearnLogMechanic(
                encounter,
                candidate,
                this.clock.ElapsedSeconds,
                this.config.DefaultPrewarnSeconds);
        }
    }

    private IEnumerable<string> GetStringCandidates(ILogMessage message)
    {
        for (var i = 0; i < message.ParameterCount; i++)
        {
            if (!message.TryGetStringParameter(i, out var value))
                continue;

            var text = value.ExtractText().Trim();
            if (!string.IsNullOrWhiteSpace(text))
                yield return text;
        }
    }

    private static bool IsLikelyMechanicName(string text, string sourceName, string targetName)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        if (trimmed.Length < 2 || trimmed.Length > 40)
            return false;

        if (trimmed.Equals(sourceName, StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (trimmed.All(char.IsDigit))
            return false;

        if (trimmed.Contains("HP", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("MP", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
