using System.Collections.Generic;

namespace DataAccessGeneration.XUnitTest;

public class FakeDataLookup : IDataLookup
{
    public List<string> GetProceduresForSchemaData = new List<string>();
    public List<string> GetProceduresForSchema(string schemaName) => GetProceduresForSchemaData;

    public List<ParameterDefinition> GetParametersForProcedureData = new List<ParameterDefinition>();
    public List<ParameterDefinition> GetParametersForProcedure(string schemaName, string procedureName) => GetParametersForProcedureData;

    public List<ResultDefinition> GetResultDefinitionsForProcedureData = new List<ResultDefinition>();
    public List<ResultDefinition> GetResultDefinitionsForProcedures(string schemaName, string procedureName, List<ParameterDefinition> parameters, bool executeDuringGeneration) => GetResultDefinitionsForProcedureData;

    public List<UserDefinedTypeGrouping> GetUserDefinedTypesData = new List<UserDefinedTypeGrouping>();
    public List<UserDefinedTypeGrouping> GetUserDefinedTypes() => GetUserDefinedTypesData;

    public UserDefinedTypeGrouping? GetUserDefinedTypeData = null;
    public UserDefinedTypeGrouping? GetUserDefinedType(string schemaName, string udtName) => GetUserDefinedTypeData;
    public string? GetResultDefinitionsErrorsForProceduresData { get; set; } = null;
    public string? GetResultDefinitionsErrorsForProcedures(string schemaName, string procedureName) => GetResultDefinitionsErrorsForProceduresData;
}