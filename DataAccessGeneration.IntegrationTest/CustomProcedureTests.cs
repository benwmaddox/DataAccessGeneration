using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;

namespace DataAccessGeneration.XUnitTest;

public class CustomProcedureTests
{
    [Fact]
    public void ProcedureWithUDTParametersAndTempTable()
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

        var dropDefinitions = @"
DROP PROC IF EXISTS dbo.GetTempTableWithUDT ;


DROP TYPE IF EXISTS dbo.udtStringTable ;

";
        var typeDefinition = @"

CREATE TYPE dbo.udtStringTable AS TABLE(
    Item nvarchar(255) NOT NULL
) ;

";
        // Insert 
        var procDefinition = @"

CREATE OR ALTER PROCEDURE dbo.GetTempTableWithUDT(
    @input dbo.udtStringTable READONLY
)
AS
BEGIN
    CREATE TABLE #SampleTable (
        name varchar(5)
    ) ;
    SELECT * FROM #SampleTable ;
END ;

";

        ExecuteQuery(settings.ConnectionString, dropDefinitions);
        ExecuteQuery(settings.ConnectionString, typeDefinition);
        ExecuteQuery(settings.ConnectionString, procDefinition);

        settings.ProcedureList = new List<ProcedureSetting>()
        {
            new ProcedureSetting()
            {
                Proc = "GetTempTableWithUDT",
                Return = ReturnType.List
            }
        };
        var dataLookup = new DataLookup(settings.ConnectionString);
        generator.Generate(settings, dataLookup, Path.GetTempPath());

        Assert.NotEmpty(fileManager.SavedFiles);

        ExecuteQuery(settings.ConnectionString, dropDefinitions);

    }

    private int ExecuteQuery(string connectionString, string query)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            SqlCommand cm = new SqlCommand(query, connection);

            cm.CommandTimeout = 120000;
            // Opening Connection  
            connection.Open();
            // Executing the SQL query  
            var result = cm.ExecuteNonQuery();
            return result;

        }
    }
}