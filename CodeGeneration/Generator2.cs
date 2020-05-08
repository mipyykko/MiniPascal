using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using Common.AST;
using Common.Symbols;
using ScopeAnalyze;

namespace CodeGeneration
{
    public class CodeBlock
    {
        public List<CodeLine> Lines = new List<CodeLine>();

        public CodeBlock()
        {
        }

        public CodeBlock(CodeLine c) => Add(c);
        public CodeBlock(IEnumerable<CodeLine> c) => Add(c);
        public CodeBlock(CodeBlock c) => Add(c);
        
        public void Add(CodeLine c) => Lines.Add(c);
        public void Add(IEnumerable<CodeLine> c) => Lines.AddRange(c);
        public void Add(CodeBlock c) => Lines.AddRange(c.Lines);
        
        public override string ToString()
        {
            return string.Join("\n", Lines);
        }
    }
    
    public class CodeLine
    {
        public int Line;
        public string Code;
        public string Comment;

        private CodeLine(string code, string comment = "", int line = -1)
        {
            Line = line;
            Code = code;
            Comment = comment;
        }

        public static CodeLine Of(string code, string comment = "", int line = -1) => new CodeLine(code, comment, line);
        
        public override string ToString()
        {
            return $"{Code} # {(Line >= 0 ? $"{Line}: " : "")}{Comment}";
        }
    }

    public class Generator2
    {
        private List<CFG> Cfgs;
        private CodeBlock _code = new CodeBlock();
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

        public void AddCode(CodeLine c) => _code.Add(c);
        public void AddCode(IEnumerable<CodeLine> c) => _code.Add(c);
        
        public string Generate()
        {
            foreach (var cfg in Cfgs)
            {
                var function = cfg.Function;
                _scopeStack.Push(function.Scope);
                var signature = FunctionSignature(function);
                var body = new List<string>();
                
                _code.Add(CodeLine.Of(signature, $"{cfg.Function}"));

                foreach (var block in cfg.Blocks)
                {
                    _code.Add(CodeLine.Of($"L{block.Index}: "));
                    foreach (var statement in block.Statements)
                    {
                        var c = CreateStatement(statement);
                        _code.Add(c);
                    }

                    if (block.Child is BranchBlock bb)
                    {
                        
                        var ex = bb.Expression;
                    }
                    else if (block.Child != null)
                    {
                        AddCode(CodeLine.Of($"goto L{block.Child.Index};"));
                    }
                    else
                    {
                        AddCode(CodeLine.Of("return;"));
                    }
                    
                    
                }

                _scopeStack.Pop();
            }

            return _code.ToString(); //string.Join("\n", _code);
        }

        public CodeBlock CreateStatement(Node node)
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

        public CodeBlock VarDeclarationStatement(VarDeclarationNode node)
        {
            var output = new CodeBlock();

            foreach (var id in node.Ids)
            {
                var temp = NextTemporary;
                AddTemporary(new Variable{Name = id.Token.Content, Scope = CurrentScope}, temp);
                output.Add(CodeLine.Of($"{VarSignature(node, temp.Name)};", id.Token.Content, id.Token.SourceInfo.LineRange.Line));
            }

            return output;
        }

        public dynamic GetValue(ValueNode node)
        {
            return node.Type.PrimitiveType switch
            {
                PrimitiveType.String => $"\"{node.Value}\"",
                PrimitiveType.Boolean => node.Value == true ? 1 : 0,
                _ => node.Value
            };
        }
        
        public CodeBlock AssignmentStatement(AssignmentNode node)
        {
            var output = new CodeBlock();
            var (temp, code) = StoreOrLoad(node.LValue);

            if (node.Expression is ValueNode vn)
            {
                output.Add(CodeLine.Of($"{code}\n{temp.Name} = {GetValue(vn)}", node.LValue.Id.Token.Content));
            }
            else
            {

                var (temp2, code2) = ComputeTemporary(node.Expression);

                // TODO: del old temp?

                output.Add(code);
                output.Add(code2);
                output.Add(CodeLine.Of($"{temp.Name} = {temp2.Name}"));
            }

            return output;
        }

        public CodeBlock CallStatement(CallNode node)
        {
            var code = new CodeBlock();
            var args = new List<IVariable>();
                    
            foreach (var arg in node.Arguments)
            {
                var (argTemp, argCode) = ComputeTemporary(arg);
                code.Add(argCode);
                args.Add(argTemp);
            }

            var argStrings = new List<string>();

            for (var i = 0; i < node.Arguments.Count; i++)
            {
                var reference = ((ParameterNode) node.Function.Parameters[i]).Reference;
                argStrings.Add($"{(reference ? "&" : "")}{args[i].Name}");
            }

            var argString = string.Join(", ", argStrings);

            code.Add(CodeLine.Of($"{node.Id.Token.Content}({argString})"));
            return code; // TODO
        }

        public CodeBlock IfStatement(IfNode node)
        {
            return new CodeBlock();
        }

        public CodeBlock WhileStatement(WhileNode node)
        {
            return new CodeBlock();
        }

        private string PrintfType(PrimitiveType type) => type switch
        {
            PrimitiveType.Integer => "%d",
            PrimitiveType.Real => "%g",
            PrimitiveType.String => "%s",
            PrimitiveType.Boolean => "%d",
        };
        public CodeBlock WriteStatement(WriteStatementNode node)
        {
            var args = new List<IVariable>();
            var printfTypes = new List<string>();
            var result = new CodeBlock();

            foreach (var arg in node.Arguments)
            {
                var (t, code) = StoreOrLoad(arg);
                args.Add(t);
                printfTypes.Add(PrintfType(arg.Type.PrimitiveType));
                result.Add(code);
            }

            var printString = string.Join(" ", printfTypes);
            var argsString = string.Join(", ", args.Select(a => a.Name));
            
            result.Add(CodeLine.Of($"printf(\"{printString}\\n\", {argsString});"));

            return result;
        }

        public CodeBlock ReadStatement(ReadStatementNode node)
        {
            return new CodeBlock();
        }

        public CodeBlock AssertStatement(AssertStatementNode node)
        {
            return new CodeBlock();
        }

        public CodeBlock ReturnStatement(ReturnStatementNode node)
        {
            return new CodeBlock();
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

        public (IVariable, CodeBlock) ComputeTemporary(Node node)
        {
            switch (node)
            {
                case ValueOfNode vof: return ComputeTemporary(vof.LValue); // TODO
                case ValueNode n:
                {
                    var t = NextTemporary;
                    return (t, new CodeBlock(CodeLine.Of($"{t.Name} = {GetValue(n)};"))); // TODO
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
                                return (existing, new CodeBlock());
                            }

                            var t = NextTemporary;
                            var (res, code) = StoreOrLoad(vn);
                            AddTemporary(vn.Variable, t);
                            code.Add(CodeLine.Of($"{t.Name} = {res.Name}"));
                            return (t, code);
                        }
                        case ArrayDereferenceNode adn:
                        {
                            var t = NextTemporary;
                            var (res, code) = StoreOrLoad(adn);
                            code.Add(CodeLine.Of($"{t.Name} = {res.Name}"));
                            //var (res2, code2) = StoreOrLoad(adn.Expression);
                            return (t, code);
                        }
                    }
                    break;
                }
                case CallNode n:
                {
                    var code = new CodeBlock();
                    var args = new List<IVariable>();
                    
                    foreach (var arg in n.Arguments)
                    {
                        var (argTemp, argCode) = ComputeTemporary(arg);
                        code.Add(argCode);
                        args.Add(argTemp);
                    }

                    var argString = string.Join(", ", args.Select(a => a.Name));

                    var t = NextTemporary;

                    code.Add(CodeLine.Of($"{t.Name} = {n.Id.Token.Content}({argString})"));
                    return (t, code); // TODO
                }
            }

            return (null, new CodeBlock());
        }

        public (IVariable, CodeBlock) StoreOrLoad(Node node)
        {
            var output = new CodeBlock();
            if (node is ValueOfNode vof) return StoreOrLoad(vof.LValue);
            
            if (node is ArrayDereferenceNode adn)
            {
                var (temp, code) = StoreOrLoad(adn.LValue);
                var (tempIdx, code2) = ComputeTemporary(adn.Expression);

                output.Add(code); // TODO ?
                return (temp, output);
            }

            if (node is ValueNode vn)
            {
                var t = NextTemporary;
                output.Add(CodeLine.Of($"{_types[vn.Type.PrimitiveType]} {t.Name} = {GetValue(vn)}"));
                return (t, output);
            }

            if (node is CallNode cn)
            {
                return ComputeTemporary(cn);
            }
            // TODO: CallNode

            var variable = ((LValueNode) node).Variable;
            var tempVar = GetTemporary(variable);
            
            return (tempVar, output);
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
        
        public (IVariable, CodeBlock) CreateTemporaryFromExpression(BinaryOpNode node)
        {
            IVariable temp = null, prevTemp = null;
            var _node = node;
            var first = true;
            var output = new CodeBlock();

            while (true)
            {
                if (_node.Right is BinaryOpNode bop)
                {
                    var (bopTemp, bopCode) = CreateTemporaryFromExpression(bop);
                    output.Add(bopCode);
                }

                if (_node.Left is LValueNode)
                {
                    var (_temp, code) = ComputeTemporary(_node.Left);
                    output.Add(code);
                    temp = _temp;
                }

                if (_node.Left is ValueNode vn)
                {
                    var t = NextTemporary;
                    output.Add(CodeLine.Of($"{VarSignature(vn, t.Name)} = {vn.Value}"));
                    temp = t;
                }

                if (_node.Right is LValueNode)
                {
                    var (_temp, code) = ComputeTemporary(_node.Right);
                    output.Add(code);
                    output.Add(CodeLine.Of($"{temp.Name} = {temp.Name} {_node.Token.Content} {_temp.Name}"));

                    return (temp, output);
                }

                if (_node.Right is ValueNode vn2)
                {
                    //AddCode();
                    output.Add(CodeLine.Of($"{temp.Name} = {temp.Name} {_node.Token.Content} {vn2.Value}"));
                    return (temp, output);
                }

                // TODO: unarynode and what have you

                prevTemp = temp;

                if (_node.Right is NoOpNode) break;
            }

            return (prevTemp, output);
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
            var nodeParameters = ((FunctionOrProcedureDeclarationNode) variable.Node).Parameters; 
            for (var i = 0; i < nodeParameters.Count; i++)
            {
                var par = (ParameterNode) nodeParameters[i];
                var temp = NextTemporary;
                AddTemporary(par.Variable, temp);
                parameters.Add(VarSignature(par, $"{(par.Reference ? "*": "")}{temp.Name}")); // TODO: reference?
            }
            var code = $"{VarSignature(variable.Node, name)} ({string.Join(", ", parameters)})";

            return code;
        }
    }
}