using System.Data;
using System.Text.RegularExpressions;


namespace DataAccessGeneration
{
    public static class TypeConversions {
		public static string SQLDBType(this ParameterDefinition p) => getSqlDbTypeEnumFromSQLType(p.TypeName);
		public static string getSqlDbTypeEnumFromSQLType(string typeName)
		{
			if (typeName == "varchar") return "VarChar";
			if (typeName == "nvarchar") return "NVarChar";
			if (typeName == "int") return "Int";
			if (typeName == "tinyint") return "TinyInt";
			if (typeName == "smallint") return "SmallInt";
			if (typeName == "bit") return "Bit";
			if (typeName == "uniqueidentifier") return "UniqueIdentifier";
			if (typeName == "datetime") return "DateTime";
			if (typeName == "datetime2") return "None";
			if (typeName == "datetimeoffset") return nameof(SqlDbType.DateTimeOffset);
			if (typeName == "char") return nameof(SqlDbType.Char);
			if (typeName == "date") return "Date";
			if (typeName == "smalldatetime") return "SmallDateTime";
			if (typeName == "decimal") return "Decimal";
			if (typeName == "numeric") return "Decimal";
			if (typeName == "nvarchar") return "NVarChar";
			if (typeName == "sysname") return "VarChar";
			if (typeName == "text") return "Text";
			if (typeName == "ntext") return "Text";
			if (typeName == "float") return "Float";
			if (typeName == "binary") return "Binary";
			if (typeName == "varbinary") return "VarBinary";
			if (typeName == "money") return "Money";
			if (typeName == "bigint") return "BigInt";
			if (typeName == "real") return "Real";
			if (typeName == "sql_variant") return "SQLVariant";
			if (typeName == "image") return "Image";
			if (typeName == "hierarchyid") return nameof(SqlDbType.Binary);
			if (typeName == "geometry") return nameof(SqlDbType.Binary);
			if (typeName == "nchar") return nameof(SqlDbType.NChar);
			if (typeName == "geography") return nameof(SqlDbType.Binary);
			if (typeName == "smallmoney") return nameof(SqlDbType.SmallMoney);
			if (typeName == "timestamp") return nameof(SqlDbType.Timestamp);
			if (typeName == "time") return nameof(SqlDbType.Time);
			if (typeName == "xml") return nameof(SqlDbType.Xml);

			throw new Exception("Unknown SQL TYPE: " + typeName);

		}

		public static string? DatabaseDefaultValue(this ParameterDefinition p) => getDatabaseDefaultValueFromTypeName(p.TypeName);

		public static string? getDatabaseDefaultValueFromTypeName(string typeName)
		{
			switch (typeName)
			{
				case "varchar":
					return "''";
				case "nvarchar":
					return "''";
				case "char":
					return "''";
				case "nchar":
					return "''";
				case "xml":
					return "'<body></body>'";
				case "int":
					return "0";
				case "tinyint":
					return "0";
				case "smallint":
					return "0";
				case "bit":
					return "0";
				case "datetime":
					return "'2022-11-10 09:00:00'";
				case "datetime2":
					return "'2022-11-10 09:00:00'";
				case "datetimeoffset":
					return "'2022-11-10 09:00:00'";
				case "timestamp":
					return "'2022-11-10 09:00:00'";
				case "smalldatetime":
					return "'2022-11-10 09:00:00'";
				case "date":
					return "'2022-11-10'";
				case "time":
					return "'09:00:00'";
				case "image":
					return "NULL";
				case "text":
					return "''";
				case "ntext":
					return "''";
				case "real":
					return "0";
				case "money":
					return "0";
				case "float":
					return "0";
				case "decimal":
					return "0";
				case "numeric":
					return "0";
				case "smallmoney":
					return "0";
				case "bigint":
					return "0";
				case "hierarchyid":
					return "'/'";
				case "geometry":
					return "geometry::Parse('POINT(4 5 6 3.5)')";
				case "geography":
					return "geography::STGeomFromText('LINESTRING(-122.360 47.656, -122.343 47.656 )', 4326)";
				case "varbinary":
					return "0";
				case "binary":
					return "0";
				case "uniqueidentifier":
					return $"'{Guid.NewGuid().ToString()}'";
				case "sql_variant":
					return "NULL";
					
				default:
					return null;
			}
		}

		public static string CSharpType(this ParameterDefinition p, List<string>? userDefinedTypeNames = null) => getCSharpTypeFromSQLType(p.TypeName, p.MaxLength, p.Precision, p.Scale, true, userDefinedTypeNames);
		public static string CSharpType(this UserDefinedTableRowDefinition r, bool? isNullable = null,  List<string>? userDefinedTypeNames = null) => getCSharpTypeFromSQLType(r.TypeName, r.MaxLength, r.Precision, r.Scale, isNullable ?? r.IsNullable, userDefinedTypeNames);
		public static string CSharpType(this ResultDefinition r, List<string>? userDefinedTypeNames = null) => getCSharpTypeFromSQLType(r.TypeName, r.MaxLength, r.Precision, r.Scale, r.IsNullable, userDefinedTypeNames);

		public static string getCSharpTypeFromSQLType(string typeName, short? maxLength = null, byte? precision = null, byte? scale = null, bool? isNullable = true, List<string>? userDefinedTypes = null)
		{
			if (typeName == "varchar") return "string" + (isNullable == true ? "?" : "");
			if (typeName == "nvarchar") return "string" + (isNullable == true ? "?" : "");
			if (typeName == "int") return "int" + (isNullable == true ? "?" : "");
			if (typeName == "tinyint") return "byte" + (isNullable == true ? "?" : "");
			if (typeName == "smallint") return "short" + (isNullable == true ? "?" : "");
			if (typeName == "bit") return "bool" + (isNullable == true ? "?" : "");
			if (typeName == "uniqueidentifier") return "Guid" + (isNullable == true ? "?" : "");
			if (typeName == "datetime") return "DateTime" + (isNullable == true ? "?" : "");
			if (typeName == "datetime2") return "DateTime" + (isNullable == true ? "?" : "");
			if (typeName == "datetimeoffset") return "DateTimeOffset";
			// I expected it to be a char for single characters, but it seems ADO.net treats it as a string. 
			// It could make sense if it is more than 1 character
			if (typeName == "char") return "string" + (isNullable == true ? "?" : "");
			if (typeName == "date") return "DateTime" + (isNullable == true ? "?" : "");
			if (typeName == "smalldatetime") return "DateTime" + (isNullable == true ? "?" : "");
			if (typeName == "decimal") return "decimal" + (isNullable == true ? "?" : "");
			if (typeName == "numeric") return "decimal" + (isNullable == true ? "?" : "");
			if (typeName == "sysname") return "string" + (isNullable == true ? "?" : "");
			if (typeName == "text") return "string" + (isNullable == true ? "?" : "");
			if (typeName == "ntext") return "string" + (isNullable == true ? "?" : "");
			if (typeName == "float") return "double" + (isNullable == true ? "?" : "");
			if (typeName == "binary") return "byte[]" + (isNullable == true ? "?" : "");
			if (typeName == "varbinary") return "byte[]" + (isNullable == true ? "?" : "");
			if (typeName == "money") return "decimal" + (isNullable == true ? "?" : "");
			if (typeName == "bigint") return "long" + (isNullable == true ? "?" : "");
			if (typeName == "real") return "float" + (isNullable == true ? "?" : "");
			if (typeName == "sql_variant") return "object" + (isNullable == true ? "?" : "");
			if (typeName == "image") return "object" + (isNullable == true ? "?" : "");
			if (typeName == "hierarchyid") return "object" + (isNullable == true ? "?" : "");
			if (typeName == "geometry") return "object" + (isNullable == true ? "?" : "");
			if (typeName == "nchar") return "string" + (isNullable == true ? "?" : "");
			if (typeName == "geography") return "object" + (isNullable == true ? "?" : "");
			if (typeName == "smallmoney") return "object" + (isNullable == true ? "?" : "");
			if (typeName == "timestamp") return "DateTime" + (isNullable == true ? "?" : "");
			if (typeName == "time") return "TimeSpan" + (isNullable == true ? "?" : "");
			if (typeName == "xml") return "string" + (isNullable == true ? "?" : "");
			if (userDefinedTypes != null && userDefinedTypes.Contains(typeName)) return $"List<{typeName}>" + (isNullable == true ? "?" : "");

			throw new Exception("Unknown SQL TYPE: " + typeName);
		}



		public static string CSharpPropertyName(this ParameterDefinition parameter) => convertSQLColumnNameToCSharpPropertyName(parameter.Name);
		public static string CSharpPropertyName(this UserDefinedTableRowDefinition parameter) => convertSQLColumnNameToCSharpPropertyName(parameter.ColumnName);
		public static string CSharpPropertyName(this ResultDefinition rd) => convertSQLColumnNameToCSharpPropertyName(rd.Name);
		public static string convertSQLColumnNameToCSharpPropertyName(string input)
		{
			var result = input.Replace("@", "")
				.Replace(" ", "_")
				.Replace("#", "Number")
				;
			// C# can't accept properties starting with a digit
			var startsWithDigit = new Regex("^\\d.*");
			if (startsWithDigit.IsMatch(result))
			{
				result = "N_" + result;
			}

			return result;
		}

		public static string ToCamelCase(this string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			if (input.Length > 1)
				return char.ToLowerInvariant(input[0]) + input.Substring(1);

			return input.ToLowerInvariant();
		}

		/// <summary>
		/// Sometimes the conversion from C# types isn't exact and this will force a conversion at runtime before calling to the database.
		/// The stack trace should be more precise because it would hit prior to executing the database call.
		/// </summary>
		public static string? ParameterDataVerification(this ParameterDefinition parameter)
		{
			if (parameter.TypeName == "datetime")
			{
				return $"SqlDateTime? verify{parameter.CSharpPropertyName()} = (SqlDateTime?)parameters.{parameter.CSharpPropertyName()};";
			}
			
			// In the future, we could add string length checks and decimal scale/precision checks
			return null;
		}
    }
}