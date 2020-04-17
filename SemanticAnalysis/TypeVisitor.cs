using System;
using System.Collections.Generic;
using Common;
using Common.AST;
using Common.Symbols;

namespace ScopeAnalyze
{
    public class TypeVisitor : AnalyzeVisitor
    {
        private readonly Stack<Node> FunctionStack = new Stack<Node>();
        
        public TypeVisitor(Scope scope)
        {
            Scopes.Push(scope);
        }
        
        public override dynamic Visit(ProgramNode node)
        {
            EnterScope(node.Scope);
            FunctionStack.Push(node);
            node.DeclarationBlock.Accept(this);
            node.MainBlock.Accept(this);
            FunctionStack.Pop();
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
            var id = node.Id.Token.Content; // Accept(this);
            var variable = GetVariable(id);
            var indexExpressionType = node.IndexExpression.Accept(this);

            if (indexExpressionType != null && variable.PrimitiveType != PrimitiveType.Array)
            {
                throw new Exception($"cannot index a non-array variable {id}");
            }
            var statementType = node.Expression.Accept(this);

            if (variable.PrimitiveType == PrimitiveType.Array)
            {
                if (indexExpressionType != null && statementType != variable.SubType)
                {
                    throw new Exception($"type error: can't assign {statementType} to array {id} of type {variable.SubType}");
                }
                if (statementType != PrimitiveType.Array)
                {
                    throw new Exception($"cannot assign non-array to array {id}");
                }
            }
            else
            {
                if (variable.PrimitiveType != statementType)
                {
                    throw new Exception($"type error: can't assign {statementType} to variable {id} of type {variable.PrimitiveType}");
                }
            }

            return null;
        }

        public override dynamic Visit(CallNode node)
        {
            var id = node.Id.Accept(this);
            var callable = (IVariable) GetFunctionOrProcedure(id);

            foreach (var argument in node.Arguments)
            {
                argument.Accept(this);
            }
            
            // TODO: next up - check the argument types
            
            throw new NotImplementedException();
            return callable.PrimitiveType;
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

            /*var leftType = node.Left is IdentifierNode
                ? ((Variable) GetVariable(left)).PrimitiveType
                : left;
            var rightType = node.Right is IdentifierNode
                ? ((Variable) GetVariable(right)).PrimitiveType
                : right;*/
            
            if (left != right)
            {
                throw new Exception($"type error: can't perform {left} {op} {right}");
            }

            var type = RelationalOperators.Includes(op) ? PrimitiveType.Boolean : left;

            // node.Type.PrimitiveType = type;// TODO: not what's supposed to be

            return type;
        }

        public override dynamic Visit(UnaryOpNode node)
        {
            var op = node.Token.Content;
            var type = node.Expression.Accept(this);

            if ((type != PrimitiveType.Boolean && op == "not") ||
                ("+-".Contains(op) && type != PrimitiveType.Integer && type != PrimitiveType.Real))
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
            var variable = node.Variable.Accept(this); // node.Identifier.Accept(this);
            // var variable = (Variable) GetVariable(id);
            
            /*if (variable.PrimitiveType != PrimitiveType.Array)
            {
                throw new Exception($"syntax error: tried to get size from non-array {id}");
            }*/

            return PrimitiveType.Integer;
        }

        public override dynamic Visit(IdentifierNode node)
        {
            return node.Token.Content;
        }

        public override dynamic Visit(LiteralNode node)
        {
            return node.Type.PrimitiveType;
        }

        public override dynamic Visit(IfNode node)
        {
            var expressionType = node.Expression.Accept(this);

            if (expressionType != PrimitiveType.Boolean)
            {
                throw new Exception($"expected a boolean expression in if statement condition");
            }

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
            var expressionType = node.Expression.Accept(this);

            if (expressionType != PrimitiveType.Boolean)
            {
                throw new Exception($"expected a boolean expression in while statement condition");
            }

            EnterScope(node.Statement.Scope);
            node.Statement.Accept(this);
            ExitScope();

            return null;
        }

        public override dynamic Visit(VarDeclarationNode node)
        {
            node.Ids.ForEach(id => id.Accept(this));
            return null;
        }

        public override dynamic Visit(ProcedureDeclarationNode node)
        {
            EnterScope(node.Statement.Scope);
            FunctionStack.Push(node);
            node.Parameters.ForEach(p => p.Accept(this));
            node.Statement.Accept(this);
            FunctionStack.Pop();
            ExitScope();

            return null;
        }

        public override dynamic Visit(FunctionDeclarationNode node)
        {
            EnterScope(node.Statement.Scope);
            FunctionStack.Push(node);
            node.Parameters.ForEach(p => p.Accept(this));
            node.Statement.Accept(this);
            FunctionStack.Pop();
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
            throw new System.NotImplementedException();
        }

        public override dynamic Visit(SimpleTypeNode node)
        {
            return node.PrimitiveType;
        }

        public override dynamic Visit(ArrayTypeNode node)
        {
            return node.PrimitiveType; // TODO ? return subtype instead?
        }

        public override dynamic Visit(ReturnStatementNode node)
        {
            var currentNode = (IdNode) FunctionStack.Peek();
            var id = currentNode.Id.Accept(this);
            var type = node.Expression.Accept(this);

            if (!(node.Expression is NoOpNode) && currentNode is ProcedureDeclarationNode pn)
            {
                throw new Exception($"cannot return a value from procedure {id}");
            }

            if (node.Expression is NoOpNode && currentNode is FunctionDeclarationNode fn)
            {
                throw new Exception($"must return value of {type} from function {id}");
            }

            if (type != currentNode.Type.PrimitiveType)
            {
                throw new Exception($"must return value of type {currentNode.Type.PrimitiveType} from function{id}, tried to return {type}");
            }

            return null;
        }

        public override dynamic Visit(AssertStatementNode node)
        {
            var type = node.Expression.Accept(this);

            if (type != PrimitiveType.Boolean)
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

        public override dynamic Visit(VariableNode node)
        {
            var id = node.Id.Accept(this);
            var index = node.IndexExpression.Accept(this);
            var variable = (Variable) GetVariable(id);

            if (index != null)
            {
                if (variable.PrimitiveType != PrimitiveType.Array)
                {
                    throw new Exception($"can't index a non-array {id}");
                }

                node.Type = new SimpleTypeNode
                {
                    PrimitiveType = variable.SubType,
                };
                return variable.SubType;
            }

            if (variable.PrimitiveType == PrimitiveType.Array)
            {
                node.Type = new ArrayTypeNode
                {
                    PrimitiveType = PrimitiveType.Array,
                    SubType = variable.SubType
                };
                return variable.PrimitiveType;
            } 

            node.Type = new SimpleTypeNode
            {
                PrimitiveType = variable.PrimitiveType
            };
            
            return variable.PrimitiveType;
        }
    }
}