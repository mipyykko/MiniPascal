namespace Common.AST
{
    public abstract class Visitor
    {
        public abstract dynamic Visit(ProgramNode node);
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
        public abstract dynamic Visit(FunctionDeclarationNode node);
        public abstract dynamic Visit(ParameterNode node);
        public abstract dynamic Visit(TypeNode node);
        public abstract dynamic Visit(SimpleTypeNode node);
        public abstract dynamic Visit(ArrayTypeNode node);
        public abstract dynamic Visit(ReturnStatementNode node);
        public abstract dynamic Visit(AssertStatementNode node);
        public abstract dynamic Visit(ReadStatementNode node);
        public abstract dynamic Visit(WriteStatementNode node);
        public abstract dynamic Visit(DeclarationListNode node);
        public abstract dynamic Visit(VariableNode node);
        public abstract dynamic Visit(ArrayDereferenceNode node);
        public abstract dynamic Visit(ValueOfNode node);
        public abstract dynamic Visit(ErrorNode node);
        public abstract dynamic Visit(IntegerValueNode node);
        public abstract dynamic Visit(RealValueNode node);
        public abstract dynamic Visit(StringValueNode node);
        public abstract dynamic Visit(BooleanValueNode node);
    }
}