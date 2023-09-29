using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace DataAccessGeneration.XUnitTest;

public static class TestCompiler
{
    public static void AssertCompilesWithoutError(FakeFileManager fileManager)
    {
        var result = CompileFiles(fileManager);

        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error || (d.Severity == DiagnosticSeverity.Warning && d.Id != "CS1701"));
        if (errors?.Any() ?? false)
        {
            Console.WriteLine(string.Join(",", errors.Select(x => x.ToString() + Environment.NewLine + x.Location.SourceTree)));
            throw new Exception(string.Join(",", errors.Select(x => x.ToString() + Environment.NewLine + x.Location.SourceTree)));
        }
        Assert.True(result.Success);
    }

    private static EmitResult CompileFiles(FakeFileManager fileManager)
    {
        var metadataReferences = GetGlobalReferences();
        var compilation = CSharpCompilation.Create("TestAssembly",
            fileManager.SavedFiles.Select(file => CSharpSyntaxTree.ParseText(file.Value, path: file.Key)),
            metadataReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
        );
        EmitResult? result = null;
        using (var ms = new MemoryStream())
        {
            result = compilation.Emit(ms);
        }

        return result;
    }

    private static List<MetadataReference> GetGlobalReferences()
    {
        var returnList = new List<MetadataReference>();

        string assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")));
        returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")));
        returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")));
        returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")));
        returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll")));
        returnList.Add(MetadataReference.CreateFromFile(typeof(Microsoft.Data.SqlClient.SqlConnection).Assembly.Location));
        returnList.Add(MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location));
        returnList.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        returnList.Add(MetadataReference.CreateFromFile(typeof(System.ComponentModel.Component).Assembly.Location));
        returnList.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location));

        return returnList;
    }
}