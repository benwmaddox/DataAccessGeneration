namespace DataAccessGeneration.XUnitTest;

public class StringCamelCaseTests
{
    [Theory()]
    [InlineData("dateRange", "dateRange")]
    [InlineData("DateRange", "dateRange")]
    [InlineData("DATE_RANGE", "dateRange")]
    [InlineData("UserIDs", "userIds")]
    [InlineData("userIDs", "userIds")]
    [InlineData("userID", "userId")]
    [InlineData("user_ID", "userId")]
    public void CamelCaseConversion(string input, string expectedOutput)
    {
        Assert.Equal(expectedOutput, input.ToCamelCase());
    }

}