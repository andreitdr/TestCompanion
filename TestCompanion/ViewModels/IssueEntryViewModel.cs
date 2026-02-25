using System.ComponentModel;
using TestCompanion.Models;

namespace TestCompanion.ViewModels;

public class IssueEntryViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;

    private string _title = string.Empty;
    private string _description = string.Empty;

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

    public IssueEntry ToModel()
    {
        return new IssueEntry
        {
            Title = Title,
            Description = Description
        };
    }

    public void LoadFrom(IssueEntry entry)
    {
        _title = entry.Title;
        _description = entry.Description;
        OnPropertyChanged(string.Empty);
    }

    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

