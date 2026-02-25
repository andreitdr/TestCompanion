using System.Collections.ObjectModel;
using System.ComponentModel;
using TestCompanion.Models;

namespace TestCompanion.ViewModels;

public class BugEntryViewModel : INotifyPropertyChanged
{
    private readonly List<string> _allFiles;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;

    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _result = string.Empty;
    private string _expectedResult = string.Empty;

    public BugEntryViewModel(List<string> allFiles)
    {
        _allFiles = allFiles;
        SelectedRelatedFiles = new ObservableCollection<string>();
    }

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(nameof(Title)); Changed?.Invoke(); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(nameof(Description)); Changed?.Invoke(); }
    }

    public string Result
    {
        get => _result;
        set { _result = value; OnPropertyChanged(nameof(Result)); Changed?.Invoke(); }
    }

    public string ExpectedResult
    {
        get => _expectedResult;
        set { _expectedResult = value; OnPropertyChanged(nameof(ExpectedResult)); Changed?.Invoke(); }
    }

    public ObservableCollection<string> SelectedRelatedFiles { get; }

    public List<string> AvailableFiles => _allFiles;

    public BugEntry ToModel()
    {
        return new BugEntry
        {
            Title = Title,
            Description = Description,
            RelatedFiles = SelectedRelatedFiles.ToList(),
            Result = Result,
            ExpectedResult = ExpectedResult
        };
    }

    public void LoadFrom(BugEntry entry)
    {
        _title = entry.Title;
        _description = entry.Description;
        _result = entry.Result;
        _expectedResult = entry.ExpectedResult;
        SelectedRelatedFiles.Clear();
        foreach (var f in entry.RelatedFiles)
            SelectedRelatedFiles.Add(f);

        OnPropertyChanged(string.Empty);
    }

    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

