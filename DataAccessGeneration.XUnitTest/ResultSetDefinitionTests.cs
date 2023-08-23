using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataAccessGeneration.XUnitTest.Extensions;
using Xunit;

namespace DataAccessGeneration.XUnitTest;

public class ResultSetDefinitionTests
{
    [Fact]
    public void OutputClassIsCorrect()
    {
        var fileManager = new FakeFileManager();
        var generator = new Generator(fileManager);
        var settings = new Settings()
        {
            ProcedureList =new List<ProcedureSetting>(){ new ProcedureSetting() { Proc = "CustOrderHist", Return = ReturnType.Output }},
            Namespace = "DataAccessGeneration.XUnitTest",
            ConnectionString = "FAKE",
            IncludeFakes = false,
            RepositoryName = "ExampleRepository",
            SchemaName = "DBO",
            OutputRelativePath = "../Repositories/XUnitTest"
        };
        
        
        var dataLookup = new FakeDataLookup()
        {
            GetProceduresForSchemaData = new List<string>(){ "CustOrderHist"},
            GetResultDefinitionsForProcedureData = new List<ResultDefinition>()
            {
                new ResultDefinition()
                {
                    Name = "Name",
                    TypeName = "varchar",
                    Precision = 0,
                    Scale = 0,
                    IsNullable = false,
                    MaxLength = 50
                }
            },
            GetResultDefinitionsErrorsForProceduresData = null,
            GetParametersForProcedureData = new List<ParameterDefinition>()
            {
                new ParameterDefinition()
                {
                    Name = "ID",
                    TypeName = "int",
                    Precision = 0,
                    Scale = 4,
                    MaxLength = 4,
                    IsOutput = true,
                    TypeSchema = "sys",
                    DefaultValue = null
                },
                new ParameterDefinition()
                {
                    Name = "Name",
                    TypeName = "varchar",
                    Precision = 0,
                    Scale = 50,
                    MaxLength = 50,
                    IsOutput = false,
                    TypeSchema = "sys",
                    DefaultValue = null
                }
            },
            GetUserDefinedTypesData = new List<UserDefinedTableGrouping>()
            {
                
            }
        };
        generator.Generate(settings, dataLookup,  Path.GetTempPath());

        Assert.NotEmpty(fileManager.SavedFiles);
        var filePath = Path.Join(Path.GetTempPath(), "CustOrderHist.generated.cs");
        Assert.Contains( filePath, fileManager.SavedFiles.Keys);
        Assert.Contains(@"public partial class CustOrderHist_ResultSet
                                   {
                                       public int? ID { get; set; }
                                   }".TrimAllLines(), fileManager.SavedFiles[filePath].TrimAllLines());
    }
}