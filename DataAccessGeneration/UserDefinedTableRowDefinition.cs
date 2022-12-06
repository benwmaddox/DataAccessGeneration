namespace DataAccessGeneration
{
    public class UserDefinedTableRowDefinition
	{
		public string TableTypeName { get; set; } = "";
		public string ColumnName { get; set; } = "";
		public string TypeName { get; set; } = "";
		public bool? IsNullable { get; set; }
		public short? MaxLength { get; set; }
		public byte? Precision { get; set; }
		public byte? Scale { get; set; }
		public string SchemaName { get; set; } = "";
	}
}