using System.Linq;
using Xunit;

namespace DataAccessGeneration.XUnitTest;
public class ProcedureParsing
{
    [Fact]
    public void GetDefaultValue()
    {
        string procedureBody = @"CREATE PROCEDURE SalesByCategory
    @CategoryName nvarchar(15), @OrdYear nvarchar(4) = '1998'
AS
IF @OrdYear != '1996' AND @OrdYear != '1997' AND @OrdYear != '1998' 
BEGIN
	SELECT @OrdYear = '1998'
END

SELECT ProductName,
	TotalPurchase=ROUND(SUM(CONVERT(decimal(14,2), OD.Quantity * (1-OD.Discount) * OD.UnitPrice)), 0)
FROM [Order Details] OD, Orders O, Products P, Categories C
WHERE OD.OrderID = O.OrderID 
	AND OD.ProductID = P.ProductID 
	AND P.CategoryID = C.CategoryID
	AND C.CategoryName = @CategoryName
	AND SUBSTRING(CONVERT(nvarchar(22), O.OrderDate, 111), 1, 4) = @OrdYear
GROUP BY ProductName
ORDER BY ProductName
";

        var result = DataLookup.DetermineDefaultValues(procedureBody);
        Assert.Single(result);
        Assert.Equal("@OrdYear", result.First().Key);
        Assert.Equal("'1998'", result.First().Value);
    }
    
    [Fact]
    public void GetMultipleDefaultValue()
    {
        
        string procedureBody = @"CREATE PROCEDURE SalesByCategory
    @CategoryName nvarchar(15) = 'NoName', @OrdYear nvarchar(4) = '1998'
AS
IF @OrdYear != '1996' AND @OrdYear != '1997' AND @OrdYear != '1998' 
BEGIN
	SELECT @OrdYear = '1998'
END

SELECT ProductName,
	TotalPurchase=ROUND(SUM(CONVERT(decimal(14,2), OD.Quantity * (1-OD.Discount) * OD.UnitPrice)), 0)
FROM [Order Details] OD, Orders O, Products P, Categories C
WHERE OD.OrderID = O.OrderID 
	AND OD.ProductID = P.ProductID 
	AND P.CategoryID = C.CategoryID
	AND C.CategoryName = @CategoryName
	AND SUBSTRING(CONVERT(nvarchar(22), O.OrderDate, 111), 1, 4) = @OrdYear
GROUP BY ProductName
ORDER BY ProductName
";


        var result = DataLookup.DetermineDefaultValues(procedureBody);
        Assert.Equal(2, result.Count);
        Assert.Equal("@CategoryName", result.First().Key);
        Assert.Equal("'NoName'", result.First().Value);
    }
}