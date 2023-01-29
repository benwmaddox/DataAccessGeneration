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
            GetUserDefinedTypesData = new List<UserDefinedTableRowDefinition>()
            {
                
            }
        };
        generator.Generate(settings, dataLookup,  Path.GetTempPath());

        AssertCompilesWithoutError(fileManager);
    }

    [Theory]
    [InlineData(@"
    SELECT p.name FROM sys.procedures p
        JOIN sys.schemas s ON p.schema_id = s.schema_id
        WHERE s.name = @SchemaName")]
    [InlineData(@"
SELECT definition FROM sys.sql_modules
WHERE OBJECT_ID =  OBJECT_ID(@schemaName + '.' + @procName)")]
    [InlineData(@"
SELECT p.name as ParameterName, ISNULL(t.name, ut.name) as TypeName, ISNULL(ts.name, uts.name) as TypeSchema,  p.max_length, p.precision, p.scale, p.system_type_id, p.is_output
FROM sys.parameters p 
LEFT JOIN sys.types t ON p.system_type_id = t.system_type_id AND t.is_user_defined = 0 AND t.name <> 'sysname'
LEFT JOIN sys.types ut ON p.user_type_id = ut.user_type_id AND ut.name <> 'AUTO_ID'
LEFT JOIN sys.schemas ts ON t.schema_id = ts.schema_id
LEFT JOIN sys.schemas uts ON ut.schema_id = uts.schema_id
WHERE p.OBJECT_ID = OBJECT_ID(@schemaName + '.' + @procName))
AND ISNULL(ut.name, t.name) IS NOT null
ORDER BY p.parameter_id")]
    [InlineData(@"SELECT rs.name, rs.is_nullable, ISNULL(t.name, ut.name) AS typeName, rs.max_length, rs.precision, rs.scale
FROM sys.dm_exec_describe_first_result_set('{schemaName}.{procedureName}', NULL, 0) rs
LEFT JOIN sys.types t ON rs.system_type_id = t.system_type_id AND t.is_user_defined = 0 AND t.name <> 'sysname'
LEFT JOIN sys.types ut ON rs.user_type_id = ut.user_type_id  AND ut.name <> 'AUTO_ID'
WHERE rs.name IS NOT null
ORDER BY rs.column_ordinal")]
    [InlineData(@"SELECT error_message
FROM sys.dm_exec_describe_first_result_set('{schemaName}.{procedureName}', NULL, 0) rs")]
    public void TestQueries(string query)
    {
        
        var fileManager = new FakeFileManager();
        var generator = new Generator(fileManager);
        var settings = new Settings()
        {
            Namespace = "DataAccessGeneration.XUnitTest",
            ConnectionString = "FAKE",
            IncludeFakes = false,
            QueryList = new List<QuerySetting>()
            {
                new QuerySetting()
                {
                    Name = "Sample",
                    Return = ReturnType.List,
                    Query = query
                }
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
            GetUserDefinedTypesData = new List<UserDefinedTableRowDefinition>()
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
            fileManager.SavedFiles.Select(file => CSharpSyntaxTree.ParseText(file.Value, path: file.Key) ),
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