using System;
using System.Linq;

namespace DataAccessGeneration.XUnitTest.Extensions;

public static class StringExtensions
{
    public static string TrimAllLines(this string value)
    {
        return string.Join(Environment.NewLine, value.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Select(x => x.Trim()));
    }
}