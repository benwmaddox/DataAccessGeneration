namespace DataAccessGeneration.XUnitTest;

public class StringCamelCaseTests
{
    [Theory()]
    [InlineData("DATERange", "dateRange")]
    [InlineData("dateRange", "dateRange")]
    [InlineData("DateRange", "dateRange")]
    [InlineData("DATE_RANGE", "dateRange")]
    public void CamelCaseConversion(string input, string expectedOutput)
    {
        Assert.Equal(expectedOutput, input.ToCamelCase());
    }

}