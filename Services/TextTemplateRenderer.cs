using WeiyueMistakeTTS.Models;

namespace WeiyueMistakeTTS.Services;

public static class TextTemplateRenderer
{
    public static string Render(
        string template,
        TeamMember? member,
        MechanicDefinition mechanic,
        MistakeRecord record)
    {
        var name = member?.DisplayName;
        if (string.IsNullOrWhiteSpace(name))
            name = record.MemberId;

        return template
            .Replace("{name}", name, StringComparison.OrdinalIgnoreCase)
            .Replace("{job}", member?.Job ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{mechanic}", mechanic.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{type}", record.MistakeType, StringComparison.OrdinalIgnoreCase)
            .Replace("{note}", record.Note, StringComparison.OrdinalIgnoreCase)
            .Replace("{count}", record.Count.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

