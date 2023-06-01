using System.Collections.Generic;
using Xunit;

namespace DataAccessGeneration.XUnitTest;

public class GenerateInterface
{
    [Fact]
    public static void CorrectTypeIEnumerable()
    {
        // Issue 47
        var result = Generator.GenerateInterface("Proc", new List<ParameterDefinition>()
            {
                new ParameterDefinition()
                {
                    TypeName = "UDFProperty",
                    Name = "UDFProperty"
                }

            },
            "Repo", "Proc_Result", new List<string>(){"UDFProperty"}, new List<UserDefinedTableRowDefinition>()
            {
                new UserDefinedTableRowDefinition()
                {
                    TypeName = "uniqueidentifier",
                    TableTypeName = "UDFProperty",
                    ColumnName = "InnerColumnName",
                    SchemaName = "DBO"
                }
            });

        var expected = @"IEnumerable<Guid>";

        Assert.Contains(expected, result);
    }
}