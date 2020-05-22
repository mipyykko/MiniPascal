using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Common;
using Common.AST;
using Common.Symbols;

namespace ScopeAnalyze
{
    public class TypeVisitor : AnalyzeVisitor
    {
        private readonly Stack<Node> FunctionStack = new Stack<Node>();

        public TypeVisitor(Scope scope) : base(scope)
        {
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

        private bool CheckLHSType(TypeNode l, TypeNode r)
        {
            if (l.GetType() != r.GetType()) return false;

            switch (l)
            {
                case SimpleTypeNode st when st.PrimitiveType != r.PrimitiveType:
                case ArrayTypeNode at when r.PrimitiveType != PrimitiveType.Array:
                    return false;
                case ArrayTypeNode at when at.SubType != ((ArrayTypeNode) r).SubType:
                    return false;
                default:
                    return true;
            }
        }
        
        public override dynamic Visit(AssignmentNode node)
        {
            // var id = node.Id.Token.Content; // Accept(this);
            //var variable = GetVariable(id);
            // var variable = node.Variable;
            var id = node.LValue.Id.Accept(this);
            node.LValue.Accept(this);

            var lValueType = node.LValue.Type;
            //var indexExpressionType = node.IndexExpression.Accept(this);

            //if (indexExpressionType != null && variable.PrimitiveType != PrimitiveType.Array)
            //    throw new Exception($"cannot index a non-array variable {id}");
            node.Expression.Accept(this);

            var statementType = node.Expression.Type;
            
            if (!CheckLHSType(lValueType, statementType))
            {
                throw new Exception($"type error: can't assign {statementType} to {id} of type {lValueType}");
            }
            /*if (variable.PrimitiveType == PrimitiveType.Array)
            {
                // TODO: check sizes?
                if (indexExpressionType != null && statementType != variable.SubType)
                    throw new Exception(
                        $"type error: can't assign {statementType} to array {id} of type {variable.SubType}");
                if (statementType != PrimitiveType.Array) throw new Exception($"cannot assign non-array to array {id}");
            }
            else
            {
                if (variable.PrimitiveType != statementType)
                    throw new Exception(
                        $"type error: can't assign {statementType} to variable {id} of type {variable.PrimitiveType}");
            }*/

            return null;
        }

        private (PrimitiveType, PrimitiveType, int) GetArgumentInfo(Node node)
        {
            switch (node)
            {
                case ValueOfNode vofNode: {
                    switch (vofNode.LValue)
                    {
                        case VariableNode varNode:
                        {
                            var argId = varNode.Id.Accept(this);
                            var argVariable = (Variable) GetVariable(argId);

                            return (argVariable.PrimitiveType, argVariable.SubType, argVariable.Size);
                        }
                        case ArrayDereferenceNode arrNode:
                        {
                            
                            var argId = ((VariableNode) arrNode.LValue).Id.Accept(this);
                            var argVariable = (Variable) GetVariable(argId);
                            var index = arrNode.Expression.Accept(this);

                            return index != null
                                ? (argVariable.SubType, PrimitiveType.Void, -1)
                                : (PrimitiveType.Array, argVariable.SubType, argVariable.Size);
                        }
                    }

                    throw new Exception($"got unknown argument {vofNode}");
                }    
                case VariableNode varNode:
                {
                    var argId = varNode.Id.Accept(this);
                    var argVariable = (Variable) GetVariable(argId);

                    return (argVariable.PrimitiveType, argVariable.SubType, argVariable.Size);
                }
                case ArrayDereferenceNode arrNode:
                {
                    var argId = ((VariableNode) arrNode.LValue).Id.Accept(this);
                    var argVariable = (Variable) GetVariable(argId);
                    var index = arrNode.Expression.Accept(this);

                    return index != null 
                        ? (argVariable.SubType, PrimitiveType.Void, -1) 
                        : (PrimitiveType.Array, argVariable.SubType, argVariable.Size);
                }
                case ValueNode valNode:
                {
                    return (valNode.Type.PrimitiveType, PrimitiveType.Void, -1);
                }
                case BinaryOpNode binOpNode:
                {
                    return (binOpNode.Type.PrimitiveType, PrimitiveType.Void, -1);
                }
                case UnaryOpNode unaryOpNode:
                {
                    return (unaryOpNode.Type.PrimitiveType, PrimitiveType.Void, -1);
                }
                case CallNode callNode:
                {
                    return (callNode.Type.PrimitiveType, PrimitiveType.Void, -1);
                }
            }
            
            throw new Exception($"got unknown argument {node}");
        }
        
        public override dynamic Visit(CallNode node)
        {
            var id = node.Id.Accept(this);
            var callable = (FunctionVariable) GetFunctionOrProcedure(id);

            if (callable == null)
            {
                throw new Exception($"function or procedure {id} not declared");
            }

            var callableNode = (FunctionDeclarationNode) callable.Node;
            node.Type = callableNode.Type;
            var callableParameters = callableNode.Parameters;
            
            if (node.Arguments.Count != callableParameters.Count)
                throw new Exception(
                    $"wrong number of parameters given for function or procedure {id}: expected {callableParameters.Count}, got {node.Arguments.Count}");

            for (var i = 0; i < node.Arguments.Count; i++)
            {
                var parameter = (ParameterNode) callableParameters[i];
                if (!parameter.Reference && node.Arguments[i] is LValueNode ln)
                {
                    node.Arguments[i] = new ValueOfNode
                    {
                        LValue = ln,
                        Type = ln.Type
                    };
                }

                node.Arguments[i].Accept(this);
            }

            var arguments = node.Arguments;
            
            for (var i = 0; i < arguments.Count; i++)
            {
                var arg = arguments[i];

                var parameterNode = (ParameterNode) callableParameters[i];
                var parameterId = parameterNode.Id.Accept(this);

                var parameterVariable = (Variable) GetVariable(parameterId, callableNode.Scope);
                var parameterType = parameterVariable.PrimitiveType;
                var parameterSubType = parameterVariable.SubType;
                var parameterSize = -1;

                if (parameterNode.Reference && !(arg is LValueNode))
                    throw new Exception(
                        $"wrong parameter type for {id} - {parameterId} expected a variable or an array dereference, got {arg}");

                var (argType, argSubType, argSize) = GetArgumentInfo(arg);

                if (argType != parameterType)
                    throw new Exception(
                        $"wrong parameter type for {id} - {parameterId}: expected {parameterType}, got {argType}");

                if (argType != PrimitiveType.Array) continue;

                if (argSubType != parameterSubType)
                    throw new Exception(
                        $"wrong parameter type for {id} - array {parameterId} expected to be of subtype {parameterSubType}, got {argSubType}");

                if (parameterSize >= 0 && argSize != parameterSize)
                    throw new Exception(
                        $"incompatible array types: expected array of size {parameterSize}, got {argSize}");
            }
            // TODO: next up - check the argument types

            return callableNode.Type.PrimitiveType;
        }

        private static readonly OperatorType[] RelationalOperators =
        {
            OperatorType.Eq,
            OperatorType.Neq,
            OperatorType.Lt,
            OperatorType.Leq,
            OperatorType.Geq,
            OperatorType.Gt
        };

        private static readonly OperatorType[] ArithmeticOperators =
        {
            OperatorType.Add,
            OperatorType.Sub,
            OperatorType.Mul,
            OperatorType.Div
        };

        private static readonly Dictionary<PrimitiveType, OperatorType[]> PermittedOperations =
            new Dictionary<PrimitiveType, OperatorType[]>
            {
                [PrimitiveType.Integer] = ArithmeticOperators.Concat(RelationalOperators).Concat(new[] {OperatorType.Mod}).ToArray(),
                [PrimitiveType.Real] = ArithmeticOperators.Concat(RelationalOperators).ToArray(),
                [PrimitiveType.String] = RelationalOperators.Concat(new[] {OperatorType.Add}).ToArray(),
                [PrimitiveType.Boolean] = RelationalOperators.Concat(new[] {OperatorType.And, OperatorType.Or}).ToArray(),
                [PrimitiveType.Array] = new OperatorType[] { },
                [PrimitiveType.Void] = new OperatorType[] { }
            };

        public override dynamic Visit(BinaryOpNode node)
        {
            var left = node.Left.Accept(this);
            var right = node.Right.Accept(this);

            var op = node.Op;

            var ops = (OperatorType[]) PermittedOperations[left];

            if (left != right || left == right && !ops.Contains(op))
                throw new Exception($"type error: can't perform {left} {op} {right}");

            var type = RelationalOperators.Contains(op)
                ? PrimitiveType.Boolean
                : left;

            node.Type = new SimpleTypeNode
            {
                PrimitiveType = type
            };
            return type;
        }

        public override dynamic Visit(UnaryOpNode node)
        {
            var op = node.Op;
            var type = node.Expression.Accept(this);

            if (type != PrimitiveType.Boolean && op == OperatorType.Not ||
                new[] { OperatorType.Add, OperatorType.Sub }.Contains(op) && type != PrimitiveType.Integer && type != PrimitiveType.Real)
                throw new Exception($"invalid op {op} on {type}");

            node.Type = new SimpleTypeNode
            {
                PrimitiveType = type
            };
            return type;
        }

        public override dynamic Visit(ExpressionNode node)
        {
            var type = node.Expression.Accept(this);

            return type;
        }

        public override dynamic Visit(SizeNode node)
        {
            var id = node.LValue.Id.Accept(this);
            var variableType = node.LValue.Accept(this); // node.Identifier.Accept(this);
            var variable = (Variable) GetVariable(id);

            if (variable.PrimitiveType != PrimitiveType.Array)
                throw new Exception($"syntax error: tried to get size from non-array {id}");
            if (node.LValue is ArrayDereferenceNode) 
                throw new Exception($"syntax error: tried to get size from a non-array element of array {id}");

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
                throw new Exception($"expected a boolean expression in if statement condition");

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
                throw new Exception($"expected a boolean expression in while statement condition");

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
                if (idNode.Type is ArrayTypeNode at)
                {
                    var size = at.Size.Accept(this);

                    if (size != PrimitiveType.Integer)
                        throw new Exception($"array {id} size must be an integer expression");
                }
            }

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
        }

        public override dynamic Visit(SimpleTypeNode node)
        {
            return node.PrimitiveType;
        }

        public override dynamic Visit(ArrayTypeNode node)
        {
            return node.SubType; // TODO ? return subtype instead?
        }

        public override dynamic Visit(ReturnStatementNode node)
        {
            var currentNode = (IdNode) FunctionStack.Peek();
            var id = currentNode.Id.Accept(this);
            node.Expression.Accept(this);

            var type = currentNode.Type;
            
            var expressionType = node.Expression.Type.PrimitiveType;
            var expressionSubType = node.Expression.Type is ArrayTypeNode eat ? eat.SubType : PrimitiveType.Void;

            if (!(node.Expression is NoOpNode) && currentNode.Type.PrimitiveType == PrimitiveType.Void && expressionType != PrimitiveType.Void)
                throw new Exception($"cannot return a value from procedure {id}");

            if (node.Expression is NoOpNode && expressionType != PrimitiveType.Void && currentNode is FunctionDeclarationNode fn)
                throw new Exception($"must return value of {type} from function {id}");

            if (type is ArrayTypeNode at)
            {
                if (expressionType != PrimitiveType.Array)
                {
                    throw new Exception(
                        $"must return array of type {at.SubType} from function{id}, tried to return {expressionType}");
                }
                if (at.SubType != expressionSubType)
                {
                    throw new Exception(
                        $"must return array of type {at.SubType} from function{id}, tried to return array of {expressionSubType}");
                }
            }
            else
            {
                if (expressionType != type.PrimitiveType)
                    throw new Exception(
                        $"must return value of type {type.PrimitiveType} from function{id}, tried to return {expressionType}");
            }

            return null;
        }

        public override dynamic Visit(AssertStatementNode node)
        {
            var type = node.Expression.Accept(this);

            if (type != PrimitiveType.Boolean) throw new Exception($"non-boolean assertion");

            return null;
        }

        public override dynamic Visit(ReadStatementNode node)
        {
            foreach (var v in node.Variables)
            {
                if (!(v is LValueNode))
                    throw new Exception($"read statement must have a variable as a parameter, got {v}");
                v.Accept(this);
            }

            return null;
        }

        private readonly PrimitiveType[] WriteStatementTypes =
        {
            PrimitiveType.Integer,
            PrimitiveType.Real,
            PrimitiveType.String
        };

        public override dynamic Visit(WriteStatementNode node)
        {
            for (var i = 0; i < node.Arguments.Count; i++) {
                if (node.Arguments[i] is LValueNode lvn)
                {
                    node.Arguments[i] = new ValueOfNode
                    {
                        LValue = lvn,
                        Type = lvn.Type,
                        Token = lvn.Token
                    };
                }

                var argument = node.Arguments[i];
                argument.Accept(this);
                // var id = ((IdNode) argument).Id.Accept(this);

                if (!WriteStatementTypes.Includes(argument.Type.PrimitiveType))
                    throw new Exception($"invalid parameter of type {argument.Type.PrimitiveType} for writeln");
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
            // var index = node.IndexExpression.Accept(this);
            var variable = (Variable) GetVariable(id);

            if (variable == null) throw new Exception($"variable {id} not defined");
            /*if (index != null)
            {
                if (variable.PrimitiveType != PrimitiveType.Array) throw new Exception($"can't index a non-array {id}");

                node.Type = new SimpleTypeNode
                {
                    PrimitiveType = variable.SubType,
                };
                return variable.SubType;
            }
            */
            if (variable.PrimitiveType == PrimitiveType.Array)
            {
                node.Type = new ArrayTypeNode
                {
                    PrimitiveType = PrimitiveType.Array,
                    SubType = variable.SubType
                };
                return variable.SubType;
            }

            node.Type = new SimpleTypeNode
            {
                PrimitiveType = variable.PrimitiveType
            };

            return variable.PrimitiveType;
        }

        public override dynamic Visit(ArrayDereferenceNode node)
        {
            // node.LValue.Accept(this);
            var lValueType = node.LValue.Accept(this);
            node.Expression.Accept(this);

            if (lValueType != PrimitiveType.Integer)
            {
                throw new Exception("type error: array index must be an integer expression");
            }
            var expressionType = node.Expression is NoOpNode ? null : node.Expression.Type;
            
            /*if (expressionType == null)
            {
                node.Type = new ArrayTypeNode
                {
                    PrimitiveType = PrimitiveType.Array,
                    SubType = ((ArrayTypeNode) node.LValue.Type).SubType
                };
                return PrimitiveType.Array;
            }*/

            node.Type = new SimpleTypeNode
            {
                PrimitiveType = lValueType // ((ArrayTypeNode) lValueType).SubType
            };

            return lValueType;
        }

        public override dynamic Visit(ValueOfNode node)
        {
            var lValue = node.LValue.Accept(this);
            node.Type = node.LValue.Type;
            
            return lValue;
        }

        public override dynamic Visit(ErrorNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(IntegerValueNode node)
        {
            return PrimitiveType.Integer;
        }

        public override dynamic Visit(RealValueNode node)
        {
            return PrimitiveType.Real;
        }

        public override dynamic Visit(StringValueNode node)
        {
            return PrimitiveType.String;
        }

        public override dynamic Visit(BooleanValueNode node)
        {
            return PrimitiveType.Boolean;
        }
    }
}