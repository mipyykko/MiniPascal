using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.AST;

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

        private void ExitScope() => _scopes.Pop();
        
        private bool CheckVariable(string id)
        {
            var s = CurrentScope;

            while (s != null)
            {
                try
                {
                    s.SymbolTable.GetSymbol(id);
                    return true;
                }
                catch
                {
                    s = s.Parent;
                }
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
            var id = node.Id.Accept(this);

            if (CheckVariable(id)) return null;
            
            throw new Exception($"variable {id} not declared");;
        }

        public override dynamic Visit(CallNode node)
        {
            var id = node.Id.Accept(this);

            if (!CheckVariable(id))
            {
                throw new Exception($"function or procedure {id} not declared");
            };
            var arguments = node.Arguments;
            foreach (var arg in arguments)
            {
                arg.Accept(this);
            }
            
            if (node.Function == null)
            {
                return null; // must be a builtin then? 
            }
            
            var fnParameters = node.Function.Parameters;
            
            if (arguments.Count != fnParameters.Count)
            {
                throw new Exception($"wrong number of parameters given for function {id}: expected {fnParameters.Count}, got {arguments.Count}");
            }

            for (var i = 0; i < arguments.Count; i++)
            {
                var arg = arguments[i];
                var pn = (ParameterNode) fnParameters[i];
                if (pn.Reference && !(arg is IdentifierNode))
                {
                    throw new Exception($"expected a variable as a parameter, got {arg}");
                }
            }

            return node.Function.Type;
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

            if (left != right)
            {
                throw new Exception($"type error: can't perform {left} {op} {right}");
            }

            var type = RelationalOperators.Contains(op) ? "boolean" : left;
            
            node.Type = type; // TODO: not what's supposed to be

            return type;
        }

        public override dynamic Visit(UnaryOpNode node)
        {
            var op = node.Token.Content;
            var type = node.Expression.Accept(this);

            if ((type != "boolean" && op == "not") ||
                ("+-".Contains(op) && type != "integer" && type != "real"))
            {
                throw new Exception($"invalid op {op} on {type}");
            }

            return type;
        }

        public override dynamic Visit(ExpressionNode node)
        {
            var type = node.Expression.Accept(this);

            return type;
        }

        public override dynamic Visit(SizeNode node)
        {
            return "integer";
        }

        public override dynamic Visit(IdentifierNode node)
        {
            // var idx = node.IndexExpression.Accept(this);

            return node.Token.Content;
        }

        public override dynamic Visit(LiteralNode node)
        {
            return node.Token.Type switch
            {
                TokenType.IntegerValue => "integer",
                TokenType.RealValue => "real",
                TokenType.BooleanValue => "boolean",
                TokenType.StringValue => "string",
                _ => "void"
            };
        }

        public override dynamic Visit(IfNode node)
        {
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
            return node.Token.Content;
        }

        public override dynamic Visit(SimpleTypeNode node)
        {
            return node.Token.Content;
        }

        public override dynamic Visit(ArrayTypeNode node)
        {
            return node.Token.Content; // todo?
        }

        public override dynamic Visit(ReturnStatementNode node)
        {
            return null; // TODO
        }

        public override dynamic Visit(AssertStatementNode node)
        {
            var type = node.Expression.Accept(this);

            if (type != "boolean")
            {
                throw new Exception($"non-boolean assertion");
            }

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

        public override dynamic Visit(ScopeStatementListNode node)
        {
            throw new System.NotImplementedException();
        }
    }
}