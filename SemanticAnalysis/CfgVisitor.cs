using Common;
using Common.AST;

namespace ScopeAnalyze
{
    public class CfgVisitor : AnalyzeVisitor
    {
        public CfgVisitor(Scope scope) : base(scope)
        {
        }

        public override dynamic Visit(ProgramNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(NoOpNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(StatementListNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(AssignmentNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(CallNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(BinaryOpNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(UnaryOpNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(ExpressionNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(SizeNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(IdentifierNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(LiteralNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(IfNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(WhileNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(VarDeclarationNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(ProcedureDeclarationNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(FunctionDeclarationNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(ParameterNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(TypeNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(SimpleTypeNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(ArrayTypeNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(ReturnStatementNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(AssertStatementNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(ReadStatementNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(WriteStatementNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(DeclarationListNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(VariableNode node)
        {
            throw new System.NotImplementedException();
        }
    }
}