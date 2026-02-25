namespace TestCompanion.Models;

public class AreaNode
{
    public string Name { get; set; } = string.Empty;
    public List<AreaNode> Children { get; set; } = new();

    public override string ToString() => Name;
}
