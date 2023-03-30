namespace DataAccessGeneration
{
    public class ResultDefinition
	{
		public string Name { get; set; } = "";
		public string TypeName { get; set; } = "";
		public bool? IsNullable { get; set; }
		public int? MaxLength { get; set; }
		public byte? Precision { get; set; }
		public byte? Scale { get; set; }
	}
}