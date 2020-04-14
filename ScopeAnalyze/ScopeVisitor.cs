using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.AST;
using Common.Symbols;

namespace ScopeAnalyze
{
    public class ScopeVisitor : Visitor
    {
        private readonly Stack<Scope> _scopes = new Stack<Scope>();

        public ScopeVisitor(Scope s)
        {
            _scopes.Push(s);
        }
        
        private void EnterScope(ScopeType type)
        {
            var parent = _scopes.Any() ? CurrentScope : null;
            var scope = new Scope
            {
                Parent = parent,
                ScopeType = type,
                SymbolTable = new SymbolTable()
            };
            
            Console.WriteLine($"Entering {scope}");
            
            _scopes.Push(scope);
        }

        private void ExitScope()
        {
            if (_scopes.Any())
            {
                Console.WriteLine($"Exiting {CurrentScope}");
                _scopes.Pop();
            }
        }

        private Scope CurrentScope => _scopes.Peek();
        
        public override dynamic Visit(ProgramNode node)
        {
            // already in main scope
            node.DeclarationBlock.Scope = CurrentScope;
            node.DeclarationBlock.Accept(this);
            node.MainBlock.Scope = CurrentScope;
            node.MainBlock.Accept(this);
            ExitScope();

            return node;
        }

        public override dynamic Visit(NoOpNode node)
        {
            return node;
        }

        public override dynamic Visit(StatementListNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);

            return node;
        }

        public override dynamic Visit(AssignmentNode node)
        {
            return null;
        }

        public override dynamic Visit(CallNode node)
        {
            var id = node.Id.Accept(this);
            var fn = CurrentScope.SymbolTable.GetSymbol(id);

            if (!(fn is UserFunction) && !(fn is BuiltinFunction))
            {
                throw new Exception($"can't call a non-function {id}");
            }

            foreach (var arg in node.Arguments)
            {
                arg.Accept(this);
            }
            
            node.Function = fn.Node;
            
            return null;
        }

        public override dynamic Visit(BinaryOpNode node)
        {
            return null;
        }

        public override dynamic Visit(UnaryOpNode node)
        {
            return null;
        }

        public override dynamic Visit(ExpressionNode node)
        {
            return null;
        }

        public override dynamic Visit(SizeNode node)
        {
            return null;
        }

        public override dynamic Visit(IdentifierNode node)
        {
            return node.Token.Content;
        }

        public override dynamic Visit(LiteralNode node)
        {
            var type = node.Token.Type;
            var content = node.Token.Content;

            return type switch
            {
                TokenType.IntegerValue => int.Parse(content),
                TokenType.RealValue => float.Parse(content),
                TokenType.BooleanValue => bool.Parse(content),
                _ => content
            };
        }

        public override dynamic Visit(IfNode node)
        {
            EnterScope(ScopeType.IfThen);
            node.TrueBranch.Scope = CurrentScope;
            node.TrueBranch.Accept(this);
            ExitScope();
            EnterScope(ScopeType.IfElse);
            node.FalseBranch.Scope = CurrentScope;
            node.FalseBranch.Accept(this);
            ExitScope();

            return node;
        }

        public override dynamic Visit(WhileNode node)
        {
            EnterScope(ScopeType.While);
            node.Statement.Scope = CurrentScope;
            node.Statement.Accept(this);
            ExitScope();

            return node;
        }

        public override dynamic Visit(VarDeclarationNode node)
        {
            Console.WriteLine(string.Join(", ", node.Ids));
            
            foreach (var idNode in node.Ids)
            {
                var id = idNode.Accept(this);
                var type = idNode.Type.Accept(this);
                var size = idNode.Type is ArrayTypeNode at ? at.Size.Accept(this) : -1;

                if (!CurrentScope.SymbolTable.AddSymbol(new Variable
                {
                    Name = id,
                    Type = type,
                    Size = size
                }))
                {
                    throw new Exception($"variable {id} already declared");
                };
            }

            return null;
        }

        public override dynamic Visit(ProcedureDeclarationNode node)
        {
            var id = node.Id.Accept(this);
            
            if (!CurrentScope.SymbolTable.AddSymbol(new UserFunction
            {
                Name = id,
                Node = node
            }))
            {
                throw new Exception($"procedure {id} already declared");
            };

            EnterScope(ScopeType.Procedure);

            foreach (ParameterNode par in node.Parameters)
            {
                var parId = par.Id.Accept(this);
                var parType = par.Type.Accept(this);
                
                if (!CurrentScope.SymbolTable.AddSymbol(new Variable
                {
                    Node = par,
                    Name = parId,
                    Type = parType
                }))
                {
                    throw new Exception($"procedure parameter {parId} already declared");
                }
            }

            node.Statement.Scope = CurrentScope;
            node.Statement.Accept(this);
            ExitScope();
            return node;
        }

        public override dynamic Visit(FunctionDeclarationNode node)
        {
            var id = node.Id.Accept(this);
            var type = node.Type.Accept(this);

            if (!CurrentScope.SymbolTable.AddSymbol(new UserFunction
            {
                Name = id,
                Node = node,
                Type = type
            }))
            {
                throw new Exception($"function {id} already declared");
            }

            EnterScope(ScopeType.Function);

            foreach (ParameterNode par in node.Parameters)
            {
                var parId = par.Id.Accept(this);
                var parType = par.Type.Accept(this);

                if (!CurrentScope.SymbolTable.AddSymbol(new Variable
                {
                    Node = par,
                    Name = parId,
                    Type = parType
                }))
                {
                    throw new Exception($"function parameter {parId} already declared");
                }
            }

            node.Statement.Scope = CurrentScope;
            node.Statement.Accept(this);
            ExitScope();

            return node;
        }

        public override dynamic Visit(ParameterNode node)
        {
            return null;
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
            var size = node.Size.Accept(this);
            var type = node.Type.Accept(this);
            
            return type; // TODO
        }

        public override dynamic Visit(ReturnStatementNode node)
        {
            return null;
        }

        public override dynamic Visit(AssertStatementNode node)
        {
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

            return node;
        }

        public override dynamic Visit(ScopeStatementListNode node)
        {
            EnterScope(ScopeType.Unknown); // TODO
            node.Left.Scope = CurrentScope;
            node.Right.Scope = CurrentScope;
            node.Left.Accept(this);
            node.Right.Accept(this);
            ExitScope();

            return node;
        }
    }
}