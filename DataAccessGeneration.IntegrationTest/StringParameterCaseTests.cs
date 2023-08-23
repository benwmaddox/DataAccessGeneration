namespace DataAccessGeneration.XUnitTest;

public class StringParameterCaseTests
{
    [Theory()]
    [InlineData("DATERange", "dateRange")]
    [InlineData("dateRange", "dateRange")]
    [InlineData("DateRange", "dateRange")]
    [InlineData("DATE_RANGE", "dateRange")]
    [InlineData("UserIDs", "userIDs")]
    [InlineData("userIDs", "userIDs")]
    [InlineData("userID", "userId")]
    [InlineData("user_ID", "userId")]
    public void ParameterCaseConversion(string input, string expectedOutput)
    {
        Assert.Equal(expectedOutput, input.ToParameterCase());
    }

}