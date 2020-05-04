using System;
using System.Collections.Generic;
using Common;
using Common.AST;
using Common.Symbols;

namespace ScopeAnalyze
{
    /**
     * Creates scopes and assigns variables to them
     */
    public class ScopeVisitor : AnalyzeVisitor
    {
        public ScopeVisitor(Scope scope) : base(scope)
        {
            
        }

        public override dynamic Visit(ProgramNode node)
        {
            // already in main scope
            // CurrentScope.Node = node;
            var main = (FunctionVariable) GetFunctionOrProcedure("$main$");
            
            CreateScope(ScopeType.Main);
            var mainFunction = new Function
            {
                Variable = main,
                Scope = CurrentScope
            };
            node.Scope = CurrentScope;
            
            node.DeclarationBlock.Scope = CurrentScope;
            node.DeclarationBlock.Accept(this);
            var mainNode = node.MainBlock.Accept(this);
            mainNode.Scope = CurrentScope;
            node.MainBlock = new FunctionDeclarationNode
            {
                Id = new IdentifierNode
                {
                    Token = Token.Of(TokenType.Identifier, main.Name, SourceInfo.Of((0,0), (0,0,0)))
                },
                Statement = mainNode,
                Function = mainFunction,
                Parameters = new List<Node>(),
                Type = new SimpleTypeNode
                {
                    PrimitiveType = PrimitiveType.Void
                },
                Scope = CurrentScope
            };
            main.Node = mainNode;
            
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

            if (node.Left is ReturnStatementNode && !(node.Right is NoOpNode))
                throw new Exception($"unreachable code after return statement");
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

            foreach (var arg in node.Arguments) arg.Accept(this);

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
        }

        public override dynamic Visit(IfNode node)
        {
            CreateScope(ScopeType.IfThen);
            node.TrueBranch.Scope = CurrentScope;
            node.TrueBranch.Accept(this);
            ExitScope();
            CreateScope(ScopeType.IfElse);
            node.FalseBranch.Scope = CurrentScope;
            node.FalseBranch.Accept(this);
            ExitScope();

            return node;
        }

        public override dynamic Visit(WhileNode node)
        {
            CreateScope(ScopeType.While);
            node.Statement.Scope = CurrentScope;
            node.Statement.Accept(this);
            ExitScope();

            return node;
        }

        public override dynamic Visit(VarDeclarationNode node)
        {
            foreach (var idNode in node.Ids)
            {
                var id = idNode.Accept(this);
                var type = idNode.Type.Accept(this);
                var size = -1; // simple type
                // was: at.Size.Accept(this)
                var subType = PrimitiveType.Void;

                if (idNode.Type is ArrayTypeNode at)
                {
                    if (at.Size is NoOpNode)
                        size = 0; // array[]
                    else
                        size = -1; // array[expression]
                    subType = type;
                    type = PrimitiveType.Array;
                }

                var variable = new Variable
                {
                    Name = id,
                    PrimitiveType = type,
                    SubType = subType,
                    Size = size
                };
                
                if (!CurrentScope.SymbolTable.AddSymbol(variable))
                    throw new Exception($"variable {id} already declared");
                ((IdNode) idNode).Variable = variable;
            }

            return null;
        }

        public dynamic VisitFunctionOrProcedureDeclarationNode(FunctionOrProcedureDeclarationNode node)
        {
            var id = node.Id.Accept(this);

            /*if (node is ProcedureDeclarationNode)
            {
                if (!CurrentScope.SymbolTable.AddSymbol(new UserFunction
                {
                    Name = id,
                    Node = node
                }))
                {
                    throw new Exception($"procedure {id} already declared");
                };
                CreateScope(ScopeType.Procedure);
            }
            else
            {*/
            var type = node.Type.Accept(this);

            var functionVariable = new UserFunctionVariable
            {
                Name = id,
                Node = node,
                PrimitiveType = type
            };
            
            if (!CurrentScope.SymbolTable.AddSymbol(functionVariable))
                throw new Exception($"function {id} already declared");

            CreateScope(ScopeType.Function);
            var func = new Function
            {
                Variable = functionVariable,
                Scope = CurrentScope
            };
            node.Function = func;
            CurrentScope.Function = func;
            
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
                    size = atn.Size is NoOpNode ? 0 : 1;
                }

                var variable = new Variable
                {
                    Node = par,
                    Name = parId,
                    PrimitiveType = parType,
                    SubType = subType,
                    Size = size
                };
                
                if (!CurrentScope.SymbolTable.AddSymbol(variable))
                    throw new Exception(
                        $"{(parType == PrimitiveType.Void ? "procedure" : "function")} {id} parameter {parId} already declared");
                par.Variable = variable;
            }

            node.Scope = CurrentScope;
            node.Statement.Scope = CurrentScope;
            CurrentScope.Node = node;
            node.Statement.Accept(this);
            ExitScope();

            return null;
        }

        public override dynamic Visit(ProcedureDeclarationNode node)
        {
            return VisitFunctionOrProcedureDeclarationNode(node);
        }

        public override dynamic Visit(FunctionDeclarationNode node)
        {
            return VisitFunctionOrProcedureDeclarationNode(node);
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
            node.Function = CurrentScope.Function;
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

        public override dynamic Visit(VariableNode node)
        {
            return null;
        }

        public override dynamic Visit(ErrorNode node)
        {
            throw new NotImplementedException();
        }
    }
}