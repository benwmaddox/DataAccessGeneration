using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace DataAccessGeneration;

public class DataLookup : IDataLookup
{
    private string _connectionString;

    public DataLookup(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<string> GetProceduresForSchema(string schemaName)
    {
        var results = new List<string>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {

            SqlCommand cm = new SqlCommand($@"
SELECT p.name FROM sys.procedures p
JOIN sys.schemas s ON p.schema_id = s.schema_id
WHERE s.name = '{schemaName}'", connection);

            cm.CommandTimeout = 120000;
            // Opening Connection  
            connection.Open();
            // Executing the SQL query  
            SqlDataReader sdr = cm.ExecuteReader();
            while (sdr.Read())
            {
                results.Add((string)sdr["name"]);
            }

        }

        return results;
    }

    private string? GetProcedureBody(string schemaName, string procedureName)
    {
        var results = new List<string>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {

            SqlCommand cm = new SqlCommand($@"
SELECT definition FROM sys.sql_modules
WHERE OBJECT_ID =  OBJECT_ID('{schemaName}.{procedureName}')
", connection);

            cm.CommandTimeout = 120000;
            // Opening Connection  
            connection.Open();
            // Executing the SQL query  
            SqlDataReader sdr = cm.ExecuteReader();
            while (sdr.Read())
            {
                results.Add((string)sdr["definition"]);
            }

        }

        return results.FirstOrDefault();
    }

    public List<ParameterDefinition> GetParametersForProcedure(string schemaName, string procedureName)
    {
        var results = new List<ParameterDefinition>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            SqlCommand cm = new SqlCommand($@"
SELECT p.name as ParameterName, ISNULL(t.name, ut.name) as TypeName, ISNULL(ts.name, uts.name) as TypeSchema, IsNull(isp.character_maximum_length, Cast(p.max_length AS int)) AS max_length,  p.precision, p.scale, p.system_type_id, p.is_output
FROM sys.parameters p 
LEFT JOIN sys.types t ON p.system_type_id = t.system_type_id AND t.is_user_defined = 0 AND t.name <> 'sysname'
LEFT JOIN sys.types ut ON p.user_type_id = ut.user_type_id AND ut.name <> 'AUTO_ID'
LEFT JOIN sys.schemas ts ON t.schema_id = ts.schema_id
LEFT JOIN sys.schemas uts ON ut.schema_id = uts.schema_id
LEFT JOIN INFORMATION_SCHEMA.PARAMETERS isp ON p.name = isp.parameter_name AND isp.SPECIFIC_SCHEMA = '{schemaName}' AND isp.SPECIFIC_NAME = '{procedureName}'
WHERE p.OBJECT_ID = OBJECT_ID('{schemaName}.{procedureName}')
AND ISNULL(ut.name, t.name) IS NOT null
ORDER BY p.parameter_id
", connection);

            cm.CommandTimeout = 120000;
            connection.Open();
            SqlDataReader sdr = cm.ExecuteReader();
            while (sdr.Read())
            {
                results.Add(new ParameterDefinition()
                {
                    Name = (string)sdr["ParameterName"],
                    TypeName = (string)sdr["TypeName"],
                    TypeSchema = (string)sdr["TypeSchema"],
                    MaxLength = (int)sdr["max_length"],
                    Precision = (byte)sdr["precision"],
                    Scale = (byte)sdr["scale"],
                    IsOutput = (bool)sdr["is_output"]
                });
            }
        }

        var procedureBody = GetProcedureBody(schemaName, procedureName);
        if (procedureBody != null)
        {
            var defaultValues = DetermineDefaultValues(procedureBody);
            foreach (var result in results)
            {
                if (defaultValues.ContainsKey(result.Name))
                {
                    result.DefaultValue = defaultValues[result.Name];
                }
            }
        }

        return results;

    }


    // Getting parameter name, ignoring the type (\s*\S*\s*), and getting default value
    private static Regex defaultMatch = new Regex(@"(?<parameterName>(@\S*))\s*\S*\s*=\s*(?<defaultValue>\S*)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
    private static Regex firstAsMatch = new Regex(@"\sAS\s", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public static Dictionary<string, string> DetermineDefaultValues(string procedureBody)
    {
        var firstAs = firstAsMatch.IsMatch(procedureBody) ? firstAsMatch.Match(procedureBody).Index : -1;
        if (firstAs == -1) return new Dictionary<string, string>();
        var parameterInformation = procedureBody.Substring(0, firstAs);
        var matches = defaultMatch.Matches(parameterInformation);
        var results = new Dictionary<string, string>();
        foreach (Match match in matches)
        {
            string value = match.Groups["defaultValue"].Value;
            if (value.EndsWith(",")) value = value.Substring(0, value.Length - 1);
            if (value.EndsWith(")")) value = value.Substring(0, value.Length - 1);
            results[match.Groups["parameterName"].Value] = value;
        }

        return results;
    }

    private object? ConvertDBNullToNull(object item)
    {
        return item == DBNull.Value ? null : item;
    }


    public List<ResultDefinition> GetResultDefinitionsForProcedures(string schemaName, string procedureName, List<ParameterDefinition> parameters, bool executeDuringGeneration)
    {
        var results = new List<ResultDefinition>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            SqlCommand cm = new SqlCommand($@"SELECT rs.name, rs.is_nullable, ISNULL(t.name, ut.name) AS typeName, rs.max_length, rs.precision, rs.scale
FROM sys.dm_exec_describe_first_result_set('{schemaName}.{procedureName}', NULL, 0) rs
LEFT JOIN sys.types t ON rs.system_type_id = t.system_type_id AND t.is_user_defined = 0 AND t.name <> 'sysname'
LEFT JOIN sys.types ut ON rs.user_type_id = ut.user_type_id  AND ut.name <> 'AUTO_ID'
WHERE rs.name IS NOT null
ORDER BY rs.column_ordinal
", connection);
            cm.CommandTimeout = 120000;
            connection.Open();

            SqlDataReader sdr = cm.ExecuteReader();
            while (sdr.Read())
            {
                results.Add(new ResultDefinition()
                {
                    Name = (string)sdr["name"],
                    TypeName = (string)sdr["typeName"],
                    IsNullable = (bool)sdr["is_nullable"],
                    MaxLength = (short)sdr["max_length"],
                    Precision = (byte)sdr["precision"],
                    Scale = (byte)sdr["scale"],
                });
            }
        }

        // If can't use normal means to load the result set, try to get it from the procedure call. But it depends on knowing all the parameter default values
        if (!results.Any() && executeDuringGeneration)
        {
            try
            {
                // User defined types have to be declares prior to the query execution
                var query =
                    string.Join(" ",
                        parameters.Where(x => x.DatabaseDefaultValue() == null).Select(p => $"DECLARE {p.Name} {p.TypeSchema}.{p.TypeName};"
                        )) +
                    $@" EXEC {schemaName}.{procedureName} {string.Join(", ", parameters.Select(p =>
                        p.DatabaseDefaultValue() != null ?
                            // Default value for known types
                            $"{p.Name} = {p.DatabaseDefaultValue()}"
                            // Using the UDT variable names
                            : $"{p.Name} = {p.Name}"
                    ))}";
                results = GetResultDefinitionForProcedureExecution(query);
            }
            catch
            {
                // Not a huge deal since this was a fallback attempt
            }

        }

        return results;
    }

    public List<ResultDefinition> GetResultDefinitionForProcedureExecution(string procedureCallingCode)
    {
        var results = new List<ResultDefinition>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                SqlCommand cm = new SqlCommand(procedureCallingCode, connection);
                cm.CommandTimeout = 120000;
                cm.Transaction = transaction;
                try
                {
                    SqlDataReader sdr = cm.ExecuteReader();
                    for (int i = 0; i < sdr.FieldCount; i++)
                    {
                        results.Add(new ResultDefinition()
                        {
                            Name = sdr.GetName(i),
                            TypeName = sdr.GetDataTypeName(i),
                            IsNullable = sdr.GetSchemaTable().Rows[i]["AllowDBNull"] as bool? ?? false,
                            MaxLength = sdr.GetSchemaTable().Rows[i]["ColumnSize"] as short? ?? 0,
                            Precision = sdr.GetSchemaTable().Rows[i]["NumericPrecision"] as byte? ?? 0,
                            Scale = sdr.GetSchemaTable().Rows[i]["NumericScale"] as byte? ?? 0,
                        });
                    }
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                transaction.Rollback();
            }
        }

        return results;
    }


    private List<UserDefinedTypeGrouping>? _udtGroupings = null;

    public UserDefinedTypeGrouping? GetUserDefinedType(string schemaName, string udtName)
    {
        _udtGroupings ??= GetUserDefinedTypes();
        var match = _udtGroupings.SingleOrDefault(x => x.SchemaName == schemaName && x.UDTTypeName == udtName);
        if (match != null)
        {
            match.RetrievedForUse = true;
        }

        return match;
    }

    public string? GetResultDefinitionsErrorsForProcedures(string schemaName, string procedureName)
    {
        var results = new List<string?>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            SqlCommand cm = new SqlCommand($@"SELECT error_message
FROM sys.dm_exec_describe_first_result_set('{schemaName}.{procedureName}', NULL, 0) rs
", connection);

            cm.CommandTimeout = 120000;
            connection.Open();

            SqlDataReader sdr = cm.ExecuteReader();
            while (sdr.Read())
            {
                results.Add((string?)ConvertDBNullToNull(sdr["error_message"]));
            }
        }

        return results.FirstOrDefault(x => x != null);
    }

    public List<UserDefinedTypeGrouping> GetUserDefinedTypes()
    {
        var results = new List<UserDefinedTableRowDefinition>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            // Distinct is a current workaround. TODO: Add in tt.schema_id or SCHEMA_NAME(tt.schema_id) to handle cases where the same type name is used in multiple schemas.
            // Requires coordinating with other portions of code to pick the correct user defined type per procedure

            SqlCommand cm = new SqlCommand($@"
SELECT DISTINCT tt.name AS TableTypeName, c.name AS ColumnName, t.max_length, t.precision, t.scale, t.is_nullable, ISNULL(ut.name, t.name) AS TypeName, c.column_id, s.name as SchemaName
FROM sys.table_types tt
JOIN sys.columns c ON tt.type_table_object_id = c.object_id
join sys.schemas s on tt.schema_id = s.schema_id
LEFT JOIN sys.types t ON c.system_type_id = t.system_type_id AND t.is_user_defined = 0 AND t.name <> 'sysname' 
LEFT JOIN sys.types ut ON c.user_type_id = ut.user_type_id AND ut.name <> 'AUTO_ID'
WHERE tt.is_user_defined = 1
ORDER BY s.name, c.column_id
", connection);
            cm.CommandTimeout = 120000;
            connection.Open();

            SqlDataReader sdr = cm.ExecuteReader();
            while (sdr.Read())
            {
                results.Add(new UserDefinedTableRowDefinition()
                {
                    TableTypeName = (string)sdr["TableTypeName"],
                    SchemaName = (string)sdr["SchemaName"],
                    ColumnName = (string)sdr["ColumnName"],
                    TypeName = (string)sdr["TypeName"],
                    IsNullable = (bool)sdr["is_nullable"],
                    MaxLength = (short)sdr["max_length"],
                    Precision = (byte)sdr["precision"],
                    Scale = (byte)sdr["scale"],
                });
            }
        }

        var groupings = results.GroupBy(x => new { x.SchemaName, x.TableTypeName }).Select(x => new UserDefinedTypeGrouping()
        {
            UDTTypeName = x.Key.TableTypeName,
            SchemaName = x.Key.SchemaName,
            Rows = x.ToList()
        }).ToList();

        foreach (var item in groupings)
        {
            item.RequiresSchemaToDisambiguate = groupings.Any(x => x.UDTTypeName == item.UDTTypeName && x.SchemaName != item.SchemaName);
        }

        _udtGroupings = groupings;
        return groupings;

    }

    public List<ResultDefinition> GetSystemTypes()
    {
        var results = new List<ResultDefinition>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            SqlCommand cm = new SqlCommand($@"SELECT t.name, t.is_nullable, t.name AS typeName, t.max_length, t.precision, t.scale
FROM sys.types t
WHERE is_user_defined = 0
AND name <> 'sysname'
", connection);

            cm.CommandTimeout = 120000;
            connection.Open();

            SqlDataReader sdr = cm.ExecuteReader();
            while (sdr.Read())
            {
                results.Add(new ResultDefinition()
                {
                    Name = (string)sdr["name"],
                    TypeName = (string)sdr["typeName"],
                    IsNullable = (bool)sdr["is_nullable"],
                    MaxLength = (short)sdr["max_length"],
                    Precision = (byte)sdr["precision"],
                    Scale = (byte)sdr["scale"],
                });
            }
        }

        return results;
    }
}