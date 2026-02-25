using System.Text.Json.Serialization;

namespace TestCompanion.Models;

public class BugEntry
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> RelatedFiles { get; set; } = new();
    public string Result { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
}

