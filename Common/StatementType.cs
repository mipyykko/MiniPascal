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
        AssignmentStatement,
        CallStatement,
        ReturnStatement,
        ReadStatement,
        WriteStatement,
        AssertStatement,
        IfStatement,
        WhileStatement,
        SimpleExpression,
        Term,
        Factor,
        Variable,
        Operator,
        Operand,
        Sign
    }
}