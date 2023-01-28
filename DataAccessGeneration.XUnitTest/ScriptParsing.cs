using System.Linq;
using Xunit;

namespace DataAccessGeneration.XUnitTest;

public class ScriptParsingTests
{
    [Fact]
    public void FindNameParameterInQuery()
    {
        string query = @"
SELECT p.name FROM sys.procedures p
JOIN sys.schemas s ON p.schema_id = s.schema_id
WHERE s.name = @schemaName";
        var parser = new ScriptParsing();
        var result = parser.FindErrorsInQuery(query);
        Assert.Empty(result);

        var parameters = parser.FindStructureInQuery(query);
        var firstResultSet = parameters.Single();
        Assert.Equal("@schemaName", firstResultSet.VariableDefinitions.Single().Name);
        
        Assert.Equal(2, firstResultSet.Tables.Count);
        Assert.Equal("procedures", firstResultSet.Tables[0].Table);
        Assert.Equal("schemas", firstResultSet.Tables[1].Table);
        Assert.Single(firstResultSet.ReturnColumns);
        Assert.Equal("name", firstResultSet.ReturnColumns[0].Column);
        
        Assert.Equal("name", firstResultSet.VariableDefinitions.Single().MatchingColumn);
        
        Assert.Equal("s", firstResultSet.VariableDefinitions.Single().MatchingTableAlias);
    }
    
    
    
}
