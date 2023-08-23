using System.Collections.Generic;
using Xunit;

namespace DataAccessGeneration.XUnitTest;

public class GenerateInterface
{
    [Fact]
    public void CorrectTypeIEnumerable()
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
            "Repo", "Proc_Result", new FakeDataLookup()
            {
                GetUserDefinedTypeData = new UserDefinedTableGrouping()
                {
                    UDTTypeName = "UDFProperty",
                    Rows = new List<UserDefinedTableRowDefinition>()
                    {
                        new UserDefinedTableRowDefinition()
                        {
                            TypeName = "uniqueidentifier",
                            TableTypeName = "UDFProperty",
                            ColumnName = "InnerColumnName",
                            SchemaName = "DBO"
                        }
                    }
                }
            });
        /*
         *, new List<UserDefinedTableRowDefinition>()
            {
                new UserDefinedTableRowDefinition()
                {
                    TypeName = "uniqueidentifier",
                    TableTypeName = "UDFProperty",
                    ColumnName = "InnerColumnName",
                    SchemaName = "DBO"
                }
            }
         *
         */
        var expected = @"IEnumerable<Guid>";

        Assert.Contains(expected, result);
    }
}