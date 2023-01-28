using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataAccessGeneration;

public interface ISettingItem
{
    
}
public class QuerySetting : ISettingItem
{
 
    public string Query { get; set; } = "";
    public string Name { get; set; } = "";
    public ReturnType? Return { get; set; }
    
    // Spaces are valid in proc names, but not in C# names.
    public string GetName() => Name.Replace(" ", "_");   
}
public class ProcedureSetting : ISettingItem
{
    public string Proc { get; set; } = "";
    public string? Name { get; set; }
    public ReturnType? Return { get; set; }
    
    // Spaces are valid in proc names, but not in C# names.
    public string GetName() => Name ?? Proc.Replace(" ", "_");
}
