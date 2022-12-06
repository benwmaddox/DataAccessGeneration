namespace DataAccessGeneration;

public class ResultPropertyMetaData
{
    public string CSharpName { get; set; } = "";
    public string CSharpType { get; set; } = "";
    public bool IsNullable { get; set; } = false;
    public string? DefaultPropertyAssignment { get; set; }
    public string DatabaseName { get; set; } = "";
}