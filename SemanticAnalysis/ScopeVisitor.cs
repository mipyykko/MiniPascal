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
            CurrentScope.Node = node;
            
            node.DeclarationBlock.Scope = CurrentScope;
            node.DeclarationBlock.Accept(this);
            node.MainBlock.Scope = CurrentScope;
            node.MainBlock.Accept(this);
            ExitScope();

            return node;
        }

        public override dynamic Visit(NoOpNode node)
        {
            return null;
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
            /*var fn = CurrentScope.GetSymbol(id);

            while (!(fn is UserFunction) && !(fn is BuiltinFunction))
            {
                var s = CurrentScope.Parent;
                if (s == null)
                {
                    throw new Exception($"can't call a non-function {id}");
                }

                fn = s.GetSymbol(id);
            }*/

            foreach (var arg in node.Arguments)
            {
                arg.Accept(this);
            }
            
            /*node.Function = fn.Node;
            node.Type = fn.Node?.Type;*/

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
            return null;
            /*
            var type = node.Token.Type;
            var content = node.Token.Content;

            return type switch
            {
                TokenType.IntegerValue => int.Parse(content),
                TokenType.RealValue => float.Parse(content),
                TokenType.BooleanValue => bool.Parse(content),
                _ => content
            };*/
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
                var size = idNode.Type is ArrayTypeNode at ? int.Parse(at.Size.Token.Content) : -1;
                // was: at.Size.Accept(this)
                var subType = PrimitiveType.Void;

                if (idNode.Type is ArrayTypeNode)
                {
                    subType = type;
                    type = PrimitiveType.Array;
                }

                if (!CurrentScope.SymbolTable.AddSymbol(new Variable
                {
                    Name = id,
                    PrimitiveType = type,
                    SubType = subType,
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
                var subType = PrimitiveType.Void;
                var size = -1;

                if (par.Type is ArrayTypeNode atn)
                {
                    parType = PrimitiveType.Array;
                    subType = atn.SubType;
                    size = atn.Size.Accept(this) ?? -1;
                }
                
                if (!CurrentScope.SymbolTable.AddSymbol(new Variable
                {
                    Node = par,
                    Name = parId,
                    PrimitiveType = parType,
                    SubType = subType,
                    Size = size
                }))
                {
                    throw new Exception($"procedure parameter {parId} already declared");
                }
            }

            node.Scope = CurrentScope;
            node.Statement.Scope = CurrentScope;
            CurrentScope.Node = node;
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
                PrimitiveType = type
            }))
            {
                throw new Exception($"function {id} already declared");
            }

            EnterScope(ScopeType.Function);

            foreach (ParameterNode par in node.Parameters)
            {
                var parId = par.Id.Accept(this);
                var parType = par.Type.Accept(this);
                var subType = PrimitiveType.Void;
                var size = -1;

                if (par.Type is ArrayTypeNode atn)
                {
                    parType = PrimitiveType.Array;
                    subType = atn.SubType;
                    size = atn.Size.Accept(this) ?? -1;
                }
                
                if (!CurrentScope.SymbolTable.AddSymbol(new Variable
                {
                    Node = par,
                    Name = parId,
                    PrimitiveType = parType,
                    SubType = subType,
                    Size = size
                }))
                {
                    throw new Exception($"function parameter {parId} already declared");
                }
            }

            node.Scope = CurrentScope;
            node.Statement.Scope = CurrentScope;
            CurrentScope.Node = node;
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
            return null;
        }

        public override dynamic Visit(SimpleTypeNode node)
        {
            return node.PrimitiveType;
        }

        public override dynamic Visit(ArrayTypeNode node)
        {
            var size = node.Size.Accept(this);
            var type = node.Type.Accept(this);
            
            return type; // TODO
        }

        public override dynamic Visit(ReturnStatementNode node)
        {
            node.Expression.Accept(this);
            return null;
        }

        public override dynamic Visit(AssertStatementNode node)
        {
            node.Expression.Accept(this);

            return null;
        }

        public override dynamic Visit(ReadStatementNode node)
        {
            return null;
        }

        public override dynamic Visit(WriteStatementNode node)
        {
            node.Arguments.ForEach(n => n.Accept(this));
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