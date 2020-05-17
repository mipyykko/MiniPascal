using System.Collections.Generic;
using System.Linq;
using Common;
using Common.AST;
using Common.Symbols;

namespace ScopeAnalyze
{
    /**
     * Will check if a call points to a built-in function and replace the node where necessary
     */
    public class BuiltinVisitor : AnalyzeVisitor
    {
        public BuiltinVisitor(Scope scope) : base(scope)
        {
        }

        public override dynamic Visit(ProgramNode node)
        {
            node.DeclarationBlock.Accept(this);
            node.MainBlock.Accept(this);

            return node;
        }

        public override dynamic Visit(NoOpNode node)
        {
            return null;
        }

        public Node ReplaceCallNode(Node node)
        {
            if (!(node is CallNode)) return node;

            var cn = (CallNode) node;
            var id = cn.Id.Accept(this);
            var variable = GetFunctionOrProcedure(id);

            if (variable is BuiltinFunctionVariable)
                return ((string) id).ToLower() switch
                {
                    "writeln" => new WriteStatementNode
                    {
                        Token = cn.Token,
                        Arguments = cn.Arguments,
                        Type = cn.Type,
                        Scope = cn.Scope
                    },
                    "read" => new ReadStatementNode
                    {
                        Token = cn.Token,
                        Variables = cn.Arguments,
                        Type = cn.Type,
                        Scope = cn.Scope
                    },
                    _ => cn
                };

            return cn;
        }

        public override dynamic Visit(StatementListNode node)
        {
            node.Left = ReplaceCallNode(node.Left);

            node.Left.Accept(this);
            node.Right.Accept(this);

            return null;
        }

        public override dynamic Visit(AssignmentNode node)
        {
            node.LValue.Accept(this);
            //node.IndexExpression.Accept(this);
            node.Expression.Accept(this);

            return null;
        }

        public override dynamic Visit(CallNode node)
        {
            return null;
        }

        public override dynamic Visit(BinaryOpNode node)
        {
            node.Left = ReplaceCallNode(node.Left);
            node.Right = ReplaceCallNode(node.Right);

            node.Left.Accept(this);
            node.Right.Accept(this);

            return null;
        }

        public override dynamic Visit(UnaryOpNode node)
        {
            node.Expression = ReplaceCallNode(node.Expression);
            node.Expression.Accept(this);

            return null;
        }

        public override dynamic Visit(ExpressionNode node)
        {
            node.Expression = ReplaceCallNode(node.Expression);
            node.Expression.Accept(this);

            return null;
        }

        public override dynamic Visit(SizeNode node)
        {
            node.LValue.Accept(this);
            return null;
        }

        public override dynamic Visit(IdentifierNode node)
        {
            return node.Token.Content;
        }

        public override dynamic Visit(LiteralNode node)
        {
            return null;
        }

        public override dynamic Visit(IfNode node)
        {
            node.Expression = ReplaceCallNode(node.Expression);
            node.TrueBranch = ReplaceCallNode(node.TrueBranch);
            node.FalseBranch = ReplaceCallNode(node.FalseBranch);

            node.Expression.Accept(this);
            EnterScope(node.TrueBranch.Scope);
            node.TrueBranch.Accept(this);
            ExitScope();
            EnterScope(node.FalseBranch.Scope);
            node.FalseBranch.Accept(this);
            ExitScope();

            return null;
        }

        public override dynamic Visit(WhileNode node)
        {
            node.Expression = ReplaceCallNode(node.Expression);
            node.Statement = ReplaceCallNode(node.Statement);

            node.Expression.Accept(this);
            EnterScope(node.Statement.Scope);
            node.Statement.Accept(this);
            ExitScope();

            return null;
        }

        public override dynamic Visit(VarDeclarationNode node)
        {
            return null;
        }

        public override dynamic Visit(ProcedureDeclarationNode node)
        {
            EnterScope(node.Scope);
            node.Statement.Accept(this);
            ExitScope();

            return null;
        }

        public override dynamic Visit(FunctionDeclarationNode node)
        {
            EnterScope(node.Scope);
            node.Statement.Accept(this);
            ExitScope();

            return null;
        }

        public override dynamic Visit(ParameterNode node)
        {
            return null;
        }

        public override dynamic Visit(TypeNode node)
        {
            return null;
        }

        public override dynamic Visit(SimpleTypeNode node)
        {
            return null;
        }

        public override dynamic Visit(ArrayTypeNode node)
        {
            return null;
        }

        public override dynamic Visit(ReturnStatementNode node)
        {
            node.Expression = ReplaceCallNode(node.Expression);
            node.Expression.Accept(this);

            return null;
        }

        public override dynamic Visit(AssertStatementNode node)
        {
            node.Expression = ReplaceCallNode(node.Expression);
            node.Expression.Accept(this);

            return null;
        }

        public override dynamic Visit(ReadStatementNode node)
        {
            return null;
        }

        public override dynamic Visit(WriteStatementNode node)
        {
            var newArguments = node.Arguments.Select(ReplaceCallNode).ToList();

            node.Arguments = newArguments;

            return null;
        }

        public override dynamic Visit(DeclarationListNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);

            return null;
        }

        public override dynamic Visit(VariableNode node)
        {
            return null;
        }

        public override dynamic Visit(ArrayDereferenceNode node)
        {
            node.Expression = ReplaceCallNode(node.Expression);
            node.Expression.Accept(this);

            return null;
        }

        public override dynamic Visit(ValueOfNode node)
        {
            node.LValue.Accept(this);
            
            return null;
        }

        public override dynamic Visit(ErrorNode node)
        {
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(IntegerValueNode node)
        {
            return null;
        }

        public override dynamic Visit(RealValueNode node)
        {
            return null;
        }

        public override dynamic Visit(StringValueNode node)
        {
            return null;
        }

        public override dynamic Visit(BooleanValueNode node)
        {
            return null;
        }
    }
}