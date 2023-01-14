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
        var result = parser.FindParametersInQuery(query);
        Assert.NotNull(result);
        
    }
}
