using System;
using System.Collections.Generic;
using Common;
using Common.AST;
using Common.Symbols;
using ScopeAnalyze;

namespace CodeGeneration
{
    public class CodeBlock
    {
        public int Id;
        public string Code;
        public List<Block> Next;
    }

    public class Generator2
    {
        private List<CFG> Cfgs;
        private List<string> _code = new List<string>();
        private static int _nextTemporary = 0;
        private Stack<Scope> _scopeStack = new Stack<Scope>();
        private Dictionary<Scope, Dictionary<string, IVariable>> _temporaries = new Dictionary<Scope, Dictionary<string, IVariable>>();
        
        private Scope CurrentScope => _scopeStack.Count > 0 ? _scopeStack.Peek() : null;
        public IVariable NextTemporary => new TemporaryVariable
        {
            Name = $"v{_nextTemporary++}",
        };
        
        public Generator2(List<CFG> cfgs)
        {
            Cfgs = cfgs;
        }

        public void AddCode(string c) => _code.Add(c);
        public void AddCode(IEnumerable<string> c) => _code.AddRange(c);
        
        public string Generate()
        {
            foreach (var cfg in Cfgs)
            {
                var function = cfg.Function;
                _scopeStack.Push(function.Scope);
                var signature = FunctionSignature(function);
                var body = new List<string>();
                
                _code.Add(signature);

                foreach (var block in cfg.Blocks)
                {
                    AddCode($"L{block.Index}: ");
                    foreach (var statement in block.Statements)
                    {
                        var c = CreateStatement(statement);
                        AddCode(c);
                    }

                    if (block.Child is BranchBlock bb)
                    {
                        
                        var ex = bb.Expression;
                    }
                    else if (block.Child != null)
                    {
                        AddCode($"goto L{block.Child.Index};");
                    }
                    else
                    {
                        AddCode($"return;");
                    }
                    
                    
                }

                _scopeStack.Pop();
            }

            return string.Join("\n", _code);
        }

        public string CreateStatement(Node node)
        {
            return node switch
            {
                VarDeclarationNode n => VarDeclarationStatement(n),
                AssignmentNode n => AssignmentStatement(n),
                CallNode n => CallStatement(n),
                IfNode n => IfStatement(n),
                WhileNode n => WhileStatement(n),
                WriteStatementNode n => WriteStatement(n),
                ReadStatementNode n => ReadStatement(n),
                AssertStatementNode n => AssertStatement(n),
                ReturnStatementNode n => ReturnStatement(n),
                _ => null // TODO
            };
        }

        public string VarDeclarationStatement(VarDeclarationNode node)
        {
            var output = new List<String>();

            foreach (var id in node.Ids)
            {
                var temp = NextTemporary;
                AddTemporary(new Variable{Name = id.Token.Content, Scope = CurrentScope}, temp);
                output.Add($"{VarSignature(node, temp.Name)};");
            }

            return string.Join("\n", output);
        }

        public string AssignmentStatement(AssignmentNode node)
        {
            var (temp, code) = StoreOrLoad(node.LValue);

            if (node.Expression is ValueNode vn)
            {
                return $"{code}\n{temp.Name} = {vn.Value}";
            }

            var (temp2, code2) = ComputeTemporary(node.Expression);
            
            // TODO: del old temp?

            return $"{code}\n{code2}\n{temp.Name} = {temp2.Name}";
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

        public void AddTemporary(IVariable variable, IVariable temporary)
        {
            if (!_temporaries.ContainsKey(variable.Scope))
            {
                _temporaries[variable.Scope] = new Dictionary<string, IVariable>();
            }

            _temporaries[variable.Scope][variable.Name] = temporary;
        }

        public IVariable GetTemporary(IVariable variable)
        {
            if (!_temporaries.ContainsKey(CurrentScope)) return null;
            
            return _temporaries[CurrentScope].TryGetValueOrDefault(variable.Name);
        }

        public (IVariable, string) ComputeTemporary(Node node)
        {
            switch (node)
            {
                case ValueNode n:
                {
                    var t = NextTemporary;
                    return (t, $"{t.Name} = {n.Value};"); // TODO
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
                            var existing = GetTemporary(vn.Variable); // TODO
                            if (existing != null)
                            {
                                return (existing, existing.Name);
                            }

                            var t = NextTemporary;
                            var (res, code) = StoreOrLoad(vn);
                            AddTemporary(vn.Variable, t);
                            return (t, $"{code}{t.Name} = {res.Name}");
                        }
                        case ArrayDereferenceNode adn:
                        {
                            var t = NextTemporary;
                            var (res, code) = StoreOrLoad(adn);
                            var (res2, code2) = StoreOrLoad(adn.Expression);
                            return (t, $"{code}{code2} {t.Name}[{res2.Name}] = {res.Name}");
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

                return (temp, $"{code2}\n{code}[{tempIdx.Name}]");
            }

            if (node is ValueNode vn)
            {
                var t = NextTemporary;
                return (t, $"{_types[vn.Type.PrimitiveType]} {t.Name} = {vn.Value}");
            }
            // TODO: CallNode

            var variable = ((LValueNode) node).Variable;
            var tempVar = GetTemporary(variable);
            
            return (tempVar, tempVar.Name);
        }

        public List<dynamic> UnrollBinaryNode(BinaryOpNode node)
        {
            var result = new List<dynamic>();
            var first = true;
            var _node = node;

            var visitor = new ExpressionVisitor(CurrentScope);
            var value = node.Accept(visitor);

            return result;
        }
        
        public (IVariable, string) CreateTemporaryFromExpression(BinaryOpNode node)
        {
            IVariable temp = null, prevTemp = null;
            var _node = node;
            var first = true;
            
            do
            {
                if (_node.Left is LValueNode)
                {
                    var (_temp, code) = ComputeTemporary(_node.Left);
                    AddCode(code);
                    temp = _temp;
                }

                if (_node.Left is ValueNode vn)
                {
                    var t = NextTemporary;
                    AddCode($"{VarSignature(vn, t.Name)} = {vn.Value}");
                    temp = t;
                }

                if (_node.Right is LValueNode)
                {
                    var (_temp, code) = ComputeTemporary(_node.Right);
                    AddCode(code);
                    AddCode($"{temp.Name}{_node.Token.Content}{_temp.Name}");
                    temp = _temp;
                }

                if (_node.Right is ValueNode vn2)
                {
                    AddCode($"{temp.Name} = {temp.Name} {_node.Token.Content} {vn2.Value}");
                }

                if (_node.Right is BinaryOpNode bop)
                {
                    var (nextTemp, code) = CreateTemporaryFromExpression(bop);
                    temp = nextTemp;
                }
                if (prevTemp == null)
                {
                    prevTemp = temp;
                }
                else
                {
                    if (_node.Right is NoOpNode)
                    {
                        var result = NextTemporary;
                        
                    }
                }
            } while (false);

            return (prevTemp, "");
        }
        public string VarSignature(Node node, string name)
        {
            if (node.Type is SimpleTypeNode st)
            {
                var type = st.PrimitiveType switch
                {
                    PrimitiveType.Integer => "int",
                    PrimitiveType.Real => "float",
                    PrimitiveType.String => "char*",
                    PrimitiveType.Boolean => "bool",
                    PrimitiveType.Void => "void"
                };
                return $"{type} {name}";
            }

            if (node.Type is ArrayTypeNode at)
            {
                var size = -1;

                if (!(at.Size is NoOpNode) && !(at.Size is ValueNode))
                {
                    // evaluate the expression, output the temporary code and return the result temp
                }
                else if (at.Size is ValueNode vn)
                {
                    size = vn.Value;
                }
                
                var type = _types[at.SubType];

                return $"{type} {name}[{(size > 0 ? size.ToString() : "")}]";
            }
            
            throw new Exception($"got {node.Type}");
        }

        private readonly Dictionary<PrimitiveType, string> _types = new Dictionary<PrimitiveType, string>
        {
            [PrimitiveType.Integer] = "int",
            [PrimitiveType.Real] = "float",
            [PrimitiveType.String] = "char*",
            [PrimitiveType.Boolean] = "bool",
        };
        
        public string FunctionSignature(Function function)
        {
            var variable = function.Variable;
            var name = variable.Name;

            if (name.Equals("$main$"))
            {
                return "int main(int argc, char **argv)";
            }

            var parameters = new List<string>();

            foreach (ParameterNode n in ((FunctionOrProcedureDeclarationNode) variable.Node).Parameters)
            {
                var temp = NextTemporary;
                AddTemporary(n.Variable, temp);
                parameters.Add($"{VarSignature(n, temp.Name)}"); // TODO: reference?
            }
            var code = $"{VarSignature(variable.Node, name)} ({string.Join(", ", parameters)})";

            return code;
        }
    }
}