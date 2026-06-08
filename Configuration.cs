using Dalamud.Configuration;

namespace WeiyueMistakeTTS;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool MainWindowOpen { get; set; }

    public bool TtsEnabled { get; set; } = true;

    public int TtsVolume { get; set; } = 80;

    public int TtsRate { get; set; }

    public double DefaultPrewarnSeconds { get; set; } = 8;

    public bool AutoPullDetection { get; set; } = true;

    public bool AutoLearnTimeline { get; set; } = true;

    public bool AutoLearnBattleLogTimeline { get; set; } = true;

    public bool FflogsImportCasts { get; set; } = true;

    public bool FflogsImportDamageEvents { get; set; } = true;

    public string FflogsClientId { get; set; } = string.Empty;

    public string FflogsClientSecret { get; set; } = string.Empty;

    public string FflogsAccessToken { get; set; } = string.Empty;

    public long FflogsAccessTokenExpiresAtUnix { get; set; }

    public bool OnlyCurrentParty { get; set; } = false;

    public int MinMistakeCountToSpeak { get; set; } = 1;

    public string ReminderTemplate { get; set; } = "{name}注意，马上是{mechanic}，上次问题：{note}";
}
