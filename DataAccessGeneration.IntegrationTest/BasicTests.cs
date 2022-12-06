using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.XPath;
using Xunit;

namespace DataAccessGeneration.XUnitTest;

public class BasicTests
{
    [Fact]
    public void RunsWithoutError()
    {
        var fileManager = new FakeFileManager();
        var generator = new Generator(fileManager);
        
        string executingDirectory = Path.GetDirectoryName(System.AppContext.BaseDirectory)!;
        var settingPath = Path.Join(executingDirectory, "IntegrationTestSettings.json");
        var settingJson = File.ReadAllText(settingPath);
        var settingsList = JsonSerializer.Deserialize<List<Settings>>(settingJson, options: new JsonSerializerOptions()
                           {
                               AllowTrailingCommas = true,
                               Converters = { new JsonStringEnumConverter() }
                           }) 
                           ?? throw new ArgumentException("Missing IntegrationTestSettings.json file");

        var settings = settingsList.First();
        
        var dataLookup = new DataLookup(settings.ConnectionString);
        generator.Generate(settings, dataLookup, "C:/Test/");
        
        Assert.NotEmpty(fileManager.SavedFiles);
    }
    
    
    [Fact]
    public void GetResultSetFromExecution()
    {
        var fileManager = new FakeFileManager();
        var generator = new Generator(fileManager);
        
        string executingDirectory = Path.GetDirectoryName(System.AppContext.BaseDirectory)!;
        var settingPath = Path.Join(executingDirectory, "IntegrationTestSettings.json");
        var settingJson = File.ReadAllText(settingPath);
        var settingsList = JsonSerializer.Deserialize<List<Settings>>(settingJson, options: new JsonSerializerOptions()
                           {
                               AllowTrailingCommas = true,
                               Converters = { new JsonStringEnumConverter() }
                           }) 
                           ?? throw new ArgumentException("Missing IntegrationTestSettings.json file");

        var settings = settingsList.Last();
        // settings.ProcedureList = new List<ProcedureSetting> {settings.ProcedureList.First()};
        var dataLookup = new DataLookup(settings.ConnectionString);
        var result = dataLookup.GetResultDefinitionForProcedureExecution(@"EXEC dbo.CustOrderHist @CustomerID = null
");
        
        Assert.NotEmpty(result);
    }

    [Fact]
    public void SystemSqlTypesCovered()
    {
        string executingDirectory = Path.GetDirectoryName(System.AppContext.BaseDirectory)!;
        var settingPath = Path.Join(executingDirectory, "IntegrationTestSettings.json");
        var settingJson = File.ReadAllText(settingPath);
        var settingsList = JsonSerializer.Deserialize<List<Settings>>(settingJson, options: new JsonSerializerOptions()
                           {
                               AllowTrailingCommas = true,
                               Converters = { new JsonStringEnumConverter() }
                           }) 
                           ?? throw new ArgumentException("Missing IntegrationTestSettings.json file");

        var settings = settingsList.Last();
        
        var dataLookup = new DataLookup(settings.ConnectionString);
        var systemTypes = dataLookup.GetSystemTypes();
        foreach (var systemType in systemTypes)
        {
            var validCSharpType = systemType.CSharpType(null);
            TypeConversions.getSqlDbTypeEnumFromSQLType(systemType.TypeName);
            var sqlDefaultValue = TypeConversions.getDatabaseDefaultValueFromTypeName(systemType.TypeName);
            if (sqlDefaultValue == null)
            {
                throw new Exception("No default value for " + systemType.TypeName);
            }
        }
        
    }
}
