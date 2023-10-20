using System.Linq;
using System.Text;

namespace DataAccessGeneration;

public class Generator
{
    private readonly IFileManager _fileManager;
    private readonly ParallelOptions _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 2 };
    public string CurrentActivity { get; set; } = "Initialized";
    public List<string> Errors = new List<string>();

    public Generator(IFileManager fileManager)
    {
        _fileManager = fileManager;
    }

    public void Generate(Settings settings, IDataLookup dataLookup, string outputDirectory)
    {
        var procedures = dataLookup.GetProceduresForSchema(settings.SchemaName).Select(x => new ProcedureSetting()
        {
            Proc = x,
            Return = ReturnType.List
        }).ToList();
        if (settings.ProcedureList.Any())
        {
            CurrentActivity = $"{settings.RepositoryName}: Checking procedure list for issues";
            procedures = FilterProcedures(procedures, settings.ProcedureList);
            VerifyNoDuplicateProcedures(settings.ProcedureList);
            VerifyProceduresAreInSchema(procedures, settings.ProcedureList);
        }
        else
        {
            int loadStarted = 0;
            Parallel.ForEach(procedures, _parallelOptions, (procedure, state, index) =>
            {
                loadStarted++;
                CurrentActivity = $"{settings.RepositoryName}: Loading procedure information {loadStarted} of {procedures.Count}";
                var parameters = dataLookup.GetParametersForProcedure(settings.SchemaName, procedure.Proc);
                var results = dataLookup.GetResultDefinitionsForProcedures(settings.SchemaName, procedure.Proc, parameters, executeDuringGeneration: settings.AllowExecution);
                procedure.Return = results.Any() ? ReturnType.List : ReturnType.None;
                if (Char.IsDigit(procedure.Proc[0]))
                {
                    procedure.Name = "N" + procedure.Proc.Replace(" ", "_");
                }
            });
        }

        if (string.IsNullOrWhiteSpace(settings.RepositoryName))
        {
            settings.RepositoryName = $"{settings.SchemaName}Repository";
        }

        CurrentActivity = $"{settings.RepositoryName} loading user defined types";
        var userDefinedTypes = dataLookup.GetUserDefinedTypes();
        var repoClass = GenerateRepoClassWithConstructor(settings);
        _fileManager.WriteFiles(outputDirectory, repoClass);
        List<OutputFile> fakeRepoClasses = new List<OutputFile>();
        if (settings.IncludeFakes)
        {
            fakeRepoClasses = GenerateFakeRepoClassWithConstructor(settings);
            _fileManager.WriteFiles(outputDirectory, fakeRepoClasses);
        }

        var procedureClasses = new List<OutputFile>();
        int generateStarted = 0;
        Parallel.ForEach(procedures, _parallelOptions, (procedure, state, index) =>
        {
            generateStarted++;
            CurrentActivity = $"{settings.RepositoryName} Generating procedure {generateStarted} of {procedures.Count}: {procedure.Proc}";
            procedureClasses.Add(GenerateProcedure(procedure, settings, dataLookup));
            if (settings.IncludeFakes)
            {
                procedureClasses.Add(GenerateFakeForProcedure(procedure, settings, dataLookup));
            }

            procedureClasses.Add(GenerateTransactionManagedProcedure(procedure, settings, dataLookup));
        });
        _fileManager.WriteFiles(outputDirectory, procedureClasses);

        var userDefinedTypeClasses = GenerateUserDefinedTypeClasses(userDefinedTypes.Where(x => x.RetrievedForUse).ToList(), settings.Namespace, dataLookup);
        CurrentActivity = $"{settings.RepositoryName} writing user defined types";
        _fileManager.WriteFiles(outputDirectory, userDefinedTypeClasses);

        var filesToKeep = procedureClasses.Select(x => x.RelativeFilePath)
            .Concat(userDefinedTypeClasses.Select(x => x.RelativeFilePath))
            .Concat(repoClass.Select(x => x.RelativeFilePath))
            .Concat(fakeRepoClasses.Select(x => x.RelativeFilePath))
            .ToHashSet();

        var deletedFiles = _fileManager.DeleteFiles(outputDirectory, filesToKeep);
        CurrentActivity = settings.RepositoryName + $" finished. {filesToKeep.Count} files generated."
                                                  + (deletedFiles.Any()
                                                      ? Environment.NewLine + $"  {deletedFiles.Count} files deleted: " +
                                                        string.Join("", deletedFiles.Select(file => Environment.NewLine + "    " + file))
                                                      : "");
    }

    private void VerifyNoDuplicateProcedures(List<ProcedureSetting> settingsProcedureList)
    {
        var duplicates = settingsProcedureList.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
        if (duplicates.Any())
        {
            var error = $"Duplicate procedures found: {string.Join(", ", duplicates)}";
            Errors.Add(error);
        }
    }

    private static List<ProcedureSetting> FilterProcedures(List<ProcedureSetting> procedures, List<ProcedureSetting> settingsProcedureList)
    {
        if (!settingsProcedureList.Any()) return procedures;

        var procedureNames = procedures.Select(x => x.Proc).ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        return settingsProcedureList.Where(x => procedureNames.Contains(x.Proc)).ToList();

    }

    private static List<OutputFile> GenerateRepoClassWithConstructor(Settings settings)
    {
        return new List<OutputFile>()
        {
            new OutputFile
            (
                $"{settings.RepositoryName}.generated.cs",
                WrapInNamespace(
                    $@"public enum TransactionResult
                {{
                    Rollback,
                    Commit
                }}

                public partial interface I{settings.RepositoryName}
                {{
                    Task RunTransaction(Func<TransactionManagedContext, Task<TransactionResult>> action);
                }}

                public class TransactionManagedContext
                {{                        
                    public TransactionManagedContext(I{settings.RepositoryName} repository, SqlConnection? connection, SqlTransaction? transaction)
                    {{
                        Repository = repository;
                        Connection = connection;
                        Transaction = transaction;
                    }}

                    public I{settings.RepositoryName} Repository {{ get; set; }}
                    public SqlConnection? Connection {{ get; set; }}
                    public SqlTransaction? Transaction {{ get; set; }}
                }}

                public partial class {settings.RepositoryName} : I{settings.RepositoryName}
	            {{
		            protected bool _inTransaction = false;
		            private string _connectionString;
		            public {settings.RepositoryName}(string connectionString) 
		            {{
			            _connectionString = connectionString;
		            }}
		            
		            private object? ConvertDBNullToNull(object item)
		            {{
			            return item == DBNull.Value ? null : item;
		            }}
                
                    public async Task RunTransaction(Func<TransactionManagedContext, Task<TransactionResult>> action)
                    {{
                        using (SqlConnection connection = new SqlConnection(_connectionString))
                        {{
                            await connection.OpenAsync();
                            using (var transaction = connection.BeginTransaction())
                            {{
                                try
                                {{
                                    _inTransaction = true;
                                    var transactionManaged = new {settings.RepositoryName}.TransactionManaged(connection, transaction);
                                    var transactionContext = new TransactionManagedContext(transactionManaged, connection, transaction);
                                    var result = await action.Invoke(transactionContext);
                                    if (result == TransactionResult.Rollback) 
                                    {{
                                        transaction.Rollback();
                                    }}
                                    else 
                                    {{
                                        transaction.Commit();
                                    }}
                                }}
                                catch
                                {{
                                    transaction.Rollback();
                                    _inTransaction = false;
                                    throw;
                                }}
                                _inTransaction = false;
                            }}
                        }}
                    }}

                    public partial class TransactionManaged : I{settings.RepositoryName}
                    {{
                        // Didn't use underscores to make it easier to reuse code referencing connection
                        private SqlConnection connection;
                        private SqlTransaction transaction;
                        public TransactionManaged(SqlConnection connectionParameter, SqlTransaction transactionParameter)
                        {{                            
                            connection = connectionParameter;
                            transaction = transactionParameter;                
                        }}

		                private object? ConvertDBNullToNull(object item)
		                {{
                            return item == DBNull.Value ? null : item;
		                }}
                        
                        public async Task RunTransaction(Func<TransactionManagedContext, Task<TransactionResult>> action)
                        {{
                            // I hate to do this, but don't see a great alternative
                            await Task.Run(() => throw new NotImplementedException());
                        }}
                    }}

	            }}", settings.Namespace, true)
            )
        };
    }


    private static List<OutputFile> GenerateFakeRepoClassWithConstructor(Settings settings)
    {
        return new List<OutputFile>()
        {
            new OutputFile(
                $"Fake/Fake{settings.RepositoryName}.generated.cs",
                WrapInNamespace(
                    $@"public partial class Fake{settings.RepositoryName} : I{settings.RepositoryName} 
	            {{
		            
		            public Fake{settings.RepositoryName}() 
		            {{
		            }}

                    public async Task RunTransaction(Func<TransactionManagedContext, Task<TransactionResult>> action)
                    {{
                        // In memory fakes can't have connections so nulls are used here instead                        
                        var context = new TransactionManagedContext(this, null, null); 
                        await action.Invoke(context);                        
                    }}

	            }}", settings.Namespace + ".Fake", true, settings.Namespace)
            )
        };
    }

    private static string WrapInNamespace(string content, string namespaceName, bool includeVersion, params string[] additionalUsingNamespaces)
    {
        var versionSection = includeVersion ? " " + Program.VERSION : "";

        var namespaces = additionalUsingNamespaces.ToHashSet();
        namespaces.Add("System");
        if (content.Contains("SqlConnection")) namespaces.Add("Microsoft.Data.SqlClient");
        if (content.Contains("SqlCommand")) namespaces.Add("Microsoft.Data.SqlClient");
        if (content.Contains("CommandType")) namespaces.Add("System.Data");
        if (content.Contains("List<")) namespaces.Add("System.Collections.Generic");
        if (content.Contains("CommandType")) namespaces.Add("System.Data.SqlTypes");
        if (content.Contains("Task<") || content.Contains("Task ")) namespaces.Add("System.Threading.Tasks");
        if (content.Contains(".Single(") || content.Contains(".SingleOrDefault(") || content.Contains(".Select(")) namespaces.Add("System.Linq");


        return $@"#nullable enable{string.Join("", namespaces.OrderBy(x => x).Select(x => $"\nusing {x};"))}

                // This file was generated by DataAccessGenerator{versionSection}. Please do not change manually.
                namespace {namespaceName}
                {{
                    {content}
                }}";
    }

    private ResultMetaData GetResultMetaData(ProcedureSetting procedureSetting, List<ResultDefinition> resultColumns, List<ParameterDefinition> parameters, IDataLookup lookup)
    {
        var methodReturnType = GetReturnType(procedureSetting, resultColumns, parameters);
        var properties = new List<ResultPropertyMetaData>();
        switch (procedureSetting.Return)
        {
            case ReturnType.List:
            case ReturnType.Single:
            case ReturnType.SingleOrDefault:
            case null:
                foreach (var resultColumn in resultColumns)
                {
                    properties.Add(new ResultPropertyMetaData()
                    {
                        CSharpName = resultColumn.CSharpPropertyName(),
                        DatabaseName = resultColumn.Name,
                        IsNullable = resultColumn.IsNullable ?? true,
                        DefaultPropertyAssignment = GetDefaultString(resultColumn.CSharpType()),
                        CSharpType = resultColumn.CSharpType()
                    });
                }

                break;
            case ReturnType.Scalar:
                var scalarResultColumn = resultColumns.Single();
                properties.Add(new ResultPropertyMetaData()
                {
                    CSharpName = scalarResultColumn.CSharpPropertyName(),
                    DatabaseName = scalarResultColumn.Name,
                    IsNullable = scalarResultColumn.IsNullable ?? true,
                    DefaultPropertyAssignment = GetDefaultString(scalarResultColumn.CSharpType()),
                    CSharpType = scalarResultColumn.CSharpType()
                });
                break;
            case ReturnType.Output:
                foreach (var outputParameter in parameters.Where(x => x.IsOutput))
                {
                    properties.Add(new ResultPropertyMetaData()
                    {
                        CSharpName = outputParameter.CSharpPropertyName(),
                        DatabaseName = outputParameter.Name,
                        IsNullable = true,
                        DefaultPropertyAssignment = GetDefaultString(outputParameter.CSharpType(lookup: lookup)),
                        CSharpType = outputParameter.CSharpType(lookup: lookup)
                    });
                }

                break;
            case ReturnType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var result = new ResultMetaData()
        {
            ReturnType = procedureSetting.Return ?? ReturnType.List,
            ReturnTypeCSharpString = methodReturnType,
            Properties = properties
        };

        if (result.ReturnType != ReturnType.None && !result.Properties.Any())
        {
            var error =
                $"{procedureSetting.Proc} has no return columns. Please change the Return setting from {result.ReturnType} to the appropriate type or update the procedure. It might need to be set to None if there should not be results.";
            Errors.Add(error);
        }

        return result;
    }

    private OutputFile GenerateProcedure(ProcedureSetting procedureSetting, Settings settings, IDataLookup lookup)
    {
        var parameters = lookup.GetParametersForProcedure(settings.SchemaName, procedureSetting.Proc);
        var resultColumns =
            procedureSetting.LookupOutputTypes
                ? lookup.GetResultDefinitionsForProcedures(settings.SchemaName, procedureSetting.Proc, parameters,
                    executeDuringGeneration: procedureSetting.LookupOutputTypes && settings.AllowExecution)
                : new List<ResultDefinition>();
        var resultMetaData = GetResultMetaData(procedureSetting, resultColumns, parameters, lookup);

        // Only check for errors if there aren't return columns. Sometimes you can get an error without it being a show-stopping error
        if (!resultColumns.Any() && resultMetaData.ReturnType != ReturnType.None && resultMetaData.ReturnType != ReturnType.Output)
        {
            var resultError = lookup.GetResultDefinitionsErrorsForProcedures(settings.SchemaName, procedureSetting.Proc);
            if (resultError != null)
            {
                // Errors returned from SQL for return type. This will intentionally be skipped if we say we don't want return columns
                Errors.Add(procedureSetting.Proc + ": " + resultError);
            }
        }

        var methodReturnType = resultMetaData.ReturnType != ReturnType.None ? $@"Task<{resultMetaData.ReturnTypeCSharpString}>" : "Task";

        var sb = new StringBuilder();

        var parameterDefinition = GenerateParameterDefinition(parameters, procedureSetting, lookup);
        if (parameterDefinition != null)
        {
            sb.AppendLine(parameterDefinition);
        }


        if (resultMetaData.ReturnType != ReturnType.None)
        {
            sb.AppendLine(GenerateResultSetClass(procedureSetting, resultMetaData, parameters, lookup));
        }

        sb.AppendLine(GenerateInterface(procedureSetting.GetName(), parameters, settings.RepositoryName!, methodReturnType, lookup));

        sb.AppendLine(
            $@"
            public partial class {settings.RepositoryName} : I{settings.RepositoryName}
            {{
                {AddProcedureCallingMethod(procedureSetting, parameters,  methodReturnType, settings, resultMetaData, lookup)}
                {AddShorthandMethod(procedureSetting.GetName(), parameters,methodReturnType, resultMetaData, lookup)}
                {AddUDTShorthandMethod(procedureSetting.GetName(), parameters, methodReturnType, resultMetaData, lookup)}
            }}
        ");

        return new OutputFile($"{procedureSetting.GetName()}.generated.cs",
                WrapInNamespace(sb.ToString(), settings.Namespace, false)
            );
    }



    private OutputFile GenerateTransactionManagedProcedure(ProcedureSetting procedureSetting, Settings settings, IDataLookup lookup)
    {
        var parameters = lookup.GetParametersForProcedure(settings.SchemaName, procedureSetting.Proc);
        var resultColumns = procedureSetting.LookupOutputTypes
            ? lookup.GetResultDefinitionsForProcedures(settings.SchemaName, procedureSetting.Proc, parameters, executeDuringGeneration: procedureSetting.LookupOutputTypes && settings.AllowExecution)
            : new List<ResultDefinition>();
        var resultMetaData = GetResultMetaData(procedureSetting, resultColumns, parameters, lookup);
        // Only check for errors if there aren't return columns. Sometimes you can get an error without it being a show-stopping error
        if (!resultColumns.Any() && resultMetaData.ReturnType != ReturnType.None && resultMetaData.ReturnType != ReturnType.Output)
        {
            var resultError = lookup.GetResultDefinitionsErrorsForProcedures(settings.SchemaName, procedureSetting.Proc);
            if (resultError != null)
            {
                // Errors returned from SQL for return type. This will intentionally be skipped if we say we don't want return columns
                Errors.Add(procedureSetting.Proc + ": " + resultError);
            }
        }

        var methodReturnType = resultMetaData.ReturnType != ReturnType.None ? $@"Task<{resultMetaData.ReturnTypeCSharpString}>" : "Task";

        var sb = new StringBuilder();

        sb.AppendLine($@"
        public partial class {settings.RepositoryName} : I{settings.RepositoryName}
        {{
            public partial class TransactionManaged :  I{settings.RepositoryName}
            {{
                {AddProcedureCallingMethod(procedureSetting, parameters, methodReturnType, settings, resultMetaData, lookup, false)}
                {AddShorthandMethod(procedureSetting.GetName(), parameters,  methodReturnType, resultMetaData, lookup)}
                {AddUDTShorthandMethod(procedureSetting.GetName(), parameters, methodReturnType, resultMetaData, lookup)}
            }}
        }}");

        return new OutputFile($"{procedureSetting.GetName()}.TransactionManaged.generated.cs",
                WrapInNamespace(sb.ToString(), settings.Namespace, false)
            );
    }


    private static string? GenerateResultSetClass(ProcedureSetting procedureSetting, ResultMetaData resultMetaData, List<ParameterDefinition> parameters, IDataLookup lookup)
    {
        switch (resultMetaData.ReturnType)
        {
            case ReturnType.List:
            case ReturnType.Single:
            case ReturnType.SingleOrDefault:
            case ReturnType.Scalar:
                return $@"
                        public partial class {procedureSetting.GetName()}_ResultSet
                        {{
                            {CSharpProperties(resultMetaData.Properties)}
                        }}";
            case ReturnType.None:
                return null; //no new class needed
            case ReturnType.Output:
                return $@"
                        public partial class {procedureSetting.GetName()}_ResultSet
                        {{
                            {CSharpProperties(parameters.Where(x => x.IsOutput).ToList(), lookup)}
                        }}";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    /*
     Have both inner and outer have same interface? Then expose interface in RunTransaction? Maybe make it a private inner class and expose it?
     
     https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/nested-types
     // Maybe stick connection on outer class and access in nested class. And expose the nested class for transaction calls?
     
        public enum TransactionResult
        {
            Rollback,
            Commit
        }

        
        public async Task RunTransaction(Func<M501,Task<TransactionResult>> action)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (var transaction = connection.BeginTransaction())
                {
                    _inTransaction = true;
                    try
                    {
                        var result = await action.Invoke(this);
                        if (result == TransactionResult.Rollback) transaction.Rollback();
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        _inTransaction = false;
                        throw;
                    }
                }
            }
        }
    */


    public string? GetReturnType(ProcedureSetting procedureSetting, List<ResultDefinition> resultColumns, List<ParameterDefinition> parameters)
    {
        switch (procedureSetting.Return)
        {
            case ReturnType.List:
            case null:
                if (resultColumns.Any())
                {
                    return $"List<{procedureSetting.GetName()}_ResultSet>";
                }
                else
                {
                    return null;
                }

            case ReturnType.Single:
                return $"{procedureSetting.GetName()}_ResultSet";
            case ReturnType.SingleOrDefault:
                return $"{procedureSetting.GetName()}_ResultSet?";
            case ReturnType.Scalar:
                if (resultColumns.Count == 1)
                {
                    return resultColumns.Single().CSharpType();
                }
                else
                {
                    var error =
                        $"Scalar return type is only valid for procedures with a single return column. Failure on {procedureSetting.Proc}. Found the following columns: {string.Join(", ", resultColumns.Select(x => x.Name))}";
                    Errors.Add(error);
                    return null;
                }
            case ReturnType.Output:
                if (parameters.Any(x => x.IsOutput))
                {
                    return $"{procedureSetting.GetName()}_ResultSet";
                }
                else
                {
                    var error = $"Output return type is only valid for procedures with output parameters. Failure on {procedureSetting.Proc}. No output parameters found.";
                    Errors.Add(error);
                    return null;
                }
            case ReturnType.None:
                return null;
            default:
                throw new ArgumentOutOfRangeException(nameof(procedureSetting.Return), procedureSetting.Return, null);
        }
    }

    private OutputFile GenerateFakeForProcedure(ProcedureSetting procedureSetting,  Settings settings,
        IDataLookup lookup)
    {
        var parameters = lookup.GetParametersForProcedure(settings.SchemaName, procedureSetting.Proc);
        var resultColumns = procedureSetting.LookupOutputTypes
            ? lookup.GetResultDefinitionsForProcedures(settings.SchemaName, procedureSetting.Proc, parameters,
                executeDuringGeneration: procedureSetting.LookupOutputTypes && settings.AllowExecution)
            : new List<ResultDefinition>();
        var resultMetaData = GetResultMetaData(procedureSetting, resultColumns, parameters, lookup);
        var methodReturnType = resultMetaData.ReturnType != ReturnType.None ? $@"Task<{resultMetaData.ReturnTypeCSharpString}>" : "Task";

        var sb = new StringBuilder();

        sb.AppendLine($@"public partial class Fake{settings.RepositoryName} : I{settings.RepositoryName}
        {{
            {AddFakeProcedureCallingMethod(procedureSetting, parameters, methodReturnType, settings, resultMetaData)}
            {AddShorthandMethod(procedureSetting.GetName(), parameters, methodReturnType, resultMetaData, lookup)}
            {AddUDTShorthandMethod(procedureSetting.GetName(), parameters, methodReturnType, resultMetaData, lookup)}
        }}");

        return new OutputFile($"Fake/{procedureSetting.GetName()}.generated.cs",
                WrapInNamespace(sb.ToString(), settings.Namespace + ".Fake", false, settings.Namespace)
            );

    }

    public static string GenerateInterface(string procName, List<ParameterDefinition> parameters, string repoName, string resultType, IDataLookup dataLookup)
    {
        StringBuilder sb = new StringBuilder();
        if (parameters.Any())
        {
            var shortHandMethod = parameters.Count < 4
                ? $@"{resultType} {procName}({string.Join(", ", parameters.Select(p => p.CSharpType(dataLookup) + " " + p.CSharpPropertyName().ToParameterCase()))});"
                : "";
            
            var typeMatch = parameters.Count == 1 ? dataLookup.GetUserDefinedType(parameters.Single().TypeSchema, parameters.Single().TypeName) : null;
            var udtMethod = typeMatch != null && typeMatch.Rows.Count == 1 && parameters.Count == 1
                ? $"{resultType} {procName}(IEnumerable<{typeMatch.Rows.Single().CSharpType(isNullable:false)}> {parameters.Single().CSharpPropertyName().ToParameterCase()});" 
                : "";

            sb.AppendLine($@"
            public partial interface I{repoName}
            {{
                {resultType} {procName}({procName}_Parameters parameters);
                {shortHandMethod}
                {udtMethod}
            }}");
        }
        else
        {
            sb.AppendLine($@"
            public partial interface I{repoName}
            {{
                {resultType} {procName}();  
            }}");
        }

        return sb.ToString();
    }


    public static string? GenerateParameterDefinition(List<ParameterDefinition> parameters, ProcedureSetting procedureSetting, IDataLookup lookup)
    {
        var cSharpProperties = string.Join("", parameters.Select(p =>
            CalculateParameterSummary(p)
            + $"\npublic {p.CSharpType(lookup)} {p.CSharpPropertyName()} {{ get; set; }}"
        ));
        return parameters.Any()
            ? $@"
                public partial class {procedureSetting.GetName()}_Parameters
				{{
                    {cSharpProperties}		
				}}"
            : null;
    }

    private static string CalculateParameterSummary(ParameterDefinition p)
    {
        string data = (p.IsOutput ? "\n/// Output parameter" : "") +
                      (p.DefaultValue != null ? $"\n/// Default Value: {p.DefaultValue}" : "");
        if (string.IsNullOrWhiteSpace(data)) return "";

        return "\n/// <summary>" + data + "\n/// </summary>";
    }

    private static string AddShorthandMethod(string procName, List<ParameterDefinition> parameters,  string methodReturnType, ResultMetaData resultMetaData, IDataLookup lookup)
    {
        if (parameters.Any() && parameters.Count < 4)
        {
            var parameterList = string.Join(", ", parameters.Select(p => p.CSharpType(lookup) + " " + p.CSharpPropertyName().ToParameterCase()));
            var propertyAssignmentList = string.Join("," + Environment.NewLine, parameters.Select(p => p.CSharpPropertyName() + " = " + p.CSharpPropertyName().ToParameterCase()));
            var returnLine = resultMetaData.ReturnType != ReturnType.None
                ? $"return await {procName}(parameters);"
                : $"await {procName}(parameters);";

            return $@"
                public async {methodReturnType} {procName}({parameterList})
                {{
                    var parameters = new {procName}_Parameters()
	                {{
		                {propertyAssignmentList}
	                }};
                    {returnLine}
                }}
";
        }

        return "";
    }
    private static string AddUDTShorthandMethod(string procName, List<ParameterDefinition> parameters,  string methodReturnType, ResultMetaData resultMetaData, IDataLookup dataLookup)
    {
        if (parameters.Count != 1) return "";
        var typeMatches = dataLookup.GetUserDefinedType(parameters.Single().TypeSchema, parameters.Single().TypeName)?.Rows;
        if (typeMatches == null || typeMatches.Count != 1) return "";
        var typeMatch = typeMatches.Single();
        
        var returnLine = resultMetaData.ReturnType != ReturnType.None
            ? $"return await {procName}(parameters);"
            : $"await {procName}(parameters);";

        return $@"
            public async {methodReturnType} {procName}(IEnumerable<{typeMatch.CSharpType(isNullable:false)}> {parameters.Single().CSharpPropertyName().ToParameterCase()})
            {{
                var parameters = new {procName}_Parameters()
	            {{
                    {parameters.Single().CSharpPropertyName()} = {parameters.Single().CSharpPropertyName().ToParameterCase()}.Select(item => new {typeMatch.TableTypeName}()
                    {{
                        {typeMatch.CSharpPropertyName()} = item
                    }}).ToList()
	            }};
                {returnLine}
            }}
";
    }

    private static string AddProcedureCallingMethod(ProcedureSetting procedureSetting, List<ParameterDefinition> parameters, string methodReturnType,
        Settings settings, ResultMetaData resultMetaData, IDataLookup lookup, bool includeConnectionCreation = true)
    {
        StringBuilder sb = new StringBuilder();
        if (parameters.Any())
        {
            sb.AppendLine($@"public async {methodReturnType} {procedureSetting.GetName()}({procedureSetting.GetName()}_Parameters parameters)
                {{");
        }
        else
        {
            sb.AppendLine($@"public async {methodReturnType} {procedureSetting.GetName()}()
                {{");
        }

        {
            if (includeConnectionCreation)
            {
                sb.AppendLine(
                    $@"if (_inTransaction) 
                    {{ 
                        throw new Exception(""Currently in a transaction. This requires accessing methods from the context repository instance within the RunTransaction call.""
                        + "" Please do not use the repository instance defined outside of RunTransaction."");
                    }}");
            }

            if (resultMetaData.ReturnType != ReturnType.None)
            {
                sb.AppendLine($@"var results = new List<{procedureSetting.GetName()}_ResultSet>();");
            }

            if (includeConnectionCreation)
            {
                sb.AppendLine($@"using (SqlConnection connection = new SqlConnection(_connectionString))
            {{");
            }

            // If not creating connection ourselves, assuming this includes the transaction and as such it needs to be added to the sql command
            var transactionAddition = includeConnectionCreation ? "" : ", Transaction = transaction";
            sb.AppendLine($@"using (SqlCommand cm = new SqlCommand(""[{settings.SchemaName}].[{procedureSetting.Proc}]"", connection){{CommandType = CommandType.StoredProcedure{transactionAddition}}})");
            sb.AppendLine("{");
            {
                foreach (var p in parameters)
                {
                    var udtMatch = lookup.GetUserDefinedType(p.TypeSchema, p.TypeName);
                    if (udtMatch != null)
                    {
                        // Data table for user defined types
                        sb.AppendLine($@"var dt{p.CSharpPropertyName()} = new DataTable();");
                        
                        foreach (var udtTypeColumn in udtMatch.Rows)
                        {
                            sb.AppendLine($"dt{p.CSharpPropertyName()}.Columns.Add(\"{udtTypeColumn.ColumnName}\", typeof({udtTypeColumn.CSharpType(isNullable: false)}));");
                        }

                        sb.AppendLine($@"
                            parameters.{p.CSharpPropertyName()}?.ForEach(p => 
                                dt{p.CSharpPropertyName()}.Rows.Add(new object?[]
                                {{
                                    {
                                        string.Join("," + Environment.NewLine, udtMatch.Rows.Select(dt => $@"(object?)p.{dt.ColumnName} ?? DBNull.Value"))
                                    }
                                }}));"
                        );

                        sb.AppendLine(
                            $@"cm.Parameters.Add(new SqlParameter() {{ ParameterName = ""{p.Name}"", SqlDbType = SqlDbType.Structured, Value = dt{p.CSharpPropertyName()}, TypeName = ""{p.TypeSchema}.{p.TypeName}"" }});");
                    }
                    else
                    {
                        var verificationLine = p.ParameterDataVerification();
                        if (!string.IsNullOrWhiteSpace(verificationLine)) sb.AppendLine(verificationLine);
                        // Individual rows
                        if (p.DefaultValue != null && !p.IsOutput)
                        {
                            // A default value in the database means that we only add the SqlParameter if there is a value. 
                            sb.AppendLine($@"if (parameters.{p.CSharpPropertyName()} != null)
                            {{");
                        }

                        {
                            sb.AppendLine(
                                $@"cm.Parameters.Add(new SqlParameter(""{p.Name}"", SqlDbType.{p.SQLDBType()}) 
								{{");
                            {
                                sb.AppendLine($"Value = (object?)parameters.{p.CSharpPropertyName()} ?? DBNull.Value,");
                                if (p.IsOutput) sb.AppendLine($"Direction = ParameterDirection.InputOutput,");
                                if (p.Precision != 0) sb.AppendLine($"Precision = {p.Precision},");
                                if (p.Scale != 0) sb.AppendLine($"Scale = {p.Scale},");
                                if (p.MaxLength != 0) sb.AppendLine($"Size = {p.MaxLength}");
                            }
                            sb.AppendLine($@"}});");
                        }
                        if (p.DefaultValue != null  && !p.IsOutput)
                        {
                            sb.AppendLine($@"}}");
                        }
                    }
                }

                sb.AppendLine($@"if (connection.State != ConnectionState.Open) await connection.OpenAsync();");

                AppendResultAssignment(sb, procedureSetting, parameters, resultMetaData, lookup);

                // sb.AppendLine($@"await connection.CloseAsync();");
            }
            if (includeConnectionCreation)
            {
                sb.AppendLine($"}}");
            }
            sb.AppendLine("}");

            switch (resultMetaData.ReturnType)
            {
                case ReturnType.List:
                    sb.AppendLine("return results;");
                    break;
                case ReturnType.Single:
                    sb.AppendLine("return results.Single();");
                    break;
                case ReturnType.SingleOrDefault:
                    sb.AppendLine("return results.SingleOrDefault();");
                    break;
                case ReturnType.Scalar:
                    sb.AppendLine("return results.Single()." + resultMetaData.Properties.Single().CSharpName + ";");
                    break;
                case ReturnType.Output:
                    sb.AppendLine("return results.Single();");
                    break;
                case ReturnType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        sb.AppendLine($"}}");
        return sb.ToString();
    }

    private static void AppendResultAssignment(StringBuilder sb, ProcedureSetting procedureSetting, List<ParameterDefinition> parameters, ResultMetaData resultMeta, IDataLookup lookup)
    {
        if (resultMeta.ReturnType != ReturnType.None && resultMeta.ReturnType != ReturnType.Output)
        {
            var propertyAssignments = resultMeta.Properties.Select(resultColumn =>
                resultColumn.IsNullable
                    ? $@"{resultColumn.CSharpName} = ({resultColumn.CSharpType}) ConvertDBNullToNull(sdr[""{resultColumn.DatabaseName}""])"
                    : $@"{resultColumn.CSharpName} = ({resultColumn.CSharpType}) sdr[""{resultColumn.DatabaseName}""]"
            ).JoinWithCommaAndNewLines();
            
            sb.AppendLine($@"using (SqlDataReader sdr = await cm.ExecuteReaderAsync()) 
            {{
                while (await sdr.ReadAsync())
                {{
                    results.Add(new {procedureSetting.GetName()}_ResultSet()
					{{
                        {propertyAssignments}
                    }});
                }}
            }}");
        }
        else
        {
            sb.AppendLine($@"await cm.ExecuteNonQueryAsync();");
            if (resultMeta.ReturnType == ReturnType.Output)
            {
                var propertyAssignments = resultMeta.Properties.Select(resultColumn =>
                    resultColumn.IsNullable
                        ? $@"{resultColumn.CSharpName} = ({resultColumn.CSharpType}) ConvertDBNullToNull(cm.Parameters[""{resultColumn.DatabaseName}""].Value)"
                        : $@"{resultColumn.CSharpName} = ({resultColumn.CSharpType}) cm.Parameters[""{resultColumn.DatabaseName}""].Value"
                ).JoinWithCommaAndNewLines();

                sb.AppendLine($@"results.Add(new {procedureSetting.GetName()}_ResultSet()
								{{
                                    {propertyAssignments}
                                }});");
            }
        }

        foreach (var outputParameter in parameters.Where(x => x.IsOutput))
        {
            sb.AppendLine($@"parameters.{outputParameter.CSharpPropertyName()} = ({outputParameter.CSharpType(lookup)}) ConvertDBNullToNull(cm.Parameters[""{outputParameter.Name}""].Value);");
        }
    }

    private static string AddFakeProcedureCallingMethod(ProcedureSetting procedureSetting, List<ParameterDefinition> parameters, string methodReturnType, Settings settings, ResultMetaData resultMeta)
    {
        StringBuilder sb = new StringBuilder();
        var resultSetList = resultMeta.ReturnTypeCSharpString;
        var procName = procedureSetting.GetName();
        if (resultMeta.ReturnType != ReturnType.None && parameters.Any())
        {
            sb.AppendLine($@"
            public {resultSetList} {procName}_Data = new {resultSetList?.Replace("?","")}();

            public Func<{procName}_Parameters, Fake{settings.RepositoryName}, {methodReturnType}> {procName}_Delegate  = (parameters, repository) => Task.FromResult(repository.{procName}_Data);

            public async {methodReturnType} {procName}({procName}_Parameters parameters)
            {{
                return await {procName}_Delegate(parameters, this);            
            }}");

            if (resultMeta.ReturnType == ReturnType.List)
            {
                sb.AppendLine(@$"
                public Fake{settings.RepositoryName} WithData({procName}_ResultSet item)
                {{
                    {procName}_Data.Add(item);
                    return this;                
                }}");
            } 
            else
            {
                sb.AppendLine($@"
                public Fake{settings.RepositoryName} WithData({resultSetList} item)
                {{
                    {procName}_Data = item;                                
                    return this;
                }}");
            }
            
        }
        else if (parameters.Any())
        {
            sb.AppendLine($@"public Func<{procName}_Parameters, Fake{settings.RepositoryName}, Task> {procName}_Delegate = (parameters, repository) => {{ return Task.CompletedTask; }};

            public async {methodReturnType} {procName}({procName}_Parameters parameters)
            {{
                await {procName}_Delegate(parameters, this);
            }}");
        }
        else if (resultMeta.ReturnType != ReturnType.None)
        {
            sb.AppendLine($@"public {resultSetList} {procName}_Data = new {resultSetList}();
            public Func<Fake{settings.RepositoryName}, {methodReturnType}> {procName}_Delegate = (repository) => Task.FromResult(repository.{procName}_Data);

            public async {methodReturnType} {procName}()
            {{
                return await {procName}_Delegate(this);
            }}");

            if (resultMeta.ReturnType == ReturnType.List)
            {
                sb.AppendLine($@"
                public Fake{settings.RepositoryName} WithData({procName}_ResultSet item)
                {{
                    {procName}_Data.Add(item);
                    return this;
                }}");
            }
            else
            {
                sb.AppendLine($@"
                public Fake{settings.RepositoryName} WithData({resultSetList} item)
                {{
                    {procName}_Data = item;
                    return this;
                }}");
            }
        }
        else
        {
            sb.AppendLine($"public Func<Fake{settings.RepositoryName}, Task> {procName}_Delegate = (repository) => {{ return Task.CompletedTask; }};");

            sb.AppendLine();
            sb.AppendLine($"public async {methodReturnType} {procName}()");
            sb.AppendLine($"{{");
            {
                sb.AppendLine($"await {procName}_Delegate(this);");
            }
            sb.AppendLine($"}}");
        }

        return sb.ToString();
    }


    private static List<OutputFile> GenerateUserDefinedTypeClasses(List<UserDefinedTypeGrouping> userDefinedTypeGroup, string settingsNamespace, IDataLookup lookup)
    {

        var results =
            userDefinedTypeGroup
                .Select(dt =>
                    new OutputFile(
                        $"{dt.GetCSharpTypeName()}.generated.cs",
                        WrapInNamespace(
                            $@"public partial class {dt.GetCSharpTypeName()} 
                    {{
                        {CSharpProperties(dt.Rows, lookup: lookup)}
                    }}"
                            , settingsNamespace, false))).ToList();


        return results;
    }

    private static string CSharpProperties(List<ResultPropertyMetaData> resultColumns)
    {
        var sb = new StringBuilder();
        foreach (var resultColumn in resultColumns)
        {
            sb.Append($"public {resultColumn.CSharpType} {resultColumn.CSharpName} {{ get; set; }}");
            if (resultColumn.IsNullable == false && !string.IsNullOrWhiteSpace(resultColumn.DefaultPropertyAssignment))
            {
                sb.Append($" = {resultColumn.DefaultPropertyAssignment};");
            }
            sb.AppendLine();
        }
        return sb.ToString().Trim();
    }

    private static string CSharpProperties(List<ParameterDefinition> parameters, IDataLookup lookup )
    {
        var sb = new StringBuilder();
        foreach (var parameter in parameters)
        {
            sb.AppendLine($"public {parameter.CSharpType(lookup: lookup)} {parameter.CSharpPropertyName()} {{ get; set; }}");
        }

        return sb.ToString().Trim();
    }
    
    private static string CSharpProperties(IEnumerable<UserDefinedTableRowDefinition> dt, IDataLookup lookup)
    {
        var sb = new StringBuilder();
        foreach (var row in dt)
        {
            sb.AppendLine($"public {row.CSharpType(lookup: lookup)} {row.CSharpPropertyName()} {{ get; set; }}");
        }

        return sb.ToString().Trim();
    }
    static string? GetDefaultString(string cSharpType)
    {
        if (cSharpType == "string")
        {
            return "\"\"";
        }
        if (cSharpType == "byte[]")
        {
            return "new byte[0]";
        }
        return null;
    }


    private void VerifyProceduresAreInSchema(List<ProcedureSetting> procedures, List<ProcedureSetting> settingsProcedureList)
    {
        var procNames = procedures.Select(x => x.Proc).ToHashSet();
        var unmatchedProcedures = settingsProcedureList.Where(x => !procNames.Contains(x.Proc, StringComparer.InvariantCultureIgnoreCase)).ToHashSet();
        if (unmatchedProcedures.Any())
        {
            var error = "Some specified procedures don't exist in the schema: "
                        + string.Join(", ", unmatchedProcedures.Select(proc => proc.GetName()));
            Errors.Add(error);
        }
    }
    
        
}