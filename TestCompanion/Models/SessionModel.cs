namespace TestCompanion.Models;

public class SessionModel
{
    public string Title { get; set; } = string.Empty;
    public List<string> AreaSelections { get; set; } = new();
    public string StartTime { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public long AccumulatedDurationTicks { get; set; }
    public DateTime? LastActiveTimestamp { get; set; }
    public string TesterNames { get; set; } = string.Empty;

    // Task breakdown percentages
    public double SessionSetupPercent { get; set; }
    public double TestDesignExecutionPercent { get; set; }
    public double BugInvestigationPercent { get; set; }

    // Charter vs Opportunity
    public double CharterPercent { get; set; }
    public double OpportunityPercent { get; set; }

    // Files
    public List<string> AttachedFiles { get; set; } = new();

    // Test notes
    public string TestNotes { get; set; } = string.Empty;

    // Bugs and Issues
    public List<BugEntry> Bugs { get; set; } = new();
    public List<IssueEntry> Issues { get; set; } = new();
}

