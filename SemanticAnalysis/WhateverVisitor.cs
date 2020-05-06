using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.AST;
using Common.Symbols;

namespace ScopeAnalyze
{
    public class SecondVisitor : Visitor
    {
        private readonly Stack<Scope> _scopes = new Stack<Scope>();
        private Scope CurrentScope => _scopes.Peek();

        private void EnterScope(Scope s)
        {
            _scopes.Push(s);
        }

        private void ExitScope()
        {
            _scopes.Pop();
        }

        private IVariable GetVariable(string id)
        {
            var s = CurrentScope;

            while (s != null)
            {
                var sym = s.SymbolTable.GetSymbol(id);
                if (sym is Variable) return sym;
                s = s.Parent;
            }

            return null;
        }

        private bool CheckVariable(string id)
        {
            var s = CurrentScope;

            while (s != null)
            {
                var sym = s.SymbolTable.GetSymbol(id);
                if (sym is Variable) return true;
                s = s.Parent;
            }

            return false;
        }

        private bool CheckFunctionOrProcedure(string id)
        {
            var s = CurrentScope;

            while (s != null)
            {
                var sym = s.SymbolTable.GetSymbol(id);
                if (sym is BuiltinFunctionVariable || sym is UserFunctionVariable) return true;
                s = s.Parent;
            }

            return false;
        }

        public override dynamic Visit(ProgramNode node)
        {
            EnterScope(node.MainBlock.Scope);

            node.DeclarationBlock.Accept(this);
            node.MainBlock.Accept(this);

            ExitScope();
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
            var id = ((LValueNode) node.LValue).Id.Accept(this);

            if (CheckVariable(id)) return null;

            throw new Exception($"variable {id} not declared");
            ;
        }

        public override dynamic Visit(CallNode node)
        {
            var id = node.Id.Accept(this);
            var fn = CurrentScope.GetSymbol(id);

            if (!CheckFunctionOrProcedure(id)) throw new Exception($"function or procedure {id} not declared");
            ;

            /*while (!(fn is UserFunction) && !(fn is BuiltinFunction))
            {
                var s = CurrentScope.Parent;
                if (s == null)
                {
                    throw new Exception($"can't call a non-function {id}");
                }

                fn = s.GetSymbol(id);
            }*/

            var arguments = node.Arguments;
            foreach (var arg in arguments) arg.Accept(this);

            node.Function = fn.Node;
            node.Type = fn.Node?.Type;

            if (node.Function == null) return PrimitiveType.Void; // must be a builtin then? 

            var fnParameters = node.Function.Parameters;

            if (arguments.Count != fnParameters.Count)
                throw new Exception(
                    $"wrong number of parameters given for function {id}: expected {fnParameters.Count}, got {arguments.Count}");

            for (var i = 0; i < arguments.Count; i++)
            {
                var arg = arguments[i];
                var pn = (ParameterNode) fnParameters[i];
                var pId = pn.Id.Accept(this);

                var pVariable = (Variable) node.Function.Scope.GetSymbol(pId);

                dynamic argIndexType = PrimitiveType.Void;
                var argType = arg.Type.PrimitiveType;
                var argSubType = PrimitiveType.Void;
                var argSize = -1;

                var pnType = pVariable.PrimitiveType; // pn.Type.PrimitiveType;
                var pnSubType = pVariable.SubType;
                var pnSize = -1;

                if (pn.Reference && !(arg is IdentifierNode))
                    throw new Exception(
                        $"wrong parameter type for {id} - {pId}: expected a variable as a parameter, got {arg}");

                /*if (arg is VariableNode v)
                {
                    var c = (Variable) GetVariable(v.Id.Accept(this));
                    argIndexType = v.IndexExpression.Accept(this);
                    if (argIndexType != null && argIndexType != PrimitiveType.Void)
                    {
                        argType = c.SubType;
                        argSubType = PrimitiveType.Void;
                    }
                    else
                    {
                        argType = c.PrimitiveType;
                        argSubType = c.SubType;
                        argSize = c.Size;
                    }
                }*/

                if (argType != pnType)
                    throw new Exception(
                        $"wrong parameter type for {id} - {pId}: expected {pnType}, got {argType}");

                if (argType != PrimitiveType.Array) continue;

                if (argSubType != pnSubType)
                    throw new Exception(
                        $"wrong parameter type for {id} - array {pId} expected to be of subtype {pnSubType}, got {argSubType}");


                argSize = ((ArrayTypeNode) arg.Type).Size.Accept(this);
                pnSize = ((ArrayTypeNode) pn.Type).Size.Accept(this);

                if (pnSize != null && argSize != pnSize)
                    throw new Exception(
                        $"type error: {id} expected array {pId} to be {arg.Type}, got {pn.Type}");
            }

            return node.Function is ProcedureDeclarationNode ? PrimitiveType.Void : node.Function.Type.PrimitiveType;
        }

        private static string[] RelationalOperators =
        {
            "=", "<>", "<", "<=", ">=", ">"
        };

        public override dynamic Visit(BinaryOpNode node)
        {
            var left = node.Left.Accept(this);
            var right = node.Right.Accept(this);
            var op = node.Token.Content;

            var leftType = node.Left is IdentifierNode
                ? ((Variable) CurrentScope.SymbolTable.GetSymbol(left)).PrimitiveType
                : left;
            var rightType = node.Right is IdentifierNode
                ? ((Variable) CurrentScope.SymbolTable.GetSymbol(right)).PrimitiveType
                : right;

            if (leftType != rightType) throw new Exception($"type error: can't perform {left} {op} {right}");

            var type = RelationalOperators.Contains(op) ? PrimitiveType.Boolean : left;

            node.Type.PrimitiveType = type; // TODO: not what's supposed to be

            return type;
        }

        public override dynamic Visit(UnaryOpNode node)
        {
            var op = node.Token.Content;
            var type = node.Expression.Accept(this);

            if (type != PrimitiveType.Boolean && op == "not" ||
                "+-".Contains(op) && type != PrimitiveType.Integer && type != PrimitiveType.Real)
                throw new Exception($"invalid op {op} on {type}");

            return type;
        }

        public override dynamic Visit(ExpressionNode node)
        {
            var type = node.Expression.Accept(this);

            return type;
        }

        public override dynamic Visit(SizeNode node)
        {
            return PrimitiveType.Integer;
        }

        public override dynamic Visit(IdentifierNode node)
        {
            // var idx = node.IndexExpression.Accept(this);

            return node.Token.Content;
        }

        public override dynamic Visit(LiteralNode node)
        {
            return node.Type.PrimitiveType;
        }

        public override dynamic Visit(IfNode node)
        {
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
            EnterScope(node.Statement.Scope);
            node.Statement.Accept(this);
            ExitScope();

            return null;
        }

        public override dynamic Visit(FunctionDeclarationNode node)
        {
            EnterScope(node.Statement.Scope);
            node.Statement.Accept(this);
            ExitScope();

            var type = node.Type.Accept(this);

            return type;
        }

        public override dynamic Visit(ParameterNode node)
        {
            return node.Type.Accept(this);
        }

        public override dynamic Visit(TypeNode node)
        {
            return node.PrimitiveType;
        }

        public override dynamic Visit(SimpleTypeNode node)
        {
            return node.PrimitiveType;
        }

        public override dynamic Visit(ArrayTypeNode node)
        {
            return node.PrimitiveType; // todo?
        }

        private static readonly ScopeType[] ReturnableTypes =
        {
            ScopeType.Function,
            ScopeType.Main,
            ScopeType.Procedure
        };

        public override dynamic Visit(ReturnStatementNode node)
        {
            var s = CurrentScope;

            while (s != null && !ReturnableTypes.Contains(s.ScopeType)) s = s.Parent;

            if (s == null) throw new Exception($"invalid return"); // will this ever happen?

            var value = node.Expression.Accept(this);
            var type = PrimitiveType.Void;

            switch (node.Expression)
            {
                case IdentifierNode _:
                {
                    var idValue = (IVariable) s.GetSymbol(value);
                    type = idValue.PrimitiveType;
                    break;
                }
                default:
                {
                    if (value != null) type = value; // .Type.PrimitiveType;

                    break;
                }
            }

            var id = ((IdNode) s.Node).Id.Accept(this);
            var fNode = s.Node;

            switch (s.ScopeType)
            {
                case ScopeType.Function:
                {
                    var fType = fNode.Type.PrimitiveType;

                    if (type == PrimitiveType.Void)
                        throw new Exception($"function {id} must return type {fType}, tried to return null");

                    if (type != fType)
                        throw new Exception($"function {id} must return type {fType}, tried to return {type}");

                    break;
                }
                case ScopeType.Procedure when type != PrimitiveType.Void:
                    throw new Exception($"can't return value from procedure {id}");
            }

            return type; // TODO
        }

        public override dynamic Visit(AssertStatementNode node)
        {
            var type = node.Expression.Accept(this);

            if (type != PrimitiveType.Boolean) throw new Exception($"non-boolean assertion");

            return null;
        }

        public override dynamic Visit(ReadStatementNode node)
        {
            return null;
        }

        public override dynamic Visit(WriteStatementNode node)
        {
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
            throw new NotImplementedException();
        }

        public override dynamic Visit(ArrayDereferenceNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(ValueOfNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(ErrorNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(IntegerValueNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(RealValueNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(StringValueNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(BooleanValueNode node)
        {
            throw new NotImplementedException();
        }
    }
}