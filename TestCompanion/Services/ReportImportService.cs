using System.Text.Json;
using TestCompanion.Models;

namespace TestCompanion.Services;

/// <summary>
/// Parses exported JSON reports back into a SessionModel.
/// </summary>
public class ReportImportService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Attempts to parse a JSON report file into a SessionModel.
    /// Returns null if parsing fails.
    /// </summary>
    public SessionModel? ImportFromJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var model = new SessionModel
            {
                Title = GetString(root, "title"),
                StartTime = GetString(root, "start"),
                TesterNames = GetString(root, "testers"),
                TestNotes = GetString(root, "testNotes"),
                CharterPercent = GetDouble(root, "charter"),
                OpportunityPercent = GetDouble(root, "opportunity"),
            };

            // Areas
            if (root.TryGetProperty("areas", out var areas) && areas.ValueKind == JsonValueKind.Array)
            {
                foreach (var area in areas.EnumerateArray())
                    model.AreaSelections.Add(area.GetString() ?? string.Empty);
            }

            // Task breakdown
            if (root.TryGetProperty("taskBreakdown", out var tb))
            {
                model.SessionSetupPercent = GetDouble(tb, "sessionSetup");
                model.TestDesignExecutionPercent = GetDouble(tb, "testDesignExecution");
                model.BugInvestigationPercent = GetDouble(tb, "bugInvestigationReporting");
            }

            // Attached files
            if (root.TryGetProperty("attachedFiles", out var files) && files.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in files.EnumerateArray())
                    model.AttachedFiles.Add(f.GetString() ?? string.Empty);
            }

            // Duration - parse the formatted string back to ticks
            var durationStr = GetString(root, "duration");
            model.AccumulatedDurationTicks = ParseDuration(durationStr).Ticks;

            // Bugs
            if (root.TryGetProperty("bugs", out var bugs) && bugs.ValueKind == JsonValueKind.Array)
            {
                foreach (var b in bugs.EnumerateArray())
                {
                    model.Bugs.Add(new BugEntry
                    {
                        Title = GetString(b, "title"),
                        Description = GetString(b, "description"),
                        Result = GetString(b, "result"),
                        ExpectedResult = GetString(b, "expectedResult"),
                        RelatedFiles = GetStringList(b, "relatedFiles")
                    });
                }
            }

            // Issues
            if (root.TryGetProperty("issues", out var issues) && issues.ValueKind == JsonValueKind.Array)
            {
                foreach (var iss in issues.EnumerateArray())
                {
                    model.Issues.Add(new IssueEntry
                    {
                        Title = GetString(iss, "title"),
                        Description = GetString(iss, "description")
                    });
                }
            }

            return model;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON import failed: {ex.Message}");
            return null;
        }
    }

    private static string GetString(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String)
            return val.GetString() ?? string.Empty;
        return string.Empty;
    }

    private static double GetDouble(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number)
            return val.GetDouble();
        return 0;
    }

    private static List<string> GetStringList(JsonElement el, string prop)
    {
        var list = new List<string>();
        if (el.TryGetProperty(prop, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
                list.Add(item.GetString() ?? string.Empty);
        }
        return list;
    }

    /// <summary>
    /// Parses duration strings like "1h 23m 45s", "5m 30s", "12s" back to TimeSpan.
    /// </summary>
    private static TimeSpan ParseDuration(string formatted)
    {
        if (string.IsNullOrWhiteSpace(formatted))
            return TimeSpan.Zero;

        int hours = 0, minutes = 0, seconds = 0;

        var parts = formatted.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (part.EndsWith("h") && int.TryParse(part.TrimEnd('h'), out var h))
                hours = h;
            else if (part.EndsWith("m") && int.TryParse(part.TrimEnd('m'), out var m))
                minutes = m;
            else if (part.EndsWith("s") && int.TryParse(part.TrimEnd('s'), out var s))
                seconds = s;
        }

        return new TimeSpan(hours, minutes, seconds);
    }
}

