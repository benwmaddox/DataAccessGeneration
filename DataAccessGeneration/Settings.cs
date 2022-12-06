namespace DataAccessGeneration
{
    public class Settings
    {
		public string ConnectionString { get; set; } = "";
		public string SchemaName { get; set; } = "";
        public string Namespace { get; set; } = "";
        public string OutputRelativePath { get; set; } = "";
        // If there are any items in the procedure list, load only those
        public List<ProcedureSetting> ProcedureList { get; set; } = new List<ProcedureSetting>();
        public bool IncludeFakes { get; set; } = true;
        public string? RepositoryName { get; set; } = null;
        
    }

}