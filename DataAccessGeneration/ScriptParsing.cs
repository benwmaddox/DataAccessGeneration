using Microsoft.SqlServer.TransactSql.ScriptDom;

public class ScriptParsing
{
    public string FindTokensInQuery(string query)
    {
        // using scriptdom, find all parameters in the query
        var parser = new TSql120Parser(true);
        IList<ParseError> errors;
        var script = parser.Parse(new StringReader(query), out errors);
        var tokens = script.ScriptTokenStream;
        return tokens;
    }
}