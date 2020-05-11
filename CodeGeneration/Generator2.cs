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
        public static int MaxLineLength = 0;
        
        public List<CodeLine> Lines = new List<CodeLine>();

        public CodeBlock()
        {
        }

        public CodeBlock(CodeLine c) => Add(c);
        public CodeBlock(IEnumerable<CodeLine> c) => Add(c);
        public CodeBlock(CodeBlock c) => Add(c);
        
        public void Add(CodeLine c)
        {
            MaxLineLength = Math.Max(MaxLineLength, c.Code.Length);
            Lines.Add(c);
        }

        public void Add(IEnumerable<CodeLine> c)
        {
            foreach (var l in c) Add(l);
        }

        public void Add(CodeBlock c) => Add(c.Lines);

        public override string ToString()
        {
            return string.Join("\n", Lines.Select(l => l.GetString(MaxLineLength)));
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

        public string GetString(int lineLength = 0)
        {
            var sb = new StringBuilder(Code.PadRight(lineLength));
            if (Line >= 0 || Comment.Length > 0)
            {
                sb.Append($" // {(Line >= 0 ? $"{Line}: " : "")}{Comment}");
            }
            
            return sb.ToString();
        }
        
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
        public TemporaryVariable NextTemporary => new TemporaryVariable
        {
            Name = $"v{_nextTemporary++}",
        };

        public Generator2(List<CFG> cfgs)
        {
            Cfgs = cfgs;
        }

        public void AddCode(CodeLine c) => _code.Add(c);
        public void AddCode(IEnumerable<CodeLine> c) => _code.Add(c);
        public static CodeLine EmptyLine => CodeLine.Of("");
        
        public string Generate()
        {
            foreach (var cfg in Cfgs)
            {
                var function = cfg.Function;
                _scopeStack.Push(function.Scope);
                var signature = FunctionSignature(function);
                var body = new List<string>();
                
                _code.Add(CodeLine.Of($"{signature} {{", $"{cfg.Function}"));

                foreach (var block in cfg.Blocks)
                {
                    _code.Add(CodeLine.Of($"L{block.Index}:;"));
                    foreach (var statement in block.Statements)
                    {
                        var c = CreateStatement(statement);
                        _code.Add(c);
                    }

                    if (block.Child is BranchBlock bb)
                    {
                        var expression = bb.Expression;
                        var (t, code) = ComputeTemporary(expression);

                        var ifBlock = new CodeBlock();
                        ifBlock.Add(code);
                        ifBlock.Add(CodeLine.Of($"if ({t.Name}) goto L{bb.TrueBlock.Index};"));
                        ifBlock.Add(CodeLine.Of($"goto L{bb.FalseBlock.Index};", "else/end while"));

                        AddCode(ifBlock.Lines);
                    }
                    else if (block.Child != null)
                    {
                        AddCode(CodeLine.Of($"goto L{block.Child.Index};"));
                    }
                    else
                    {
                        if (_code.Lines[^1].Code.StartsWith("return")) continue;
                        AddCode(CodeLine.Of("return;"));
                    }
                    
                    
                }

                _code.Add(CodeLine.Of("}", cfg.Function.ToString()));
                _code.Add(EmptyLine);
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
                output.Add(CodeLine.Of($"{VarSignature(node, temp.Representation)};", id.Token.Content, id.Token.SourceInfo.LineRange.Line));
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
                output.Add(code);
                output.Add(CodeLine.Of($"{temp.Representation} = {GetValue(vn)};", node.LValue.Id.Token.Content));
            }
            else
            {

                var (temp2, code2) = ComputeTemporary(node.Expression);

                // TODO: del old temp?

                output.Add(code);
                output.Add(code2);
                output.Add(CodeLine.Of($"{temp.Representation} = {temp2.Representation};", node.LValue.Id.Token.Content));
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
                argStrings.Add($"{(reference ? "&" : "")}{args[i].Representation}");

                if (args[i].PrimitiveType is PrimitiveType.Array)
                {
                    argStrings.Add($"sizeof({args[i].Representation})/sizeof(*{args[i].Representation})");
                }
            }

            var argString = string.Join(", ", argStrings);

            code.Add(CodeLine.Of($"{node.Id.Token.Content}({argString});"));
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
                var (t, code) = ComputeTemporary(arg);
                args.Add(t);
                printfTypes.Add(PrintfType(arg.Type.PrimitiveType));
                result.Add(code);
            }

            var printString = string.Join(" ", printfTypes);
            var argsString = string.Join(", ", args.Select(a => a.Representation));
            
            result.Add(CodeLine.Of($"printf(\"{printString}\\n\", {argsString});"));

            return result;
        }

        public CodeBlock ReadStatement(ReadStatementNode node)
        {
            var args = new List<IVariable>();
            var scanfTypes = new List<string>();
            var result = new CodeBlock();

            foreach (var arg in node.Variables)
            {
                var (t, code) = ComputeTemporary(arg);
                args.Add(t);
                scanfTypes.Add(PrintfType(arg.Type.PrimitiveType));
                result.Add(code);
            }

            var readString = string.Join(" ", scanfTypes);
            var argsString = string.Join(", ", args.Select(a => $"&{a.Representation}"));

            result.Add(CodeLine.Of($"scanf(\"{readString}\", {argsString});"));

            return result;
        }

        public CodeBlock AssertStatement(AssertStatementNode node)
        {
            return new CodeBlock();
        }

        public CodeBlock ReturnStatement(ReturnStatementNode node)
        {
            var (t, code) = ComputeTemporary(node.Expression);

            code.Add(CodeLine.Of($"return {t.Name};"));
            return code;
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

        private string Operator(OperatorType op) =>
            op switch
            {
                OperatorType.Add => "+",
                OperatorType.Sub => "-",
                OperatorType.Mul => "*",
                OperatorType.Div => "/",
                OperatorType.Mod => "%",
                OperatorType.And => "&&",
                OperatorType.Or => "||",
                OperatorType.Not => "!",
                OperatorType.Eq => "==",
                OperatorType.Neq => "!=",
                OperatorType.Lt => "<",
                OperatorType.Leq => "<=",
                OperatorType.Geq => ">=",
                OperatorType.Gt => ">",
                _ => throw new Exception($"invalid operator {op}")
            };
        
        public (IVariable, CodeBlock) ComputeTemporary(Node node)
        {
            switch (node)
            {
                case ValueOfNode vof: return ComputeTemporary(vof.LValue); // TODO
                case ValueNode n:
                {
                    var t = NextTemporary;
                    return (t, new CodeBlock(CodeLine.Of($"{VarSignature(n, t.Name)} = {GetValue(n)};"))); // TODO
                }
                case BinaryOpNode n:
                {
                    return CreateTemporaryFromExpression(n);
                }
                case UnaryOpNode n:
                {
                    var (expr, code) = ComputeTemporary(n.Expression);
                    var t = NextTemporary;

                    code.Add(CodeLine.Of(
                        $"{VarSignature(n, t.Representation)} = {Operator(n.Op)}{expr.Representation};"));
                    return (t, code);
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
                            var existing = (TemporaryVariable) GetTemporary(adn.LValue.Variable);
                            var (index, code) = ComputeTemporary(adn.Expression);

                            var t = new TemporaryArrayDereference
                            {
                                Name = existing.Name,
                                Scope = existing.Scope,
                                PrimitiveType = existing.SubType,
                                Index = index.Name
                            };
                            /*
                            var (res, code) = StoreOrLoad(adn);
                            code.Add(CodeLine.Of($"{t.Name} = {res.Name}"));*/
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

                        if (arg.Type.PrimitiveType is PrimitiveType.Array)
                        {
                            //var sizeT = NextTemporary;
                            //code.Add(CodeLine.Of($"size_of {sizeT.Representation} = sizeof({argTemp.Representation})/sizeof(*{argTemp.Representation});"));
                            args.Add(new TemporaryVariable
                            {
                                Name = $"_{argTemp.Representation}_size"
                            });
                        }
                    }

                    var argString = string.Join(", ", args.Select(a => a.Representation));

                    var t = NextTemporary;

                    code.Add(CodeLine.Of($"{VarSignature(n, t.Representation)} = {n.Id.Token.Content}({argString});"));
                    return (t, code); // TODO
                }
                case SizeNode n:
                {
                    //var (size, sizeCode) = StoreOrLoad(n);
                    var (t, code) = ComputeTemporary(n.LValue);
                    return (new TemporaryVariable
                    {
                        Name = $"_{t.Representation}_size"
                    }, code);
                }
            }

            throw new Exception($"not implemented {node}");
            return (null, new CodeBlock());
        }

        public (IVariable, CodeBlock) StoreOrLoad(Node node)
        {
            var output = new CodeBlock();
            // TODO: don't actually return value if not wrapped in valueof!
            if (node is ValueOfNode vof) return StoreOrLoad(vof.LValue);
            
            if (node is ArrayDereferenceNode adn)
            {
                var existing = (TemporaryVariable) GetTemporary(adn.LValue.Variable);
                var (index, code) = ComputeTemporary(adn.Expression);

                var t = new TemporaryArrayDereference
                {
                    Name = existing.Name,
                    Scope = existing.Scope,
                    PrimitiveType = existing.SubType,
                    Index = index.Name
                };
                output.Add(code); // TODO ?
                return (t, output);
            }

            if (node is ValueNode vn)
            {
                var t = NextTemporary;
                output.Add(CodeLine.Of($"{VarSignature(vn, t.Representation)} = {GetValue(vn)};"));
                return (t, output);
            }

            
            if (node is CallNode cn)
            {
                return ComputeTemporary(cn);
            }

            if (node is SizeNode sn)
            {
                var t = NextTemporary;
                var sizeVar = (TemporaryVariable) GetTemporary(sn.LValue.Variable);
                output.Add(CodeLine.Of($"int {t.Representation} = _{sizeVar.Representation}_size"));
                return (t, output);
            }

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

            var left = "";
            var right = "";
            var signature = "";

            switch (_node.Left)
            {
                // TODO: check return type from operator or expression type
                /*while (true)
            {*/
                case BinaryOpNode bopLeft:
                {
                    var (bopTemp, bopCode) = CreateTemporaryFromExpression(bopLeft);
                    output.Add(bopCode);
                    temp = bopTemp;
                    break;
                }
                case LValueNode _:
                {
                    var (_temp, code) = ComputeTemporary(_node.Left);
                    output.Add(code);
                    temp = _temp;
                    break;
                }
                case SizeNode sn:
                {
                    var (_temp, code) = ComputeTemporary(sn);
                    output.Add(code);
                    temp = _temp;
                    break;
                }
                case ValueNode vn:
                {
                    var t = NextTemporary;
                    output.Add(CodeLine.Of($"{VarSignature(vn, t.Representation)} = {GetValue(vn)};"));
                    temp = t;
                    break;
                }
            }

            switch (_node.Right)
            {
                case LValueNode lvn:
                {
                    var t = NextTemporary;
                    var (_temp, code) = ComputeTemporary(_node.Right);
                    output.Add(code);
                    output.Add(CodeLine.Of($"{VarSignature(lvn, t.Representation)} = {temp.Representation} {Operator(_node.Op)} {_temp.Representation};"));

                    return (t, output);
                }
                case ValueNode vn2:
                {
                    var t = NextTemporary;
                    output.Add(CodeLine.Of($"{VarSignature(vn2, t.Representation)} = {temp.Representation} {Operator(_node.Op)} {GetValue(vn2)};"));
                    return (t, output);
                }
                case SizeNode sn2:
                {
                    var t = NextTemporary;
                    var (_temp, code) = ComputeTemporary(sn2);
                    output.Add(code);
                    output.Add(CodeLine.Of($"{VarSignature(node, t.Representation)} = {temp.Representation} {Operator(_node.Op)} {_temp.Representation};"));
                    return (t, output);
                }
                case BinaryOpNode bop:
                    var (bopTemp, bopCode) = CreateTemporaryFromExpression(bop);
                    output.Add(bopCode);
                    output.Add(CodeLine.Of($"{temp.Representation} = {bopTemp.Representation} {Operator(_node.Op)} {temp.Representation};"));
                    temp = bopTemp;
                    break;
            }

            // TODO: unarynode and what have you

                prevTemp = temp;

/*                if (!(_node.Right is BinaryOpNode)) break;
            }*/

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
                    PrimitiveType.Boolean => "int",
                    PrimitiveType.Void => "void"
                };
                return $"{type} {name}";
            }

            if (node.Type is ArrayTypeNode at)
            {
                var size = -1;
                var type = _types[at.SubType];

                if (!(at.Size is NoOpNode) && !(at.Size is ValueNode))
                {
                    var (t, code) = ComputeTemporary(at.Size);
                    AddCode(code.Lines);
                    return $"{type} {name}[{t.Representation}]{(at.SubType == PrimitiveType.String ? "[255]" : "")}";
                    // evaluate the expression, output the temporary code and return the result temp
                }
                else if (at.Size is ValueNode vn)
                {
                    size = vn.Value;
                }
                
                return $"{type} {name}[{(size > 0 ? size.ToString() : "")}]{(at.SubType == PrimitiveType.String ? "[255]" : "")}";
            }

            throw new Exception($"invalid type, got {node.Type} from {node}");
        }

        private readonly Dictionary<PrimitiveType, string> _types = new Dictionary<PrimitiveType, string>
        {
            [PrimitiveType.Integer] = "int",
            [PrimitiveType.Real] = "float",
            [PrimitiveType.String] = "char *",
            [PrimitiveType.Boolean] = "int",
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
                temp.Reference = par.Reference;
                AddTemporary(par.Variable, temp);
                parameters.Add(VarSignature(par, temp.Representation));

                if (par.Type.PrimitiveType == PrimitiveType.Array)
                {
                    parameters.Add($"size_t _{temp.Representation}_size");
                }
            }
            
            var code = $"{VarSignature(variable.Node, name)} ({string.Join(", ", parameters)})";

            return code;
        }
    }
}