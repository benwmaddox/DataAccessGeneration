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

    public class UserDefinedTableGrouping
    {
        public string SchemaName { get; set; } = "";
        public string UDTTypeName { get; set; } = "";
        public List<UserDefinedTableRowDefinition> Rows { get; set; } = new List<UserDefinedTableRowDefinition>();
        public bool RequiresSchemaToDisambiguate { get; set; }
        public bool RetrievedForUse { get; set; }
        public string GetCSharpTypeName() => RequiresSchemaToDisambiguate ? $"{SchemaName}_{UDTTypeName}" : UDTTypeName;
    }
}