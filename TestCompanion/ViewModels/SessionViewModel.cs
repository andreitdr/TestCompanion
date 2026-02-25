using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TestCompanion.Models;
using TestCompanion.Services;

namespace TestCompanion.ViewModels;

public class AreaLevel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<AreaLevel>? SelectionChanged;

    private AreaNode? _selectedNode;

    public ObservableCollection<AreaNode> Options { get; } = new();

    public AreaNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (_selectedNode != value)
            {
                _selectedNode = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedNode)));
                SelectionChanged?.Invoke(this);
            }
        }
    }
}

public class SessionViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly AutoSaveService _autoSave;
    private readonly CoverageIniParser _coverageParser;
    private readonly ReportGeneratorService _reportGenerator;
    private List<AreaNode> _areaRoots = new();

    private DispatcherTimer? _durationTimer;
    private DispatcherTimer? _autoSaveTimer;
    private DateTime _lastActiveTime;
    private bool _isActive = true;
    private TimeSpan _accumulatedDuration;

    // Fields
    private string _title = string.Empty;
    private string _startTime = string.Empty;
    private DateTime _startTimeUtc;
    private string _durationDisplay = "0m 0s";
    private string _durationCategory = "SMALL";
    private string _testerNames = string.Empty;
    private double _sessionSetupPercent;
    private double _testDesignPercent;
    private double _bugInvestigationPercent;
    private string _taskBreakdownError = string.Empty;
    private double _charterPercent;
    private double _opportunityPercent;
    private bool _isUpdatingCharter;
    private string _testNotes = string.Empty;
    private string _validationMessage = string.Empty;
    private bool _hasValidationError;
    private string _statusMessage = string.Empty;
    private bool _isReadOnly;
    private string _openedFilePath = string.Empty;

    public SessionViewModel()
    {
        _autoSave = new AutoSaveService();
        _coverageParser = new CoverageIniParser();
        _reportGenerator = new ReportGeneratorService();

        Bugs = new ObservableCollection<BugEntryViewModel>();
        Issues = new ObservableCollection<IssueEntryViewModel>();
        AttachedFiles = new ObservableCollection<string>();
        AreaLevels = new ObservableCollection<AreaLevel>();

        AddBugCommand = new RelayCommand(AddBug);
        AddIssueCommand = new RelayCommand(AddIssue);
        RemoveBugCommand = new RelayCommand<BugEntryViewModel>(RemoveBug);
        RemoveIssueCommand = new RelayCommand<IssueEntryViewModel>(RemoveIssue);
        RemoveFileCommand = new RelayCommand<string>(RemoveFile);
        SubmitCommand = new RelayCommand(Submit);
        ClearCommand = new RelayCommand(ClearSession);
        EnableEditingCommand = new RelayCommand(EnableEditing);

        AttachedFiles.CollectionChanged += (_, _) => ScheduleAutoSave();

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        // Load coverage.ini
        await LoadCoverageAsync();

        // Try to restore from cache
        var cached = _autoSave.Load();
        if (cached != null)
        {
            RestoreFromModel(cached);
        }
        else
        {
            InitializeNewSession();
        }

        // Start duration timer
        _lastActiveTime = DateTime.UtcNow;
        _durationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _durationTimer.Tick += OnDurationTick;
        _durationTimer.Start();

        // Auto-save timer (debounced)
        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _autoSaveTimer.Tick += OnAutoSaveTick;
    }

    private async Task LoadCoverageAsync()
    {
        try
        {
            // Try multiple paths
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "coverage.ini"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coverage.ini"),
                "coverage.ini"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _areaRoots = await _coverageParser.LoadFromFileAsync(path);
                    break;
                }
            }

            if (_areaRoots.Count == 0)
            {
                // Fallback: parse embedded sample
                _areaRoots = _coverageParser.Parse(GetDefaultCoverage());
            }

            // Initialize first level
            var firstLevel = new AreaLevel();
            foreach (var root in _areaRoots)
                firstLevel.Options.Add(root);
            firstLevel.SelectionChanged += OnAreaSelectionChanged;
            AreaLevels.Add(firstLevel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load coverage.ini: {ex.Message}");
        }
    }

    private string GetDefaultCoverage()
    {
        return @"Web | Authentication | Login
Web | Authentication | Logout
Web | Authentication | OAuth | Google
Web | Authentication | OAuth | GitHub
Web | Dashboard | Widgets
Web | Dashboard | Settings
Web | Profile | Edit
Web | Profile | Avatar
API | REST | GET
API | REST | POST
API | REST | PUT
API | REST | DELETE
API | GraphQL | Queries
API | GraphQL | Mutations
Mobile | iOS | Navigation
Mobile | iOS | Push Notifications
Mobile | Android | Navigation
Mobile | Android | Push Notifications
Database | Queries | Performance
Database | Migrations";
    }

    private void InitializeNewSession()
    {
        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;
        _startTimeUtc = utcNow;

        var utcOffset = TimeZoneInfo.Local.GetUtcOffset(now);
        int offsetHours = (int)utcOffset.TotalHours;

        string offsetStr;
        if (offsetHours == 0)
            offsetStr = "UTC";
        else if (offsetHours > 0)
            offsetStr = $"UTC+{offsetHours}";
        else
            offsetStr = $"UTC{offsetHours}";

        _startTime = $"{now:dd/MM/yyyy hh:mm tt} {offsetStr}";
        OnPropertyChanged(nameof(StartTime));

        _accumulatedDuration = TimeSpan.Zero;
    }

    #region Properties

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); ScheduleAutoSave(); }
    }

    public string StartTime
    {
        get => _startTime;
        set { _startTime = value; OnPropertyChanged(); }
    }

    public string DurationDisplay
    {
        get => _durationDisplay;
        set { _durationDisplay = value; OnPropertyChanged(); }
    }

    public string DurationCategory
    {
        get => _durationCategory;
        set { _durationCategory = value; OnPropertyChanged(); }
    }

    public string TesterNames
    {
        get => _testerNames;
        set { _testerNames = value; OnPropertyChanged(); ScheduleAutoSave(); }
    }

    public double SessionSetupPercent
    {
        get => _sessionSetupPercent;
        set
        {
            _sessionSetupPercent = Math.Round(Math.Clamp(value, 0, 100), 1);
            OnPropertyChanged();
            ValidateTaskBreakdown();
            ScheduleAutoSave();
        }
    }

    public double TestDesignPercent
    {
        get => _testDesignPercent;
        set
        {
            _testDesignPercent = Math.Round(Math.Clamp(value, 0, 100), 1);
            OnPropertyChanged();
            ValidateTaskBreakdown();
            ScheduleAutoSave();
        }
    }

    public double BugInvestigationPercent
    {
        get => _bugInvestigationPercent;
        set
        {
            _bugInvestigationPercent = Math.Round(Math.Clamp(value, 0, 100), 1);
            OnPropertyChanged();
            ValidateTaskBreakdown();
            ScheduleAutoSave();
        }
    }

    public string TaskBreakdownError
    {
        get => _taskBreakdownError;
        set { _taskBreakdownError = value; OnPropertyChanged(); }
    }

    public double CharterPercent
    {
        get => _charterPercent;
        set
        {
            if (_isUpdatingCharter) return;
            _isUpdatingCharter = true;
            _charterPercent = Math.Round(Math.Clamp(value, 0, 100), 1);
            _opportunityPercent = Math.Round(100 - _charterPercent, 1);
            OnPropertyChanged();
            OnPropertyChanged(nameof(OpportunityPercent));
            _isUpdatingCharter = false;
            ScheduleAutoSave();
        }
    }

    public double OpportunityPercent
    {
        get => _opportunityPercent;
        set
        {
            if (_isUpdatingCharter) return;
            _isUpdatingCharter = true;
            _opportunityPercent = Math.Round(Math.Clamp(value, 0, 100), 1);
            _charterPercent = Math.Round(100 - _opportunityPercent, 1);
            OnPropertyChanged();
            OnPropertyChanged(nameof(CharterPercent));
            _isUpdatingCharter = false;
            ScheduleAutoSave();
        }
    }

    public string TestNotes
    {
        get => _testNotes;
        set { _testNotes = value; OnPropertyChanged(); ScheduleAutoSave(); }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        set { _validationMessage = value; OnPropertyChanged(); }
    }

    public bool HasValidationError
    {
        get => _hasValidationError;
        set { _hasValidationError = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set { _isReadOnly = value; OnPropertyChanged(); }
    }

    public string OpenedFilePath
    {
        get => _openedFilePath;
        set { _openedFilePath = value; OnPropertyChanged(); }
    }


    public ObservableCollection<AreaLevel> AreaLevels { get; }
    public ObservableCollection<BugEntryViewModel> Bugs { get; }
    public ObservableCollection<IssueEntryViewModel> Issues { get; }
    public ObservableCollection<string> AttachedFiles { get; }

    #endregion

    #region Commands

    public ICommand AddBugCommand { get; }
    public ICommand AddIssueCommand { get; }
    public ICommand RemoveBugCommand { get; }
    public ICommand RemoveIssueCommand { get; }
    public ICommand RemoveFileCommand { get; }
    public ICommand SubmitCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand EnableEditingCommand { get; }

    #endregion

    #region Area Cascading Logic

    private bool _isUpdatingAreas;

    private void OnAreaSelectionChanged(AreaLevel changedLevel)
    {
        if (_isUpdatingAreas) return;
        _isUpdatingAreas = true;

        try
        {
            var index = AreaLevels.IndexOf(changedLevel);
            if (index < 0) return;

            // Remove all levels after this one
            while (AreaLevels.Count > index + 1)
                AreaLevels.RemoveAt(AreaLevels.Count - 1);

            // If the selected node has children, add a new dropdown level
            if (changedLevel.SelectedNode != null && changedLevel.SelectedNode.Children.Count > 0)
            {
                var nextLevel = new AreaLevel();
                foreach (var child in changedLevel.SelectedNode.Children)
                    nextLevel.Options.Add(child);
                nextLevel.SelectionChanged += OnAreaSelectionChanged;
                AreaLevels.Add(nextLevel);
            }

            ScheduleAutoSave();
        }
        finally
        {
            _isUpdatingAreas = false;
        }
    }

    public List<string> GetAreaSelections()
    {
        var selections = new List<string>();
        foreach (var level in AreaLevels)
        {
            if (level.SelectedNode != null)
                selections.Add(level.SelectedNode.Name);
        }
        return selections;
    }

    #endregion

    #region Duration Tracking

    private void OnDurationTick(object? sender, object e)
    {
        if (_isActive)
        {
            var now = DateTime.UtcNow;
            _accumulatedDuration += now - _lastActiveTime;
            _lastActiveTime = now;

            DurationDisplay = _reportGenerator.FormatDuration(_accumulatedDuration);
            DurationCategory = _reportGenerator.GetDurationCategory(_accumulatedDuration);
        }
    }

    public void OnAppActivated()
    {
        _isActive = true;
        _lastActiveTime = DateTime.UtcNow;
        _durationTimer?.Start();
    }

    public void OnAppDeactivated()
    {
        if (_isActive)
        {
            _isActive = false;
            // Add remaining time
            _accumulatedDuration += DateTime.UtcNow - _lastActiveTime;
            _durationTimer?.Stop();
            PerformAutoSave();
        }
    }

    #endregion

    #region Task Breakdown Validation

    private void ValidateTaskBreakdown()
    {
        var sum = _sessionSetupPercent + _testDesignPercent + _bugInvestigationPercent;
        // Allow exactly 100 or 99.9-100.1 for floating point rounding (e.g., 33.3 * 3 = 99.9)
        if (sum >= 99.9 && sum <= 100.1)
        {
            TaskBreakdownError = string.Empty;
        }
        else
        {
            TaskBreakdownError = $"Sum is {sum:F1}%. Must equal 100% (current: {sum:F1}%)";
        }
    }

    #endregion

    #region Bug/Issue Management

    private void AddBug()
    {
        var bug = new BugEntryViewModel(AttachedFiles.ToList());
        bug.Changed += ScheduleAutoSave;
        Bugs.Add(bug);
        ScheduleAutoSave();
    }

    private void AddIssue()
    {
        var issue = new IssueEntryViewModel();
        issue.Changed += ScheduleAutoSave;
        Issues.Add(issue);
        ScheduleAutoSave();
    }

    private void RemoveBug(BugEntryViewModel? bug)
    {
        if (bug != null)
        {
            Bugs.Remove(bug);
            ScheduleAutoSave();
        }
    }

    private void RemoveIssue(IssueEntryViewModel? issue)
    {
        if (issue != null)
        {
            Issues.Remove(issue);
            ScheduleAutoSave();
        }
    }

    #endregion

    #region File Management

    public void AddFiles(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            if (!AttachedFiles.Contains(path))
                AttachedFiles.Add(path);
        }
        ScheduleAutoSave();
    }

    private void RemoveFile(string? file)
    {
        if (file != null)
            AttachedFiles.Remove(file);
    }

    #endregion

    #region Auto Save

    private void ScheduleAutoSave()
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Start();
    }

    private void OnAutoSaveTick(object? sender, object e)
    {
        _autoSaveTimer?.Stop();
        PerformAutoSave();
    }

    public void PerformAutoSave()
    {
        var model = BuildModel();
        model.AccumulatedDurationTicks = _accumulatedDuration.Ticks;
        model.LastActiveTimestamp = DateTime.UtcNow;
        _autoSave.Save(model);
    }

    private void RestoreFromModel(SessionModel model)
    {
        _title = model.Title;
        _startTime = model.StartTime;
        _startTimeUtc = model.StartTimeUtc;
        _accumulatedDuration = TimeSpan.FromTicks(model.AccumulatedDurationTicks);
        _testerNames = model.TesterNames;
        _sessionSetupPercent = model.SessionSetupPercent;
        _testDesignPercent = model.TestDesignExecutionPercent;
        _bugInvestigationPercent = model.BugInvestigationPercent;
        _charterPercent = model.CharterPercent;
        _opportunityPercent = model.OpportunityPercent;
        _testNotes = model.TestNotes;

        AttachedFiles.Clear();
        foreach (var f in model.AttachedFiles)
            AttachedFiles.Add(f);

        // Restore area selections
        RestoreAreaSelections(model.AreaSelections);

        // Restore bugs
        Bugs.Clear();
        foreach (var bugModel in model.Bugs)
        {
            var vm = new BugEntryViewModel(AttachedFiles.ToList());
            vm.LoadFrom(bugModel);
            vm.Changed += ScheduleAutoSave;
            Bugs.Add(vm);
        }

        // Restore issues
        Issues.Clear();
        foreach (var issueModel in model.Issues)
        {
            var vm = new IssueEntryViewModel();
            vm.LoadFrom(issueModel);
            vm.Changed += ScheduleAutoSave;
            Issues.Add(vm);
        }

        ValidateTaskBreakdown();

        // Notify all properties
        OnPropertyChanged(string.Empty);
    }

    private void RestoreAreaSelections(List<string> selections)
    {
        if (selections == null || selections.Count == 0) return;

        _isUpdatingAreas = true;
        try
        {
            // Clear existing levels beyond the first
            while (AreaLevels.Count > 1)
                AreaLevels.RemoveAt(AreaLevels.Count - 1);

            var currentNodes = _areaRoots;

            for (int i = 0; i < selections.Count; i++)
            {
                AreaLevel level;
                if (i < AreaLevels.Count)
                {
                    level = AreaLevels[i];
                }
                else
                {
                    level = new AreaLevel();
                    foreach (var node in currentNodes)
                        level.Options.Add(node);
                    level.SelectionChanged += OnAreaSelectionChanged;
                    AreaLevels.Add(level);
                }

                var match = level.Options.FirstOrDefault(n => n.Name == selections[i]);
                if (match != null)
                {
                    level.SelectedNode = match;
                    currentNodes = match.Children;
                }
                else
                {
                    break;
                }
            }

            // If last selected node has children, add an empty level for next selection
            var lastLevel = AreaLevels.LastOrDefault();
            if (lastLevel?.SelectedNode?.Children.Count > 0)
            {
                var nextLevel = new AreaLevel();
                foreach (var child in lastLevel.SelectedNode.Children)
                    nextLevel.Options.Add(child);
                nextLevel.SelectionChanged += OnAreaSelectionChanged;
                AreaLevels.Add(nextLevel);
            }
        }
        finally
        {
            _isUpdatingAreas = false;
        }
    }

    #endregion

    #region Validation & Submit

    private bool Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Title is required.");

        if (GetAreaSelections().Count == 0)
            errors.Add("At least one area must be selected.");

        if (string.IsNullOrWhiteSpace(TesterNames))
            errors.Add("Tester name(s) required.");

        var taskSum = SessionSetupPercent + TestDesignPercent + BugInvestigationPercent;
        if (taskSum < 99.9 || taskSum > 100.1)
            errors.Add($"Task breakdown must sum to 100% (currently {taskSum:F1}%).");

        var charterOppSum = CharterPercent + OpportunityPercent;
        if (charterOppSum < 99.9 || charterOppSum > 100.1)
            errors.Add($"Charter + Opportunity must sum to 100% (currently {charterOppSum:F1}%).");

        if (string.IsNullOrWhiteSpace(TestNotes))
            errors.Add("Test notes are required.");

        // Validate each bug
        for (int i = 0; i < Bugs.Count; i++)
        {
            var b = Bugs[i];
            if (string.IsNullOrWhiteSpace(b.Title))
                errors.Add($"Bug #{i + 1}: Title is required.");
            if (string.IsNullOrWhiteSpace(b.Description))
                errors.Add($"Bug #{i + 1}: Description is required.");
            if (string.IsNullOrWhiteSpace(b.Result))
                errors.Add($"Bug #{i + 1}: Result is required.");
            if (string.IsNullOrWhiteSpace(b.ExpectedResult))
                errors.Add($"Bug #{i + 1}: Expected Result is required.");
        }

        // Validate each issue
        for (int i = 0; i < Issues.Count; i++)
        {
            var iss = Issues[i];
            if (string.IsNullOrWhiteSpace(iss.Title))
                errors.Add($"Issue #{i + 1}: Title is required.");
            if (string.IsNullOrWhiteSpace(iss.Description))
                errors.Add($"Issue #{i + 1}: Description is required.");
        }

        if (errors.Count > 0)
        {
            ValidationMessage = string.Join("\n", errors);
            HasValidationError = true;
            return false;
        }

        ValidationMessage = string.Empty;
        HasValidationError = false;
        return true;
    }

    private async void Submit()
    {
        // Submit is now handled by the code-behind which shows a format picker dialog.
        // This is kept as a fallback for the menu item command binding.
        await ExportWithFormat(null);
    }

    /// <summary>
    /// Validates and exports the report. If format is null, reads the default from settings.
    /// Returns true if export succeeded.
    /// </summary>
    public async Task<bool> ExportWithFormat(ExportFormat? format)
    {
        if (!Validate()) return false;

        var freshSettings = new SettingsService();
        var exportFormat = format ?? freshSettings.GetExportFormat();
        var model = BuildModel();
        var report = _reportGenerator.GenerateReport(model, _accumulatedDuration, exportFormat);

        try
        {
            var reportsFolder = freshSettings.GetExportPath();
            Directory.CreateDirectory(reportsFolder);

            var sanitizedTitle = string.Join("_", model.Title.Split(Path.GetInvalidFileNameChars()));
            var extension = freshSettings.GetFileExtension(exportFormat);
            var fileName = $"Session_{sanitizedTitle}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            var filePath = Path.Combine(reportsFolder, fileName);

            await File.WriteAllTextAsync(filePath, report);

            // Clear the cache after successful save
            _autoSave.ClearCache();

            StatusMessage = $"Report saved to: {filePath}";
            HasValidationError = false;
            ValidationMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving report: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Runs validation and returns true if the form is valid.
    /// </summary>
    public bool RunValidation() => Validate();


    /// <summary>
    /// Loads a previously exported JSON report into the form in read-only mode.
    /// The accumulated duration is preserved and will continue to add up when editing is enabled.
    /// </summary>
    public void LoadFromReport(SessionModel model, string filePath)
    {
        RestoreFromModel(model);
        OpenedFilePath = filePath;
        IsReadOnly = true;
        StatusMessage = $"Opened report: {Path.GetFileName(filePath)} (read-only)";
        HasValidationError = false;
        ValidationMessage = string.Empty;

        // Stop the timer while in read-only mode
        _durationTimer?.Stop();
    }

    private void EnableEditing()
    {
        IsReadOnly = false;
        StatusMessage = "Editing enabled. Duration timer resumed.";

        // Resume the duration timer — time continues to add up
        _lastActiveTime = DateTime.UtcNow;
        _isActive = true;
        _durationTimer?.Start();
    }

    private void ClearSession()
    {
        _autoSave.ClearCache();
        _title = string.Empty;
        _testerNames = string.Empty;
        _sessionSetupPercent = 0;
        _testDesignPercent = 0;
        _bugInvestigationPercent = 0;
        _charterPercent = 0;
        _opportunityPercent = 0;
        _testNotes = string.Empty;
        _validationMessage = string.Empty;
        _hasValidationError = false;
        _statusMessage = string.Empty;
        _isReadOnly = false;
        _openedFilePath = string.Empty;

        AttachedFiles.Clear();
        Bugs.Clear();
        Issues.Clear();

        // Reset areas
        while (AreaLevels.Count > 1)
            AreaLevels.RemoveAt(AreaLevels.Count - 1);
        if (AreaLevels.Count > 0)
            AreaLevels[0].SelectedNode = null;

        InitializeNewSession();
        _accumulatedDuration = TimeSpan.Zero;
        DurationDisplay = "0m 0s";
        DurationCategory = "SMALL";
        TaskBreakdownError = string.Empty;

        // Restart the duration timer
        _lastActiveTime = DateTime.UtcNow;
        _isActive = true;
        _durationTimer?.Start();

        OnPropertyChanged(string.Empty);
    }

    private SessionModel BuildModel()
    {
        return new SessionModel
        {
            Title = Title,
            AreaSelections = GetAreaSelections(),
            StartTime = StartTime,
            StartTimeUtc = _startTimeUtc,
            AccumulatedDurationTicks = _accumulatedDuration.Ticks,
            LastActiveTimestamp = DateTime.UtcNow,
            TesterNames = TesterNames,
            SessionSetupPercent = SessionSetupPercent,
            TestDesignExecutionPercent = TestDesignPercent,
            BugInvestigationPercent = BugInvestigationPercent,
            CharterPercent = CharterPercent,
            OpportunityPercent = OpportunityPercent,
            AttachedFiles = AttachedFiles.ToList(),
            TestNotes = TestNotes,
            Bugs = Bugs.Select(b => b.ToModel()).ToList(),
            Issues = Issues.Select(i => i.ToModel()).ToList()
        };
    }

    #endregion

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
}

