using System.Text;
using System.Text.Json;
using TestCompanion.Models;

namespace TestCompanion.Services;

public class ReportGeneratorService
{
    public string GenerateReport(SessionModel model, TimeSpan activeDuration, ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Markdown => GenerateMarkdown(model, activeDuration),
            ExportFormat.Html => GenerateHtml(model, activeDuration),
            ExportFormat.Json => GenerateJson(model, activeDuration),
            _ => GeneratePlainText(model, activeDuration)
        };
    }

    // Keep backward compat
    public string GenerateReport(SessionModel model, TimeSpan activeDuration)
        => GeneratePlainText(model, activeDuration);

    #region Plain Text

    private string GeneratePlainText(SessionModel model, TimeSpan activeDuration)
    {
        var sb = new StringBuilder();
        var separator = new string('=', 60);
        var thinSep = new string('-', 60);

        sb.AppendLine(separator);
        sb.AppendLine("              TESTING SESSION REPORT");
        sb.AppendLine(separator);
        sb.AppendLine();

        sb.AppendLine($"Title: {model.Title}");
        sb.AppendLine();

        sb.AppendLine("Areas Covered:");
        if (model.AreaSelections.Count > 0)
            sb.AppendLine($"  {string.Join(" > ", model.AreaSelections)}");
        sb.AppendLine();

        sb.AppendLine($"Start: {model.StartTime}");
        sb.AppendLine();

        var durationCategory = GetDurationCategory(activeDuration);
        sb.AppendLine($"Duration: {FormatDuration(activeDuration)} ({durationCategory})");
        sb.AppendLine();

        sb.AppendLine($"Tester(s): {model.TesterNames}");
        sb.AppendLine();

        sb.AppendLine("Task Breakdown:");
        sb.AppendLine($"  Session Setup:                 {model.SessionSetupPercent:F1}%");
        sb.AppendLine($"  Test Design & Execution:       {model.TestDesignExecutionPercent:F1}%");
        sb.AppendLine($"  Bug Investigation & Reporting: {model.BugInvestigationPercent:F1}%");
        sb.AppendLine();

        sb.AppendLine($"Charter: {model.CharterPercent:F1}%  |  Opportunity: {model.OpportunityPercent:F1}%");
        sb.AppendLine();

        sb.AppendLine("Attached Files:");
        if (model.AttachedFiles.Count == 0)
            sb.AppendLine("  (none)");
        else
            for (int i = 0; i < model.AttachedFiles.Count; i++)
                sb.AppendLine($"  [{i + 1}] {model.AttachedFiles[i]}");
        sb.AppendLine();

        sb.AppendLine("Test Notes:");
        sb.AppendLine(thinSep);
        sb.AppendLine(model.TestNotes);
        sb.AppendLine(thinSep);
        sb.AppendLine();

        sb.AppendLine(separator);
        sb.AppendLine($"BUGS ({model.Bugs.Count})");
        sb.AppendLine(separator);
        for (int i = 0; i < model.Bugs.Count; i++)
        {
            var bug = model.Bugs[i];
            sb.AppendLine($"\n  Bug #{i + 1}:");
            sb.AppendLine($"    Title:           {bug.Title}");
            sb.AppendLine($"    Description:");
            foreach (var line in bug.Description.Split('\n'))
                sb.AppendLine($"                     {line.TrimEnd('\r')}");
            sb.AppendLine($"    Result:");
            foreach (var line in bug.Result.Split('\n'))
                sb.AppendLine($"                     {line.TrimEnd('\r')}");
            sb.AppendLine($"    Expected Result:");
            foreach (var line in bug.ExpectedResult.Split('\n'))
                sb.AppendLine($"                     {line.TrimEnd('\r')}");
            if (bug.RelatedFiles.Count > 0)
                sb.AppendLine($"    Related Files:   {string.Join(", ", bug.RelatedFiles)}");
            sb.AppendLine(thinSep);
        }
        sb.AppendLine();

        sb.AppendLine(separator);
        sb.AppendLine($"ISSUES ({model.Issues.Count})");
        sb.AppendLine(separator);
        for (int i = 0; i < model.Issues.Count; i++)
        {
            var issue = model.Issues[i];
            sb.AppendLine($"\n  Issue #{i + 1}:");
            sb.AppendLine($"    Title:           {issue.Title}");
            sb.AppendLine($"    Description:");
            foreach (var line in issue.Description.Split('\n'))
                sb.AppendLine($"                     {line.TrimEnd('\r')}");
            sb.AppendLine(thinSep);
        }
        sb.AppendLine();
        sb.AppendLine(separator);
        sb.AppendLine($"Report generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine(separator);

        return sb.ToString();
    }

    #endregion

    #region Markdown

    private string GenerateMarkdown(SessionModel model, TimeSpan activeDuration)
    {
        var sb = new StringBuilder();
        var durationCategory = GetDurationCategory(activeDuration);

        sb.AppendLine($"# Testing Session Report");
        sb.AppendLine();
        sb.AppendLine($"## {model.Title}");
        sb.AppendLine();
        sb.AppendLine($"| Field | Value |");
        sb.AppendLine($"|-------|-------|");
        sb.AppendLine($"| **Areas** | {string.Join(" > ", model.AreaSelections)} |");
        sb.AppendLine($"| **Start** | {model.StartTime} |");
        sb.AppendLine($"| **Duration** | {FormatDuration(activeDuration)} ({durationCategory}) |");
        sb.AppendLine($"| **Tester(s)** | {model.TesterNames} |");
        sb.AppendLine();

        sb.AppendLine($"### Task Breakdown");
        sb.AppendLine();
        sb.AppendLine($"| Task | % |");
        sb.AppendLine($"|------|---|");
        sb.AppendLine($"| Session Setup | {model.SessionSetupPercent:F1}% |");
        sb.AppendLine($"| Test Design & Execution | {model.TestDesignExecutionPercent:F1}% |");
        sb.AppendLine($"| Bug Investigation & Reporting | {model.BugInvestigationPercent:F1}% |");
        sb.AppendLine();

        sb.AppendLine($"**Charter:** {model.CharterPercent:F1}% | **Opportunity:** {model.OpportunityPercent:F1}%");
        sb.AppendLine();

        sb.AppendLine($"### Attached Files");
        sb.AppendLine();
        if (model.AttachedFiles.Count == 0)
            sb.AppendLine("_(none)_");
        else
            foreach (var f in model.AttachedFiles)
                sb.AppendLine($"- `{f}`");
        sb.AppendLine();

        sb.AppendLine($"### Test Notes");
        sb.AppendLine();
        sb.AppendLine(model.TestNotes);
        sb.AppendLine();

        sb.AppendLine($"---");
        sb.AppendLine();
        sb.AppendLine($"### Bugs ({model.Bugs.Count})");
        sb.AppendLine();
        for (int i = 0; i < model.Bugs.Count; i++)
        {
            var bug = model.Bugs[i];
            sb.AppendLine($"#### Bug #{i + 1}: {bug.Title}");
            sb.AppendLine();
            sb.AppendLine($"**Description:**");
            sb.AppendLine(bug.Description);
            sb.AppendLine();
            sb.AppendLine($"**Result:**");
            sb.AppendLine(bug.Result);
            sb.AppendLine();
            sb.AppendLine($"**Expected Result:**");
            sb.AppendLine(bug.ExpectedResult);
            if (bug.RelatedFiles.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"**Related Files:** {string.Join(", ", bug.RelatedFiles.Select(f => $"`{f}`"))}");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"---");
        sb.AppendLine();
        sb.AppendLine($"### Issues ({model.Issues.Count})");
        sb.AppendLine();
        for (int i = 0; i < model.Issues.Count; i++)
        {
            var issue = model.Issues[i];
            sb.AppendLine($"#### Issue #{i + 1}: {issue.Title}");
            sb.AppendLine();
            sb.AppendLine(issue.Description);
            sb.AppendLine();
        }

        sb.AppendLine($"---");
        sb.AppendLine($"_Report generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC_");

        return sb.ToString();
    }

    #endregion

    #region HTML

    private string GenerateHtml(SessionModel model, TimeSpan activeDuration)
    {
        var sb = new StringBuilder();
        var durationCategory = GetDurationCategory(activeDuration);

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset=\"utf-8\"/>");
        sb.AppendLine($"<title>Session Report - {Escape(model.Title)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:system-ui,sans-serif;max-width:900px;margin:2em auto;padding:0 1em;color:#222}");
        sb.AppendLine("h1{border-bottom:2px solid #333}h2,h3{margin-top:1.5em}");
        sb.AppendLine("table{border-collapse:collapse;width:100%}th,td{border:1px solid #ccc;padding:6px 10px;text-align:left}th{background:#f5f5f5}");
        sb.AppendLine(".bug{border-left:4px solid #e44;padding:0.5em 1em;margin:1em 0;background:#fff8f8}");
        sb.AppendLine(".issue{border-left:4px solid #f80;padding:0.5em 1em;margin:1em 0;background:#fffbf0}");
        sb.AppendLine("pre{background:#f5f5f5;padding:1em;overflow-x:auto;white-space:pre-wrap}");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine($"<h1>Testing Session Report</h1>");
        sb.AppendLine($"<h2>{Escape(model.Title)}</h2>");

        sb.AppendLine("<table><tbody>");
        sb.AppendLine($"<tr><th>Areas</th><td>{Escape(string.Join(" > ", model.AreaSelections))}</td></tr>");
        sb.AppendLine($"<tr><th>Start</th><td>{Escape(model.StartTime)}</td></tr>");
        sb.AppendLine($"<tr><th>Duration</th><td>{FormatDuration(activeDuration)} ({durationCategory})</td></tr>");
        sb.AppendLine($"<tr><th>Tester(s)</th><td>{Escape(model.TesterNames)}</td></tr>");
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<h3>Task Breakdown</h3>");
        sb.AppendLine("<table><thead><tr><th>Task</th><th>%</th></tr></thead><tbody>");
        sb.AppendLine($"<tr><td>Session Setup</td><td>{model.SessionSetupPercent:F1}%</td></tr>");
        sb.AppendLine($"<tr><td>Test Design &amp; Execution</td><td>{model.TestDesignExecutionPercent:F1}%</td></tr>");
        sb.AppendLine($"<tr><td>Bug Investigation &amp; Reporting</td><td>{model.BugInvestigationPercent:F1}%</td></tr>");
        sb.AppendLine("</tbody></table>");

        sb.AppendLine($"<p><strong>Charter:</strong> {model.CharterPercent:F1}% | <strong>Opportunity:</strong> {model.OpportunityPercent:F1}%</p>");

        sb.AppendLine("<h3>Attached Files</h3>");
        if (model.AttachedFiles.Count == 0)
            sb.AppendLine("<p><em>(none)</em></p>");
        else
        {
            sb.AppendLine("<ul>");
            foreach (var f in model.AttachedFiles)
                sb.AppendLine($"<li><code>{Escape(f)}</code></li>");
            sb.AppendLine("</ul>");
        }

        sb.AppendLine("<h3>Test Notes</h3>");
        sb.AppendLine($"<pre>{Escape(model.TestNotes)}</pre>");

        sb.AppendLine($"<h3>Bugs ({model.Bugs.Count})</h3>");
        for (int i = 0; i < model.Bugs.Count; i++)
        {
            var bug = model.Bugs[i];
            sb.AppendLine($"<div class=\"bug\">");
            sb.AppendLine($"<h4>Bug #{i + 1}: {Escape(bug.Title)}</h4>");
            sb.AppendLine($"<p><strong>Description:</strong></p><pre>{Escape(bug.Description)}</pre>");
            sb.AppendLine($"<p><strong>Result:</strong></p><pre>{Escape(bug.Result)}</pre>");
            sb.AppendLine($"<p><strong>Expected Result:</strong></p><pre>{Escape(bug.ExpectedResult)}</pre>");
            if (bug.RelatedFiles.Count > 0)
                sb.AppendLine($"<p><strong>Related Files:</strong> {string.Join(", ", bug.RelatedFiles.Select(f => $"<code>{Escape(f)}</code>"))}</p>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine($"<h3>Issues ({model.Issues.Count})</h3>");
        for (int i = 0; i < model.Issues.Count; i++)
        {
            var issue = model.Issues[i];
            sb.AppendLine($"<div class=\"issue\">");
            sb.AppendLine($"<h4>Issue #{i + 1}: {Escape(issue.Title)}</h4>");
            sb.AppendLine($"<pre>{Escape(issue.Description)}</pre>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine($"<hr/><p><em>Report generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</em></p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    #endregion

    #region JSON

    private string GenerateJson(SessionModel model, TimeSpan activeDuration)
    {
        var report = new
        {
            title = model.Title,
            areas = model.AreaSelections,
            start = model.StartTime,
            duration = FormatDuration(activeDuration),
            durationCategory = GetDurationCategory(activeDuration),
            testers = model.TesterNames,
            taskBreakdown = new
            {
                sessionSetup = model.SessionSetupPercent,
                testDesignExecution = model.TestDesignExecutionPercent,
                bugInvestigationReporting = model.BugInvestigationPercent
            },
            charter = model.CharterPercent,
            opportunity = model.OpportunityPercent,
            attachedFiles = model.AttachedFiles,
            testNotes = model.TestNotes,
            bugs = model.Bugs.Select(b => new
            {
                title = b.Title,
                description = b.Description,
                result = b.Result,
                expectedResult = b.ExpectedResult,
                relatedFiles = b.RelatedFiles
            }),
            issues = model.Issues.Select(iss => new
            {
                title = iss.Title,
                description = iss.Description
            }),
            reportGenerated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
        };

        return JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    #endregion

    public string GetDurationCategory(TimeSpan duration)
    {
        if (duration.TotalHours <= 1) return "SMALL";
        if (duration.TotalHours <= 4) return "MEDIUM";
        return "LONG";
    }

    public string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
        if (duration.TotalMinutes >= 1)
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        return $"{duration.Seconds}s";
    }
}

