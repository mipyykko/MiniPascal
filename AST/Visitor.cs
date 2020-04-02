namespace AST
{
    public abstract class Visitor
    {
        public abstract dynamic Visit(NoOpNode node);
        public abstract dynamic Visit(StatementListNode node);
        public abstract dynamic Visit(AssignmentNode node);
        public abstract dynamic Visit(CallNode node);
        public abstract dynamic Visit(BinaryOpNode node);
        public abstract dynamic Visit(UnaryOpNode node);
        public abstract dynamic Visit(ExpressionNode node);
        public abstract dynamic Visit(SizeNode node);
        public abstract dynamic Visit(IdentifierNode node);
        public abstract dynamic Visit(LiteralNode node);
        public abstract dynamic Visit(IfNode node);
        public abstract dynamic Visit(WhileNode node);
        public abstract dynamic Visit(VarDeclarationNode node);
        public abstract dynamic Visit(ProcedureDeclarationNode node);
        public abstract dynamic Visit(FunctionDeclarationNode node);
        public abstract dynamic Visit(ParameterNode node);

    }
}