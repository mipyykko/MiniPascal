namespace Common
{
    public enum StatementType
    {
        None,
        ProgramStatement,
        Declaration,
        Ids,
        Parameters,
        OptParameters,
        OptParametersCont,
        Type,
        ArrayType,
        ArrayTypeCont,
        SimpleType,
        Block,
        StatementListBlockEnd,
        StatementOrBlockEnd,
        Statement,
        SimpleStatement,
        StructuredStatement,
        AssignmentStatementOrCall,
        Arguments,
        Exprs,
        ReturnStatement,
        OptReturnExpr,
        ReadStatement,
        Variables,
        Variable,
        WriteStatement,
        AssertStatement,
        IfStatement,
        ElseBranch,
        WhileStatement,
        Expr,
        ExprCont,
        SimpleExpr,
        SimpleExprCont,
        Term,
        TermCont,
        IntegerExpr,
        BooleanExpr,
        Factor,
        FactorSize,
        CallOrVariable,
        CallOrVariableCont,
        VariableCont,
        RelationalOperator,
        Sign,
        AddingOperator,
        MultiplyingOperator,
        Id,
        IdCont,
        Literal,
        Digits,
        DigitsCont,
        NumberLiteral,
        RealLiteralOrEnd,
        OptExp,
        ExpCont,
        StringLiteral,
        Digit,
        TypeId,
        BooleanLiteral,
        VariableIds,

        AssignOrCallStatement
        /* // original
            Unknown,
            NoOpStatement,
            Program,
            VarDeclaration,
            ProcedureDeclaration,
            FunctionDeclaration,
            ParameterDeclaration,
            Type,
            SimpleType,
            ArrayType,
            BlockStatement,
            Statement,
            StatementList,
            AssignmentStatement,
            CallStatement,
            ReturnStatement,
            ReadStatement,
            WriteStatement,
            AssertStatement,
            IfStatement,
            WhileStatement,
            Expression,
            Arguments,
            SimpleExpression,
            Term,
            Factor,
            Variable,
            VariableList,
            Operator,
            Operand,
            Sign*/,
        Identifier,
        SignTerm,
        ArrayAssignmentStatement,
        DeclarationBlock,
        FunctionProcedureDeclaration,
        ProcedureDeclaration,
        FunctionDeclaration,
        DeclarationBlockCont,
        Parameter,
        ProgramCont,
        Error
    }
}