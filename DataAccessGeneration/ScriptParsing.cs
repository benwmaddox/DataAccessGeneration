using Microsoft.SqlServer.TransactSql.ScriptDom;

public class ScriptParsing{
    public object FindParametersInQuery(string query){
        // using scriptdom, find all parameters in the query
        var parser = new TSql120Parser(true);
        IList<ParseError> errors;
        var script = parser.Parse(new StringReader(query), out errors);
        
        return script;
    }
}