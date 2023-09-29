using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataAccessGeneration.XUnitTest;

public class CompileTests
{
    // Generate the code and also make sure it can compile
    [Fact]
    public void JsonFileCompilation()
    {
        var fileManager = new FakeFileManager();

        string executingDirectory = Path.GetDirectoryName(System.AppContext.BaseDirectory)!;
        var settingPath = Path.Join(executingDirectory, "IntegrationTestSettings.json");
        var settingJson = File.ReadAllText(settingPath);
        var settingsList = JsonSerializer.Deserialize<List<Settings>>(settingJson, new JsonSerializerOptions()
                           {
                               AllowTrailingCommas = true,
                               Converters = { new JsonStringEnumConverter() }
                           })
                           ?? throw new ArgumentException("Missing IntegrationTestSettings.json file");

        Parallel.ForEach(settingsList, (settings) =>
        {
            var generator = new Generator(fileManager);
            var dataLookup = new DataLookup(settings.ConnectionString);
            generator.Generate(settings, dataLookup, Path.GetTempPath());
            Assert.Empty(generator.Errors);
        });

        TestCompiler.AssertCompilesWithoutError(fileManager);
    }

    [Fact]
    public void SingleResultType()
    {
        var fileManager = new FakeFileManager();
        var generator = new Generator(fileManager);

        string executingDirectory = Path.GetDirectoryName(System.AppContext.BaseDirectory)!;
        var settingPath = Path.Join(executingDirectory, "IntegrationTestSettings.json");
        var settingJson = File.ReadAllText(settingPath);
        var settingsList = JsonSerializer.Deserialize<List<Settings>>(settingJson, new JsonSerializerOptions()
                           {
                               AllowTrailingCommas = true,
                               Converters = { new JsonStringEnumConverter() }
                           })
                           ?? throw new ArgumentException("Missing IntegrationTestSettings.json file");

        var settings = settingsList.First();
        foreach (var procedureSetting in settings.ProcedureList)
        {
            procedureSetting.Return = ReturnType.Single;
        }

        var dataLookup = new DataLookup(settings.ConnectionString);
        generator.Generate(settings, dataLookup, Path.GetTempPath());

        TestCompiler.AssertCompilesWithoutError(fileManager);
    }
}