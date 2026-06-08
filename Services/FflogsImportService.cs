using System.Text;
using System.Text.Json;

namespace WeiyueMistakeTTS.Services;

public sealed class FflogsImportService : IDisposable
{
    private const string GraphQlEndpoint = "https://www.fflogs.com/api/v2/client";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Configuration config;
    private readonly HttpClient httpClient = new();

    public FflogsImportService(Configuration config)
    {
        this.config = config;
    }

    public async Task<FflogsTimelineImportResult> ImportTimelineAsync(string reportInput, CancellationToken cancellationToken = default)
    {
        var reportLocation = ParseReportLocation(reportInput);
        var reportCode = reportLocation.ReportCode;
        var fightId = reportLocation.FightId;
        if (string.IsNullOrWhiteSpace(reportCode))
            throw new InvalidOperationException("请填写 FFLogs 报告链接或报告 code。");

        if (fightId <= 0)
            throw new InvalidOperationException("链接中没有 fight 参数，请打开 FFLogs 的具体战斗后复制完整链接。");

        var accessToken = await this.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        var dataTypes = new List<string>();
        if (this.config.FflogsImportCasts)
            dataTypes.Add("Casts");
        if (this.config.FflogsImportDamageEvents)
            dataTypes.Add("Damage");
        if (dataTypes.Count == 0)
            dataTypes.Add("Casts");

        var events = new List<ImportedTimelineEvent>();
        var fightName = string.Empty;
        var reportTitle = string.Empty;

        foreach (var dataType in dataTypes)
        {
            var page = await this.FetchEventsAsync(reportCode, fightId, dataType, accessToken, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(page.FightName))
                fightName = page.FightName;
            if (!string.IsNullOrWhiteSpace(page.ReportTitle))
                reportTitle = page.ReportTitle;
            events.AddRange(page.Events);
        }

        var deduped = events
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .OrderBy(x => x.TimeSeconds)
            .GroupBy(x => MakeTimelineEventKey(x), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(x => x.TimeSeconds).First())
            .ToList();

        return new FflogsTimelineImportResult(reportCode, fightId, reportTitle, fightName, deduped);
    }

    public void Dispose()
    {
        this.httpClient.Dispose();
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(this.config.FflogsAccessToken) &&
            this.config.FflogsAccessTokenExpiresAtUnix > DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds())
            return this.config.FflogsAccessToken.Trim();

        if (!string.IsNullOrWhiteSpace(this.config.FflogsAccessToken))
            return this.config.FflogsAccessToken.Trim();

        await Task.CompletedTask.ConfigureAwait(false);
        throw new InvalidOperationException("FFLogs 官方 API 需要访问令牌。当前界面已隐藏开发者凭据输入，请使用已内置/预配置的 token 构建，或提供一个代理接口。");
    }

    private async Task<FflogsEventPage> FetchEventsAsync(
        string reportCode,
        int fightId,
        string dataType,
        string accessToken,
        CancellationToken cancellationToken)
    {
        const string query = """
            query TeamMistakeTimeline($code: String!, $fightIds: [Int]!, $dataType: EventDataType!, $startTime: Float) {
              reportData {
                report(code: $code) {
                  title
                  fights(fightIDs: $fightIds) {
                    id
                    name
                    startTime
                    endTime
                    encounterID
                    kill
                  }
                  events(fightIDs: $fightIds, dataType: $dataType, hostilityType: Enemies, startTime: $startTime, limit: 10000, translate: true) {
                    data
                    nextPageTimestamp
                  }
                }
              }
            }
            """;

        var allEvents = new List<ImportedTimelineEvent>();
        var reportTitle = string.Empty;
        var fightName = string.Empty;
        double fightStartTime = 0;
        double? nextPageTimestamp = null;

        for (var page = 0; page < 20; page++)
        {
            var variables = new Dictionary<string, object?>
            {
                ["code"] = reportCode,
                ["fightIds"] = new[] { fightId },
                ["dataType"] = dataType,
                ["startTime"] = nextPageTimestamp,
            };
            var payload = JsonSerializer.Serialize(new { query, variables }, JsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, GraphQlEndpoint);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await this.httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"FFLogs API 请求失败：{(int)response.StatusCode} {response.ReasonPhrase} {json}");

            using var document = JsonDocument.Parse(json);
            ThrowIfGraphQlError(document.RootElement);

            var report = document.RootElement
                .GetProperty("data")
                .GetProperty("reportData")
                .GetProperty("report");

            if (report.ValueKind == JsonValueKind.Null)
                throw new InvalidOperationException("FFLogs 没有找到这个报告，请检查链接/code。");

            if (string.IsNullOrWhiteSpace(reportTitle) && report.TryGetProperty("title", out var titleElement))
                reportTitle = titleElement.GetString() ?? string.Empty;

            var fight = report.GetProperty("fights").EnumerateArray().FirstOrDefault();
            if (fight.ValueKind == JsonValueKind.Undefined)
                throw new InvalidOperationException("FFLogs 没有找到这个 fight id，请检查链接中的 fight= 参数。");

            if (string.IsNullOrWhiteSpace(fightName) && fight.TryGetProperty("name", out var nameElement))
                fightName = nameElement.GetString() ?? string.Empty;
            if (fightStartTime <= 0 && fight.TryGetProperty("startTime", out var startElement))
                fightStartTime = startElement.GetDouble();

            var eventsElement = report.GetProperty("events");
            if (eventsElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var eventElement in dataElement.EnumerateArray())
                {
                    var timelineEvent = ParseEvent(eventElement, fightStartTime, dataType);
                    if (timelineEvent != null)
                        allEvents.Add(timelineEvent);
                }
            }

            nextPageTimestamp = TryGetDouble(eventsElement, "nextPageTimestamp");
            if (nextPageTimestamp is null)
                break;
        }

        return new FflogsEventPage(reportTitle, fightName, allEvents);
    }

    private static ImportedTimelineEvent? ParseEvent(JsonElement eventElement, double fightStartTime, string dataType)
    {
        var timestamp = TryGetDouble(eventElement, "timestamp");
        if (timestamp is null)
            return null;

        var name = TryGetAbilityName(eventElement);
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var abilityId = TryGetAbilityId(eventElement);
        var elapsedSeconds = Math.Max(0, (timestamp.Value - fightStartTime) / 1000d);
        var sourceType = dataType.Equals("Damage", StringComparison.OrdinalIgnoreCase)
            ? "FFLogsDamage"
            : "FFLogsCast";
        return new ImportedTimelineEvent(name.Trim(), elapsedSeconds, abilityId, sourceType);
    }

    private static string TryGetAbilityName(JsonElement eventElement)
    {
        if (eventElement.TryGetProperty("ability", out var abilityElement) &&
            abilityElement.ValueKind == JsonValueKind.Object &&
            abilityElement.TryGetProperty("name", out var nameElement))
            return nameElement.GetString() ?? string.Empty;

        if (eventElement.TryGetProperty("abilityName", out var abilityNameElement))
            return abilityNameElement.GetString() ?? string.Empty;

        return string.Empty;
    }

    private static uint TryGetAbilityId(JsonElement eventElement)
    {
        if (eventElement.TryGetProperty("abilityGameID", out var abilityGameIdElement) &&
            abilityGameIdElement.ValueKind == JsonValueKind.Number &&
            abilityGameIdElement.TryGetUInt32(out var abilityGameId))
            return abilityGameId;

        if (eventElement.TryGetProperty("ability", out var abilityElement) &&
            abilityElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in new[] { "gameID", "guid", "id" })
            {
                if (abilityElement.TryGetProperty(propertyName, out var idElement) &&
                    idElement.ValueKind == JsonValueKind.Number &&
                    idElement.TryGetUInt32(out var id))
                    return id;
            }
        }

        return 0;
    }

    private static double? TryGetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
            return null;

        return property.GetDouble();
    }

    private static FflogsReportLocation ParseReportLocation(string input)
    {
        var text = input.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return new FflogsReportLocation(string.Empty, 0);

        if (Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var reportsIndex = Array.FindIndex(parts, part => part.Equals("reports", StringComparison.OrdinalIgnoreCase));
            var reportCode = string.Empty;
            if (reportsIndex >= 0 && reportsIndex + 1 < parts.Length)
                reportCode = parts[reportsIndex + 1].Trim();

            var fightId = TryExtractFightId($"{uri.Query}&{uri.Fragment}");
            return new FflogsReportLocation(reportCode, fightId);
        }

        var hashIndex = text.IndexOf('#', StringComparison.Ordinal);
        if (hashIndex >= 0)
            text = text[..hashIndex];
        var queryIndex = text.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
            text = text[..queryIndex];

        return new FflogsReportLocation(text.Trim().Trim('/'), 0);
    }

    private static int TryExtractFightId(string input)
    {
        foreach (var part in input.Split(['?', '#', '&'], StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 &&
                pair[0].Equals("fight", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(pair[1], out var fightId))
                return fightId;
        }

        return 0;
    }

    private static string MakeTimelineEventKey(ImportedTimelineEvent timelineEvent)
    {
        var timeBucket = Math.Round(timelineEvent.TimeSeconds / 2d) * 2d;
        var abilityKey = timelineEvent.ActionId != 0 ? timelineEvent.ActionId.ToString() : timelineEvent.Name;
        return $"{abilityKey}:{timeBucket:F0}";
    }

    private static void ThrowIfGraphQlError(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Array || errors.GetArrayLength() == 0)
            return;

        var message = errors[0].TryGetProperty("message", out var messageElement)
            ? messageElement.GetString()
            : errors[0].ToString();
        throw new InvalidOperationException($"FFLogs GraphQL 错误：{message}");
    }

    private sealed record FflogsEventPage(string ReportTitle, string FightName, List<ImportedTimelineEvent> Events);

    private sealed record FflogsReportLocation(string ReportCode, int FightId);
}

public sealed record FflogsTimelineImportResult(
    string ReportCode,
    int FightId,
    string ReportTitle,
    string FightName,
    IReadOnlyList<ImportedTimelineEvent> Events);
