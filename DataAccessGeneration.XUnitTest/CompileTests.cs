using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DataAccessGeneration.XUnitTest;

public class CompileTests
{
    // Generate the code and also make sure it can compile
    
    [Theory]
    [InlineData(ReturnType.List)]
    [InlineData(ReturnType.None)]
    [InlineData(ReturnType.Output)]
    [InlineData(ReturnType.Scalar)]
    [InlineData(ReturnType.Single)]
    [InlineData(ReturnType.SingleOrDefault)]
    public void VarietyOfReturnTypes(ReturnType returnType)
    {
        var fileManager = new FakeFileManager();
        var generator = new Generator(fileManager);
        var settings = new Settings()
        {
            Namespace = "DataAccessGeneration.XUnitTest",
            ConnectionString = "FAKE",
            IncludeFakes = false,
            ProcedureList = new List<ProcedureSetting>()
            {
                new ProcedureSetting() { Proc = "uspExampleProc", Return = returnType }
            },
            RepositoryName = "ExampleRepository",
            SchemaName = "DBO",
            OutputRelativePath = "../Repositories/XUnitTest"
        };
        var dataLookup = new FakeDataLookup()
        {
            GetProceduresForSchemaData = new List<string>(){ "uspExampleProc"},
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
            GetUserDefinedTypesData = new List<UserDefinedTypeGrouping>()
            {
                
            }
        };
        generator.Generate(settings, dataLookup,  Path.GetTempPath());

        AssertCompilesWithoutError(fileManager);
    }
    
    private void AssertCompilesWithoutError(FakeFileManager fileManager)
    {
        var result = CompileFiles(fileManager);

        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error || (d.Severity == DiagnosticSeverity.Warning && d.Id != "CS1701") );
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
            fileManager.SavedFiles.Select(file => CSharpSyntaxTree.ParseText(file.Value.IndentBasedOnBraces(), path: file.Key) ),
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
        returnList.Add( MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")));
        returnList.Add( MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")));
        returnList.Add( MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")));
        returnList.Add( MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")));
        returnList.Add( MetadataReference.CreateFromFile(typeof(Microsoft.Data.SqlClient.SqlConnection).Assembly.Location));
        returnList.Add( MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location));
        returnList.Add( MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        returnList.Add( MetadataReference.CreateFromFile(typeof(System.ComponentModel.Component).Assembly.Location));
        returnList.Add( MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location));

        return returnList;
    }
}