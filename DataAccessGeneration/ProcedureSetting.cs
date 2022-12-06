using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataAccessGeneration;

public class ProcedureSetting
{
    public string Proc { get; set; } = "";
    public string? Name { get; set; }
    public ReturnType? Return { get; set; }
    
    // Spaces are valid in proc names, but not in C# names.
    public string GetName() => Name ?? Proc.Replace(" ", "_");
}
