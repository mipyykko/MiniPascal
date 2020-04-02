namespace Common
{
    public enum StatementType
    {
        Unknown,
        NoOpStatement,
        Program,
        VarDeclaration,
        ProcedureDeclaration,
        FunctionDeclaration,
        ParameterDeclaration,
        Type, // simple, array?
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
        Sign
    }
}