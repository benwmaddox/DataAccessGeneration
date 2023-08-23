namespace DataAccessGeneration;

public class UserDefinedTypeGrouping
{
    public string SchemaName { get; set; } = "";
    public string UDTTypeName { get; set; } = "";
    public List<UserDefinedTableRowDefinition> Rows { get; set; } = new List<UserDefinedTableRowDefinition>();
    public bool RequiresSchemaToDisambiguate { get; set; }
    public bool RetrievedForUse { get; set; }
    public string GetCSharpTypeName() => RequiresSchemaToDisambiguate ? $"{SchemaName}_{UDTTypeName}" : UDTTypeName;
}