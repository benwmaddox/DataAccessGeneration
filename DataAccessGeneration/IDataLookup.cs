namespace DataAccessGeneration;

public interface IDataLookup
{
    List<string> GetProceduresForSchema(string schemaName);
    List<ParameterDefinition> GetParametersForProcedure(string schemaName, string procedureName);
    List<ResultDefinition> GetResultDefinitionsForProcedures(string schemaName, string procedureName,  List<ParameterDefinition> parameters, bool executeDuringGeneration);
    List<UserDefinedTypeGrouping> GetUserDefinedTypes();
    UserDefinedTypeGrouping? GetUserDefinedType(string schemaName, string udtName);
    string? GetResultDefinitionsErrorsForProcedures(string schemaName, string procedureName);
}