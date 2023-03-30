namespace DataAccessGeneration
{
    public class ParameterDefinition
	{
		public string Name { get; set; } = "";
		public string TypeName { get; set; } = "";
		public int MaxLength { get; set; }
		public byte Precision { get; set; }
		public byte Scale { get; set; }
		public bool IsOutput { get; set; }
		public string? DefaultValue { get; set; }
		public string TypeSchema { get; set; } = "";
	}
}