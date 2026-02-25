namespace TestCompanion.Models;

public enum ExportFormat
{
    PlainText,
    Markdown,
    Html,
    Json
}

public class AppSettings
{
    public string ExportPath { get; set; } = string.Empty;
    public ExportFormat LastExportFormat { get; set; } = ExportFormat.PlainText;
}

