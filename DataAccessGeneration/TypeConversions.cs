﻿using System.Data;
using System.Linq;
using System.Text.RegularExpressions;


namespace DataAccessGeneration
{
    public static class TypeConversions {
		public static string SQLDBType(this ParameterDefinition p) => getSqlDbTypeEnumFromSQLType(p.TypeName);
		public static string getSqlDbTypeEnumFromSQLType(string typeName)
		{
			
			if (typeName == "varchar") return nameof(SqlDbType.VarChar);
			if (typeName == "nvarchar") return nameof(SqlDbType.NVarChar);
			if (typeName == "int") return nameof(SqlDbType.Int);
			if (typeName == "tinyint") return nameof(SqlDbType.TinyInt);
			if (typeName == "smallint") return nameof(SqlDbType.SmallInt);
			if (typeName == "bit") return nameof(SqlDbType.Bit);
			if (typeName == "uniqueidentifier") return nameof(SqlDbType.UniqueIdentifier);
			if (typeName == "datetime") return nameof(SqlDbType.DateTime);
			if (typeName == "datetime2") return nameof(SqlDbType.DateTime2);
			if (typeName == "datetimeoffset") return nameof(SqlDbType.DateTimeOffset);
			if (typeName == "char") return nameof(SqlDbType.Char);
			if (typeName == "date") return nameof(SqlDbType.Date);
			if (typeName == "smalldatetime") return nameof(SqlDbType.SmallDateTime);
			if (typeName == "decimal") return nameof(SqlDbType.Decimal);
			if (typeName == "numeric") return nameof(SqlDbType.Decimal);
			if (typeName == "nvarchar") return nameof(SqlDbType.NVarChar);
			if (typeName == "sysname") return nameof(SqlDbType.VarChar);
			if (typeName == "text") return nameof(SqlDbType.Text);
			if (typeName == "ntext") return nameof(SqlDbType.Text);
			if (typeName == "float") return nameof(SqlDbType.Float);
			if (typeName == "binary") return nameof(SqlDbType.Binary);
			if (typeName == "varbinary") return nameof(SqlDbType.VarBinary);
			if (typeName == "money") return nameof(SqlDbType.Money);
			if (typeName == "bigint") return nameof(SqlDbType.BigInt);
			if (typeName == "real") return nameof(SqlDbType.Real);
			if (typeName == "sql_variant") return nameof(SqlDbType.Variant);
			if (typeName == "image") return nameof(SqlDbType.Image);
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

		public static string CSharpType(this ParameterDefinition p, IDataLookup? lookup) => getCSharpTypeFromSQLType(p.TypeSchema, p.TypeName, p.MaxLength, p.Precision, p.Scale, true, lookup);
		public static string CSharpType(this UserDefinedTableRowDefinition r, bool? isNullable = null, IDataLookup? lookup = null) => getCSharpTypeFromSQLType(r.SchemaName, r.TypeName, r.MaxLength, r.Precision, r.Scale, isNullable ?? r.IsNullable, lookup);
		public static string CSharpType(this ResultDefinition r, IDataLookup? lookup = null) => getCSharpTypeFromSQLType(null, r.TypeName, r.MaxLength, r.Precision, r.Scale, r.IsNullable, lookup);

		public static string getCSharpTypeFromSQLType(string? schemaName, string typeName, int? maxLength = null, byte? precision = null, byte? scale = null, bool? isNullable = true, IDataLookup? lookup = null)
		{
			var cSharpToSqlConversion = new Dictionary<string, string>()
			{
				{ "varchar", "string" },
				{ "nvarchar", "string" },
				{ "int", "int" },
				{ "tinyint", "byte" },
				{ "smallint", "short" },
				{ "bit", "bool" },
				{ "uniqueidentifier", "Guid" },
				{ "datetime", "DateTime" },
				{ "datetime2", "DateTime" },
				{ "datetimeoffset", "DateTimeOffset" },
			// I expected it to be a char for single characters, but it seems ADO.net treats it as a string. 
			// It could make sense if it is more than 1 character
				{ "char", "string" },
				{ "date", "DateTime" },
				{ "smalldatetime", "DateTime" },
				{ "decimal", "decimal" },
				{ "numeric", "decimal" },
				{ "sysname", "string" },
				{ "text", "string" },
				{ "ntext", "string" },
				{ "float", "double" },
				{ "binary", "byte[]" },
				{ "varbinary", "byte[]" },
				{ "money", "decimal" },
				{ "bigint", "long" },
				{ "real", "float" },
				{ "sql_variant", "object" },
				{ "image", "object" },
				{ "hierarchyid", "object" },
				{ "geometry", "object" },
				{ "nchar", "string" },
				{ "geography", "object" },
				{ "smallmoney", "object" },
				{ "timestamp", "DateTime" },
				{ "time", "TimeSpan" },
				{ "xml", "string" },
			};
			if (cSharpToSqlConversion.ContainsKey(typeName))
			{
				return cSharpToSqlConversion[typeName] + (isNullable == true ? "?" : "");
			}
			var udt = lookup?.GetUserDefinedType(schemaName ?? "", typeName) ;

            if (udt != null) return $"List<{udt.GetCSharpTypeName()}>" + (isNullable == true ? "?" : "");

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

		public static string ToParameterCase(this string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

            string[] split = input.Split("_")
                .Select(x => x.SplitForParameterCase())
                .SelectMany(x => x)
                .Where(x => x.Length > 0)
                .ToArray();

            split[0] = split[0].ToLower();
            for (var i = 1; i < split.Length;i++)
            {
				if (split[i].Length == 0) continue;

                split[i] = char.ToUpper(split[i][0]) + split[i].Substring(1).ToLower();
            }

            return string.Join("", split);
        }

        public static string[] SplitForParameterCase(this string source)
        {
            return Regex.Split(source, @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z]*)(?=[A-Z][a-z]+)");
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