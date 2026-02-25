using TestCompanion.Models;

namespace TestCompanion.Services;

public class CoverageIniParser
{
    public List<AreaNode> Parse(string content)
    {
        var roots = new List<AreaNode>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith(";"))
                continue;

            var parts = line.Split('|').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
            if (parts.Length == 0) continue;

            AddPath(roots, parts, 0);
        }

        return roots;
    }

    private void AddPath(List<AreaNode> nodes, string[] parts, int depth)
    {
        if (depth >= parts.Length) return;

        var name = parts[depth];
        var existing = nodes.FirstOrDefault(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            existing = new AreaNode { Name = name };
            nodes.Add(existing);
        }

        AddPath(existing.Children, parts, depth + 1);
    }

    public async Task<List<AreaNode>> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<AreaNode>();

        var content = await File.ReadAllTextAsync(filePath);
        return Parse(content);
    }
}

