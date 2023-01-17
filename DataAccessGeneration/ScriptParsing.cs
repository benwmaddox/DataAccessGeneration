using Microsoft.SqlServer.TransactSql.ScriptDom;

public class ResultSetDef
{
    public List<TableDef> Tables = new List<TableDef>();
    public List<ReturnColumnDef> ReturnColumns = new List<ReturnColumnDef>();
    public List<VariableDef> VariableDefinitions = new List<VariableDef>();
}
public class Visitor : TSqlFragmentVisitor
{
    public List<string> Results = new List<string>();
    
    // public List<TableDef> Tables = new List<TableDef>();
    // public List<ReturnColumnDef> ReturnColumns = new List<ReturnColumnDef>();
    // public List<VariableDef> VariableDefinitions = new List<VariableDef>();
    public List<ResultSetDef> ResultSets = new List<ResultSetDef>();
    
    public override void Visit(SelectStatement node)
    {
        // TODO: use this to find the uses of variables and what they reference
        var spec = node.QueryExpression as QuerySpecification;
        Results.Add("Where clause: "+ spec?.WhereClause?.SearchCondition.ToString());
        // spec.FromClause.PredictTableReference.Select(x => x.DataSource.)
        
        
        // Results.Add(node.ToString());
        QuerySpecification? querySpecification = node.QueryExpression as QuerySpecification;

        FromClause? fromClause = querySpecification?.FromClause;
        var resultSet = new ResultSetDef()
        {
            Tables = FindTableDefs(fromClause) ?? new List<TableDef>(),
            ReturnColumns = FindReturnColumns(querySpecification) ?? new List<ReturnColumnDef>(),
            VariableDefinitions = FindWhereClauses(querySpecification) ?? new List<VariableDef>()
        };
        // List<TableDef>? tableItems = FindTableDefs(fromClause);
        // if (tableItems != null)
        // {
        //     Tables.AddRange(tableItems);
        // }
        // // Collect the return columns
        // List<ReturnColumnDef>? returnColumns = FindReturnColumns(querySpecification);
        // if (returnColumns != null)
        // {
        //     ReturnColumns.AddRange(returnColumns);
        // }
        // // Collect the where clause
        // List<VariableDef>? whereClauses = FindWhereClauses(querySpecification);
        // if (whereClauses != null)
        // {
        //     VariableDefinitions.AddRange(whereClauses);
        // }
        ResultSets.Add(resultSet);
        
        
        // There could be more than one TableReference!
        // TableReference is not sure to be a NamedTableReference, could be as example a QueryDerivedTable
        NamedTableReference? namedTableReference = fromClause?.TableReferences[0] as NamedTableReference;
        QualifiedJoin? qualifiedJoin = fromClause?.TableReferences[0] as QualifiedJoin;
        TableReferenceWithAlias? tableReferenceWithAlias = fromClause?.TableReferences[0] as TableReferenceWithAlias;
        string? baseIdentifier = namedTableReference?.SchemaObject.BaseIdentifier?.Value;
        string? schemaIdentifier = namedTableReference?.SchemaObject.SchemaIdentifier?.Value;
        string? databaseIdentifier = namedTableReference?.SchemaObject.DatabaseIdentifier?.Value;
        string? serverIdentifier = namedTableReference?.SchemaObject.ServerIdentifier?.Value;
        string? alias = tableReferenceWithAlias?.Alias?.Value;
        Console.WriteLine("From:");
        Console.WriteLine($"  {"Server:",-10} {serverIdentifier}");
        Console.WriteLine($"  {"Database:",-10} {databaseIdentifier}");
        Console.WriteLine($"  {"Schema:",-10} {schemaIdentifier}");
        Console.WriteLine($"  {"Table:",-10} {baseIdentifier}"); Console.WriteLine($"  {"Alias:",-10} {alias}");
    }

    private List<VariableDef>? FindWhereClauses(QuerySpecification? querySpecification)
    {
        return querySpecification?.WhereClause.SearchCondition switch
        {
            BooleanBinaryExpression booleanBinaryExpression => throw new NotImplementedException(),
            BooleanComparisonExpression booleanComparisonExpression => new List<VariableDef>()
            {
                new VariableDef("", null, null, null)
            },
            BooleanExpressionSnippet booleanExpressionSnippet => throw new NotImplementedException(),
            BooleanIsNullExpression booleanIsNullExpression => throw new NotImplementedException(),
            BooleanNotExpression booleanNotExpression => throw new NotImplementedException(),
            BooleanParenthesisExpression booleanParenthesisExpression => throw new NotImplementedException(),
            BooleanTernaryExpression booleanTernaryExpression => throw new NotImplementedException(),
            DistinctPredicate distinctPredicate => throw new NotImplementedException(),
            EventDeclarationCompareFunctionParameter eventDeclarationCompareFunctionParameter => throw new NotImplementedException(),
            ExistsPredicate existsPredicate => throw new NotImplementedException(),
            FullTextPredicate fullTextPredicate => throw new NotImplementedException(),
            GraphMatchCompositeExpression graphMatchCompositeExpression => throw new NotImplementedException(),
            GraphMatchExpression graphMatchExpression => throw new NotImplementedException(),
            GraphMatchLastNodePredicate graphMatchLastNodePredicate => throw new NotImplementedException(),
            GraphMatchNodeExpression graphMatchNodeExpression => throw new NotImplementedException(),
            GraphMatchPredicate graphMatchPredicate => throw new NotImplementedException(),
            GraphMatchRecursivePredicate graphMatchRecursivePredicate => throw new NotImplementedException(),
            GraphRecursiveMatchQuantifier graphRecursiveMatchQuantifier => throw new NotImplementedException(),
            InPredicate inPredicate => throw new NotImplementedException(),
            LikePredicate likePredicate => throw new NotImplementedException(),
            SubqueryComparisonPredicate subqueryComparisonPredicate => throw new NotImplementedException(),
            TSEqualCall tsEqualCall => throw new NotImplementedException(),
            UpdateCall updateCall => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private List<ReturnColumnDef>? FindReturnColumns(QuerySpecification? querySpecification)
    {
        return querySpecification?.SelectElements.Select(x => x switch
        {
            SelectScalarExpression selectScalarExpression => new ReturnColumnDef("", selectScalarExpression?.ColumnName?.Value ?? "???", ""),
            SelectSetVariable selectSetVariable => new ReturnColumnDef("", selectSetVariable.Variable.Name, ""),
            SelectStarExpression selectStarExpression => throw new NotImplementedException("selectStarExpression"),
            _ => throw new ArgumentOutOfRangeException(nameof(x))
        }).ToList();
    }

    private List<TableDef>? FindTableDefs(FromClause? fromClause)
    {
        return fromClause?.TableReferences.SelectMany(fc => fc switch
        {
            JoinParenthesisTableReference joinParenthesisTableReference => throw new NotImplementedException(),
            BuiltInFunctionTableReference builtInFunctionTableReference => throw new NotImplementedException(),
            BulkOpenRowset bulkOpenRowset => throw new NotImplementedException(),
            ChangeTableChangesTableReference changeTableChangesTableReference => throw new NotImplementedException(),
            ChangeTableVersionTableReference changeTableVersionTableReference => throw new NotImplementedException(),
            DataModificationTableReference dataModificationTableReference => throw new NotImplementedException(),
            FullTextTableReference fullTextTableReference => throw new NotImplementedException(),
            GlobalFunctionTableReference globalFunctionTableReference => throw new NotImplementedException(),
            InlineDerivedTable inlineDerivedTable => throw new NotImplementedException(),
            InternalOpenRowset internalOpenRowset => throw new NotImplementedException(),
            AdHocTableReference adHocTableReference => throw new NotImplementedException(),
            QualifiedJoin qualifiedJoin1 => new List<TableDef>
            {
                ToTableDef(qualifiedJoin1.FirstTableReference),
                ToTableDef(qualifiedJoin1.SecondTableReference)
            },
            UnqualifiedJoin unqualifiedJoin => throw new NotImplementedException(),
            JoinTableReference joinTableReference => throw new NotImplementedException(),
            NamedTableReference namedTableReference1 => throw new NotImplementedException(),
            OdbcQualifiedJoinTableReference odbcQualifiedJoinTableReference => throw new NotImplementedException(),
            OpenJsonTableReference openJsonTableReference => throw new NotImplementedException(),
            OpenQueryTableReference openQueryTableReference => throw new NotImplementedException(),
            OpenRowsetCosmos openRowsetCosmos => throw new NotImplementedException(),
            OpenRowsetTableReference openRowsetTableReference => throw new NotImplementedException(),
            OpenXmlTableReference openXmlTableReference => throw new NotImplementedException(),
            PivotedTableReference pivotedTableReference => throw new NotImplementedException(),
            PredictTableReference predictTableReference => throw new NotImplementedException(),
            QueryDerivedTable queryDerivedTable => throw new NotImplementedException(),
            SchemaObjectFunctionTableReference schemaObjectFunctionTableReference => throw new NotImplementedException(),
            SemanticTableReference semanticTableReference => throw new NotImplementedException(),
            VariableMethodCallTableReference variableMethodCallTableReference => throw new NotImplementedException(),
            TableReferenceWithAliasAndColumns tableReferenceWithAliasAndColumns => throw new NotImplementedException(),
            UnpivotedTableReference unpivotedTableReference => throw new NotImplementedException(),
            VariableTableReference variableTableReference => throw new NotImplementedException(),
            TableReferenceWithAlias tableReferenceWithAlias1 => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(fc))
        }).ToList();
    }

    private TableDef ToTableDef(TableReference table)
    {
        return table switch
        {
            NamedTableReference namedTableReference => new TableDef
            {
                Alias = namedTableReference.Alias?.Value,
                Schema = namedTableReference.SchemaObject.SchemaIdentifier.Value,
                Table = namedTableReference.SchemaObject.BaseIdentifier.Value
            },
            TableReferenceWithAlias tableReferenceWithAlias => new TableDef
            {
                Alias = tableReferenceWithAlias.Alias?.Value
            },
            _ => throw new ArgumentOutOfRangeException(nameof(table))
        };
        
    }

    public override void Visit(TableReference node)
    {
        if (node is NamedTableReference)
        {
            NamedTableReference namedTable = (NamedTableReference)node;
            Results.Add("Table reference: " + namedTable.SchemaObject.BaseIdentifier.Value);
            Console.WriteLine("table reference: " + namedTable.SchemaObject.BaseIdentifier.Value);
        }
        base.Visit(node);
    }

    public override void Visit(TableReferenceWithAlias node)
    {
        var tableReference = node as NamedTableReference;
        if (tableReference != null)
        {
            Results.Add("Table reference with alias: " +tableReference.SchemaObject.SchemaIdentifier.Value + " " + tableReference.SchemaObject.BaseIdentifier.Value +" " + tableReference.Alias.Value );
            Console.WriteLine("table reference with alias: " + tableReference.SchemaObject.BaseIdentifier.Value);
        }
        Results.Add("Table reference with alias: " + node.Alias.Value);
    }
    public override  void Visit(VariableReference variableReference)
    {
        Results.Add("Variable reference: " + variableReference.Name);
        Console.WriteLine("Variable: " + variableReference.Name);
    }
    // public override void Visit(WhereClause whereClause)
    // {
    //     Results.Add("Where clause: " + string.Join(",", whereClause.SearchCondition.ScriptTokenStream.Select(y => y.Text)));
    //     Console.WriteLine("Where clause: " + whereClause.SearchCondition);
    // }
}

public record TableDef
{
    public string Schema { get; init; } = "";
    public string Table { get; init; } = "";
    public string? Alias { get; init; }
}


public record ReturnColumnDef(string TableAlias, string Column, string Alias);

public record VariableDef(string Name, string? Type, string? MatchingTableAlias, string? MatchingColumn);

public class ScriptParsing
{
    public IList<string> FindErrorsInQuery(string query)
    {
        // using scriptdom, find all parameters in the query
        var parser = new TSql120Parser(true);
        IList<ParseError> errors;
        var script = parser.Parse(new StringReader(query), out errors);
        var tokens = script.ScriptTokenStream;
        
        return errors.Select(x => x.Message).ToList();
    }
    public List<ResultSetDef> FindParametersInQuery(string query)
    {
        // using scriptdom, find all parameters in the query
        var parser = new TSql120Parser(true);
        IList<ParseError> errors;
        
        var script = parser.Parse(new StringReader(query), out errors);
        var visitor = new Visitor();
        script.Accept(visitor);
        var tokens = script.ScriptTokenStream;
        
        // return tokens.Where(t => t.TokenType == TSqlTokenType.Identifier).Select(t => t.Text).ToList();
        // foreach (var token in tokens)
        // {
        //     if (token.TokenType == TSqlTokenType.Identifier)
        //     {
        //         Console.WriteLine(token.Text);
        //     }
        // }
        // return errors.Select(x => x.Message).ToList();
        // return tokens.Select(x => x.TokenType.ToString().PadRight(30) + x.Text).ToList();
        return visitor.ResultSets;
    }
    
}