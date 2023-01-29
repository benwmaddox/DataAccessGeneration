using System.Collections.Generic;

namespace DataAccessGeneration.XUnitTest;

public class FakeDataLookup : IDataLookup
{
    public List<string> GetProceduresForSchemaData = new List<string>();
    public List<string> GetProceduresForSchema(string schemaName) => GetProceduresForSchemaData;

    public List<ParameterDefinition> GetParametersForProcedureData = new List<ParameterDefinition>();
    public List<ParameterDefinition> GetParametersForProcedure(string schemaName, string procedureName) => GetParametersForProcedureData;

    public List<ResultDefinition> GetResultDefinitionsForProcedureData = new List<ResultDefinition>();
    public List<ResultDefinition> GetResultDefinitionsForProcedures(string schemaName, string procedureName, List<ParameterDefinition> parameters, bool allowProcedureExecution) => GetResultDefinitionsForProcedureData;

    public List<UserDefinedTableRowDefinition> GetUserDefinedTypesData = new List<UserDefinedTableRowDefinition>();
    public List<UserDefinedTableRowDefinition> GetUserDefinedTypes(string schemaName) => GetUserDefinedTypesData;

    public string? GetResultDefinitionsErrorsForProceduresData { get; set; } = null;
    public string? GetResultDefinitionsErrorsForProcedures(string schemaName, string procedureName) => GetResultDefinitionsErrorsForProceduresData;
    
    public List<ParameterDefinition> GetParametersForStructureData { get; set; } = new List<ParameterDefinition>();
    public List<ParameterDefinition> GetParametersForStructure(List<ResultSetDef> resultSetDefs) => GetParametersForStructureData;

    public List<ResultDefinition> GetResultDefinitionsForStructureData { get; set; } = new List<ResultDefinition>();
    public List<ResultDefinition> GetResultDefinitionsForStructure(List<ResultSetDef> resultSetDefs) => GetResultDefinitionsForStructureData;
}