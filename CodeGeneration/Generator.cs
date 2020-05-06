using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Common;
using Common.AST;
using Common.Symbols;
using ScopeAnalyze;

namespace CodeGeneration
{
    /*public class CodeBlock
    {
        public int Id;
        public string Code;
        public List<Block> Next;
    }*/
    
    public class Generator
    {
        private static int _nextLabel = 0;
        private static int _nextVariable = 0;
        private List<CFG> Cfgs;
        private Dictionary<Scope, Dictionary<IVariable, IVariable>> _variableToTemp = new Dictionary<Scope, Dictionary<IVariable, IVariable>>();
        private Stack<Scope> _scopeStack = new Stack<Scope>();
        private List<string> _code = new List<string>();
        
        public IVariable NextVariable => new TemporaryVariable
        {
            Name = $"v{_nextVariable++}"
        };
        private Scope CurrentScope => _scopeStack.Count > 0 ? _scopeStack.Peek() : null;
        public void AddCode(string s) => _code.Add(s);
        
        public Generator(List<CFG> cfgs)
        {
            Cfgs = cfgs;
        }

        public string Generate()
        {
            var code = new List<string>();
            
            foreach (var cfg in Cfgs)
            {
                var function = cfg.Function;

                _scopeStack.Push(function.Scope);
                
                foreach (var block in cfg.Blocks)
                {
                    code.Add(Label(block));

                    foreach (var node in block.Statements)
                    {
                        code.Add(CreateStatement(node));
                    }
                }

                _scopeStack.Pop();
            }

            return "";
        }

        public string CreateStatement(Node node)
        {
            switch (node)
            {
                case AssignmentNode n:
                    return AssignmentStatement(n);
                case CallNode n:
                    return CallStatement(n);
                case IfNode n:
                    return IfStatement(n);
                case WhileNode n:
                    return WhileStatement(n);
                case WriteStatementNode n:
                    return WriteStatement(n);
                case ReadStatementNode n:
                    return ReadStatement(n);
                case AssertStatementNode n:
                    return AssertStatement(n);
                case ReturnStatementNode n:
                    return ReturnStatement(n);
            }

            return null; // TODO
        }

        public string AssignmentStatement(AssignmentNode node)
        {
            var (temp, code) = StoreOrLoad(node);

            
            if (node.Expression is NumberValueNode)
            {
                
            }
            return "";
        }

        public string CallStatement(CallNode node)
        {
            return "";
        }

        public string IfStatement(IfNode node)
        {
            return "";
        }

        public string WhileStatement(WhileNode node)
        {
            return "";
        }
        
        public string WriteStatement(WriteStatementNode node)
        {
            return "";
        }

        public string ReadStatement(ReadStatementNode node)
        {
            return "";
        }

        public string AssertStatement(AssertStatementNode node)
        {
            return "";
        }

        public string ReturnStatement(ReturnStatementNode node)
        {
            return "";
        }
        
        public string Label(Block block)
        {
            return $"L{block.Index}";
        }

        public (IVariable, string) ComputeTemporary(Node node)
        {
            switch (node)
            {
                case ValueNode n:
                {
                    var t = NextVariable;
                    return (t, $"{t} = {n.Value};"); // TODO
                }
                case BinaryOpNode n:
                {
                    return CreateTemporaryFromExpression(n);
                }
                case LValueNode n:
                {
                    switch (n)
                    {
                        case VariableNode vn:
                        {
                            var existing = LookupTemporary(vn.Id.Token.Content); // TODO
                            if (existing != null)
                            {
                                return (existing, "");
                            }

                            var t = NextVariable;
                            var (res, code) = StoreOrLoad(vn);
                            StoreTemporary(t, vn.Variable);
                            return (t, $"{code}; {t} = {res}");
                        }
                        case ArrayDereferenceNode adn:
                        {
                            var t = NextVariable;
                            var (res, code) = StoreOrLoad(adn);
                            return (t, $"{code}; {t} = {res}");
                        }
                    }
                    break;
                }
                case CallNode n:
                {
                    return (null, ""); // TODO
                }
            }

            return (null, "");
        }

        public (IVariable, string) StoreOrLoad(Node node)
        {
            if (node is ArrayDereferenceNode adn)
            {
                var (temp, code) = StoreOrLoad(adn.LValue);
                var (tempIdx, code2) = ComputeTemporary(adn.Expression);

                return (temp, $"{code}{code2}"); // TODO
            }

            if (node is CallNode cn)
            {
                var (temp, code) = ComputeTemporary(cn);
                return (temp, code);
            }

            return (((IdNode) node).Variable, "");
        }
        
        private readonly Dictionary<string, string> _operators = new Dictionary<string, string>
        {
            ["="] = "==",
            ["<>"] = "!=",
            ["or"] = "|",
            ["and"] = "&&"
        };

        public (IVariable, string) CreateTemporaryFromExpression(BinaryOpNode node)
        {
            IVariable prevTemp = null;
            var code = new List<string>();
            var _node = node;
            var idx = 0;
            IVariable temp = null;
            
            while (!(_node.Right is NoOpNode))
            {
                if (_node.Left is LValueNode)
                {
                    var (_temp, code2) = ComputeTemporary(_node.Left);
                    code.Add(code2);
                    temp = _temp;
                }

                if (_node.Left is ValueNode vn)
                {
                    var t = NextVariable;
                    code.Add($"{t} = {vn.Value}");
                    temp = t;
                }

                if (idx == 0)
                {
                    prevTemp = temp;
                }
                else
                {
                    var res = NextVariable;
                    var op = _operators.TryGetValueOrDefault(_node.Token.Content, _node.Token.Content);
                    
                    code.Add($"{res} = {prevTemp}{op}{temp};");
                    prevTemp = res;
                }

                idx += 2;                
            }

            if (node.Right is NoOpNode) return (null, "");

            return (null, "");
        }
        
        public IVariable LookupTemporary(string s)
        {
            var symbol = CurrentScope.GetSymbol(s);
            var scopeDictionary = _variableToTemp.TryGetValueOrDefault(CurrentScope);

            return scopeDictionary?.TryGetValueOrDefault(symbol);
        }

        public void StoreTemporary(IVariable s, Variable v)
        {
            if (!_variableToTemp.ContainsKey(CurrentScope))
            {
                _variableToTemp[CurrentScope] = new Dictionary<IVariable, IVariable>();
            }

            _variableToTemp[CurrentScope][v] = s;
        }
        
    public class GeneratorVisitor : Visitor
    {
        public override dynamic Visit(ProgramNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(NoOpNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(StatementListNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(AssignmentNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(CallNode node)
        {
            throw new NotImplementedException();
        }

        private readonly Dictionary<string, string> _operators = new Dictionary<string, string>
        {
            ["="] = "==",
            ["<>"] = "!=",
            ["or"] = "|",
            ["and"] = "&&"
        };
        
        public override dynamic Visit(BinaryOpNode node)
        {
            var left = node.Left.Accept(this);
            var right = node.Right.Accept(this);
            var tokenOp = node.Token.Content;

            var generatedOp = _operators.TryGetValueOrDefault(tokenOp, tokenOp);

            return $"{left} {generatedOp} {right}";
        }

        public override dynamic Visit(UnaryOpNode node)
        {
            var tokenOp = node.Token.Content;
            var generatedOp = _operators.TryGetValueOrDefault(tokenOp, tokenOp);
            var expression = node.Expression.Accept(this);

            return $"{generatedOp}{expression}"; //TODO
        }

        public override dynamic Visit(ExpressionNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(SizeNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(IdentifierNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(LiteralNode node)
        {
            return node.Type.PrimitiveType switch
            {
                PrimitiveType.Integer => (dynamic) int.Parse(node.Token.Content),
                PrimitiveType.Real => float.Parse(node.Token.Content),
                PrimitiveType.Boolean => node.Token.Content.ToLower() == "true" ? 1 : 0,
                PrimitiveType.String => $"\"{node.Token.Content}\"",
                _ => throw new NotImplementedException()
            };
        }

        public override dynamic Visit(IfNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(WhileNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(VarDeclarationNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(ProcedureDeclarationNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(FunctionDeclarationNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(ParameterNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(TypeNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(SimpleTypeNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(ArrayTypeNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(ReturnStatementNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(AssertStatementNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(ReadStatementNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(WriteStatementNode node)
        {
            throw new NotImplementedException();
        }

        public override dynamic Visit(DeclarationListNode node)
        {
            throw new NotImplementedException();
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
}