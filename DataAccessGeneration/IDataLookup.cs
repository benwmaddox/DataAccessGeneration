namespace DataAccessGeneration;

public interface IDataLookup
{
    List<string> GetProceduresForSchema(string schemaName);
    List<ParameterDefinition> GetParametersForProcedure(string schemaName, string procedureName);
    List<ResultDefinition> GetResultDefinitionsForProcedures(string schemaName, string procedureName,  List<ParameterDefinition> parameters, bool allowProcedureExecution);
    List<UserDefinedTableRowDefinition> GetUserDefinedTypes(string schemaName);
    string? GetResultDefinitionsErrorsForProcedures(string schemaName, string procedureName);
}