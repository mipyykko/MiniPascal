using System;
using System.Data;
using System.Linq;
using Common;
using Common.AST;
using Common.Errors;
using Common.Symbols;

namespace ScopeAnalyze
{
    public class ExpressionVisitor : AnalyzeVisitor
    {
        public ExpressionVisitor(Scope scope) : base(scope)
        {
        }

        public override dynamic Visit(ProgramNode node)
        {
            node.DeclarationBlock.Accept(this);
            node.MainBlock.Accept(this);

            return null;
        }

        public override dynamic Visit(NoOpNode node)
        {
            return null;
        }

        public override dynamic Visit(StatementListNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);

            return null;
        }

        public override dynamic Visit(AssignmentNode node)
        {
            var lValue = node.LValue.Accept(this);
            // var index = node.IndexExpression.Accept(this);
            var expression = node.Expression.Accept(this);

            return null;
        }

        public override dynamic Visit(CallNode node)
        {
            var id = node.Id.Accept(this);
            var fn = (FunctionVariable) GetFunctionOrProcedure(id);
            var fnNode = (FunctionDeclarationNode) fn.Node;

            node.Function = fnNode;
            node.Variable = fn;
            
            for (var i = 0; i < node.Arguments.Count; i++)
            {
                var parameterNode = (ParameterNode) fnNode.Parameters[i];

                /*if (!parameterNode.Reference && node.Arguments[i] is LValueNode)
                {
                    node.Arguments[i] = new ValueOfNode
                    {
                        LValue = (LValueNode) node.Arguments[i]
                    };
                }*/
                node.Arguments[i].Accept(this);

                var argumentNode = node.Arguments[i];
                var type = argumentNode.Type?.PrimitiveType;
                var parameterId = parameterNode.Id.Accept(this);

                if (!(argumentNode is ArrayDereferenceNode arrNode)) continue;

                var argumentId = ((VariableNode) arrNode.LValue).Id.Accept(this);
                var parameterVariable = (Variable) GetVariable(parameterId, fn.Node.Scope);
                var argumentVariable = (Variable) GetVariable(argumentId);
                    
                parameterVariable.Size = argumentVariable.Size;
                fn.Node.Scope.SymbolTable.UpdateSymbol(parameterVariable);
                /*                if (!(argumentNode is VariableNode vn) || type != PrimitiveType.Array) continue;

                var argumentId = vn.Id.Accept(this);
                var parameterVariable = (Variable) GetVariable(parameterId, fn.Node.Scope);
                var argumentVariable = (Variable) GetVariable(argumentId);

                parameterVariable.Size = argumentVariable.Size;
                fn.Node.Scope.SymbolTable.UpdateSymbol(parameterVariable);*/
            }

            return null;
        }

        public override dynamic Visit(BinaryOpNode node)
        {
            var left = node.Left.Accept(this);
            var right = node.Right.Accept(this);
            var op = node.Token.Content;

            if (left == null || right == null) return null;

            // TODO: this has no division by zero check and what have you
            
            return op switch
            {
                "+" when !(left is bool) => (left + right),
                "-" when left is int || left is float => (left - right),
                "/" when (left is int || left is float) && right > 0 => (left / right),
                "/" when (left is int || left is float) && right == 0 => Context.ErrorService.Add(
                    ErrorType.Unknown,
                    node.Token,
                    $"division by zero"
                ),
                "*" when left is int || left is float => (left * right),
                "%" when left is int => (left % right),
                "and" when left is bool => (left && right),
                "or" when left is bool => (left || right),
                "<" => (left < right),
                "<=" => (left <= right),
                ">" => (left > right),
                ">=" => (left >= right),
                "<>" => (left != right),
                "=" => (left == right),
                _ => null
                // error already reported before
                //throw new Exception($"invalid operation {op} on {left} {right}")
            };
        }

        public override dynamic Visit(UnaryOpNode node)
        {
            var op = node.Token.Content;
            var expression = node.Expression.Accept(this);

            if (expression == null) return null;

            return op switch
            {
                "+" when expression is int || expression is float => +expression,
                "-" when expression is int || expression is float => -expression,
                "not" when expression is bool => !expression,
                _ => null
                // error already reported
                //throw new Exception($"invalid operation {op} on {expression}")
            };
        }

        public override dynamic Visit(ExpressionNode node)
        {
            var expression = node.Expression.Accept(this);

            return null;
        }

        public override dynamic Visit(SizeNode node)
        {
            node.LValue.Accept(this);

            var id = node.LValue.Id.Accept(this);
            var variable = (Variable) GetVariable(id);

            return variable.Size;
        }

        public override dynamic Visit(IdentifierNode node)
        {
            return node.Token.Content;
        }

        public override dynamic Visit(LiteralNode node)
        {
            // deprecated
            return node.Token.Type switch
            {
                TokenType.IntegerValue => (dynamic) int.Parse(node.Token.Content),
                TokenType.RealValue => float.Parse(node.Token.Content),
                TokenType.BooleanValue => node.Token.Content.ToLower() == "true",
                TokenType.StringValue => node.Token.Content,
                _ => throw new NotImplementedException()
            };
        }

        public override dynamic Visit(IfNode node)
        {
            var expression = node.Expression.Accept(this);
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
            var expression = node.Expression.Accept(this);

            EnterScope(node.Statement.Scope);
            node.Statement.Accept(this);
            ExitScope();

            return null;
        }

        public override dynamic Visit(VarDeclarationNode node)
        {
            foreach (var idNode in node.Ids)
            {
                var id = idNode.Accept(this);
                var typeInfo = idNode.Type.Accept(this);

                if (idNode.Type.PrimitiveType == PrimitiveType.Array && typeInfo != null)
                {
                    var variable = (Variable) GetVariable(id);
                    variable.Size = typeInfo;
                    CurrentScope.SymbolTable.UpdateSymbol(variable);
                }
            }

            return null;
        }

        public override dynamic Visit(FunctionDeclarationNode node)
        {
            EnterScope(node.Statement.Scope);
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
            var size = node.Size.Accept(this);

            return size;
        }

        public override dynamic Visit(ReturnStatementNode node)
        {
            var expression = node.Expression.Accept(this);

            return null;
        }

        public override dynamic Visit(AssertStatementNode node)
        {
            var expression = node.Expression.Accept(this);

            return null;
        }

        public override dynamic Visit(ReadStatementNode node)
        {
            foreach (var n in node.Variables) n.Accept(this);

            return null;
        }

        public override dynamic Visit(WriteStatementNode node)
        {
            foreach (var argument in node.Arguments)
            {
                argument.Accept(this);
            }

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
            var id = node.Id.Accept(this);
            /*var index = node.IndexExpression.Accept(this);

            var variable = (Variable) GetVariable(id);

            if (variable.PrimitiveType == PrimitiveType.Array)
                if (index != null && variable.Size > 0 && (index < 0 || index >= variable.Size))
                    throw new Exception($"out of bounds error: {id} indexed with {index}");
            */
            return null;
        }

        public override dynamic Visit(ArrayDereferenceNode node)
        {
            var id = ((VariableNode) node.LValue).Id.Accept(this);
            var index = node.Expression.Accept(this);
            var variable = (Variable) GetVariable(id);
            
            //if (index != null && variable.Size > 0 && (index < 0 || index >= variable.Size))
            //    throw new Exception($"out of bounds error: {id} indexed with {index}");

            return null;
        }

        public override dynamic Visit(ValueOfNode node)
        {
            return null;
        }

        public override dynamic Visit(ErrorNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(IntegerValueNode node)
        {
            return node.Value;
        }

        public override dynamic Visit(RealValueNode node)
        {
            return node.Value;
        }

        public override dynamic Visit(StringValueNode node)
        {
            return node.Value;
        }

        public override dynamic Visit(BooleanValueNode node)
        {
            return node.Value;
        }
    }
}