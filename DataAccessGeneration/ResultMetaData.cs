namespace DataAccessGeneration;

public class ResultMetaData
{
    public ReturnType ReturnType { get; set; } = ReturnType.List;
    public List<ResultPropertyMetaData> Properties { get; set; } = new List<ResultPropertyMetaData>();
    public string? ReturnTypeCSharpString { get; set; } = "";
}