using System.Text;

namespace DataAccessGeneration;

public static class StringManipulation
{
    public static string StandardizeExtraNewlines(this string input)
    {
        var splits = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        for (var i = splits.Count - 1; i >= 2; i--)
        {
            // if 2 lines in a row are empty, remove the first one
            if (string.IsNullOrWhiteSpace(splits[i]) && string.IsNullOrWhiteSpace(splits[i - 1]))
            {
                splits.RemoveAt(i - 1);
            }
        }
        for (var i = splits.Count - 1; i >= 1; i--)
        {
            // if previous line was just an opening brace and this line is empty, remove this line
            if (splits[i - 1].Trim() == "{" && string.IsNullOrWhiteSpace(splits[i]))
            {
                splits.RemoveAt(i);
            }
        }
        for (var i = splits.Count - 2; i >= 0; i--)
        {
            // if the next line is just a closing brace and this line is empty, remove this line
            if (splits[i + 1].Trim() == "}" && string.IsNullOrWhiteSpace(splits[i]))
            {
                splits.RemoveAt(i);
            }
        }
        
        return String.Join(Environment.NewLine, splits);
    }
    public static string IndentBasedOnBraces(this string input, int startingIndent = 0)
    {
        var splits = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var indentLevel = startingIndent;
        var result = new StringBuilder();

        foreach (var line in splits)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                result.Append(Environment.NewLine);
            }
            else
            {
                // Special rule around lines starting with }. Need to back out their indent once before writing content
                if (trimmed.StartsWith("}"))
                {
                    indentLevel--;
                }
                result.Append(string.Join("", Enumerable.Repeat("    ", indentLevel)) + trimmed + Environment.NewLine);
            }
            if (trimmed.StartsWith("}"))
            {
                indentLevel++;
            }
            indentLevel -= line.Count(l => l == '}');
            indentLevel += line.Count(l => l == '{');
        }

        if (result.Length > 2)
        {
            //Removing trailing \r\n, \r, or \n
            result.Remove(result.Length - (Environment.NewLine.Length), Environment.NewLine.Length);
        }
        return result.ToString();
    }
    public static string JoinWithCommaAndNewLines(this IEnumerable<string> items)
    {
        return string.Join("," + Environment.NewLine, items);
    }		
}