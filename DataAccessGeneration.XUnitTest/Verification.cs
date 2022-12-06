using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace DataAccessGeneration.XUnitTest;

public class Verification
{

    [Fact]
    public static void VerifyIndentationSingleClass()
    {
        var initial = @"
public partial class SampleProc_Parameters
							{
								public string? Field1 { get; set; }
								public int? Field2 { get; set; }		
							}";
        var expected = @"
public partial class SampleProc_Parameters
{
    public string? Field1 { get; set; }
    public int? Field2 { get; set; }
}";
        var actual = StringManipulation.IndentBasedOnBraces(initial);

        if (actual != expected) NotEqualError(expected, actual);
    }
    
    [Fact]
    public static void VerifyIndentationNestedBraces()
    {
        var initial = @"
public partial class SampleRepository
							{
								        public async Task<List<CustOrderHist_ResultSet>> CustOrderHist(string? customerID)
        {
            var parameters = new CustOrderHist_Parameters()
            {
                CustomerID = customerID
            };
            return await CustOrderHist(parameters);
        }		
							}";
        var expected = @"
public partial class SampleRepository
{
    public async Task<List<CustOrderHist_ResultSet>> CustOrderHist(string? customerID)
    {
        var parameters = new CustOrderHist_Parameters()
        {
            CustomerID = customerID
        };
        return await CustOrderHist(parameters);
    }
}";
        var actual = StringManipulation.IndentBasedOnBraces(initial);

        if (actual != expected) NotEqualError(expected, actual);
    }

    [Fact]
    public void VerifyGenerateParameterDefinition()
    {
        string? actual = GenerateParameterDefinition(new List<ParameterDefinition>()
                         {
                             new ParameterDefinition()
                             {
                                 Name = "Field1",
                                 TypeName = "varchar"
                             },
                             new ParameterDefinition()
                             {
                                 Name = "Field2",
                                 TypeName = "int"
                             },

                         }, "SampleProc", new List<string>())
                         ?? throw new Exception("Failed to generate");


        actual = StringManipulation.IndentBasedOnBraces(actual);

        var expected =
@"public partial class SampleProc_Parameters
{
    public string? Field1 { get; set; }
    public int? Field2 { get; set; }
}";

        if (actual != expected) NotEqualError(expected, actual);
    }

    public static string? GenerateParameterDefinition(List<ParameterDefinition> parameters, string procName, List<string> userDefinedTypeNames)
    {
        return parameters.Any()
            ? $@"public partial class {procName}_Parameters
							{{{string.Join("", parameters.Select(p =>
                                $@"
								public {p.CSharpType(userDefinedTypeNames)} {p.CSharpPropertyName()} {{ get; set; }}"
                            ))}		
							}}"
            : null;
    }


    private static void NotEqualError(string expected, string actual, [CallerMemberName] string method = "")
    {
        throw new ArgumentException($"Verify failed: {method}"
                                    + $"\nExpected: \n{expected}"
                                    + $"\nActual: \n{actual}"
                                    + $"\nEscaped Expected: \n{Regex.Escape(expected)}"
                                    + $"\nEscaped Actual: \n{Regex.Escape(actual)}"
        );
    }
    
}