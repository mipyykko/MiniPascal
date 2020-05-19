using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using Common.AST;
using Common.Symbols;
using ScopeAnalyze;
using static Common.Util;

namespace CodeGeneration
{
    public class CodeBlock
    {
        public static int MaxLineLength;
        public int Depth;

        public List<CodeLine> Lines = new List<CodeLine>();

        public CodeBlock()
        {
        }

        public CodeBlock(CodeLine c)
        {
            Add(c);
        }

        public CodeBlock(IEnumerable<CodeLine> c)
        {
            Add(c);
        }

        public CodeBlock(CodeBlock c)
        {
            Add(c);
        }

        public void Add(CodeLine c)
        {
            MaxLineLength = Math.Max(MaxLineLength, c.Code.Length);
            c.Depth = Depth;
            Lines.Add(c);
        }

        public void Add(IEnumerable<CodeLine> c)
        {
            foreach (var l in c) Add(l);
        }

        public void Add(CodeBlock c)
        {
            Add(c.Lines);
        }

        public void Indent()
        {
            Depth += 2;
        }

        public void Dedent()
        {
            Depth -= 2;
        }

        public override string ToString()
        {
            return string.Join("\n", Lines.Select(l => l.GetString(MaxLineLength)));
        }
    }

    public class CodeLine
    {
        public string Code;
        public string Comment;
        public int Depth;
        public int Line;

        private CodeLine(string code, string comment = "", int line = -1)
        {
            Line = line;
            Code = code;
            Comment = comment;
        }

        public static CodeLine Of(string code, string comment = "", int line = -1)
        {
            return new CodeLine(code, comment, line);
        }

        public string GetString(int lineLength = 0)
        {
            var sb = new StringBuilder($"{Spaces(Depth)}{Code}".PadRight(lineLength));
            if (Line >= 0 || Comment.Length > 0) sb.Append($" // {(Line >= 0 ? $"{Line}: " : "")}{Comment}");

            return sb.ToString();
        }

        public override string ToString()
        {
            return $"{Code} # {(Line >= 0 ? $"{Line}: " : "")}{Comment}";
        }
    }

    public class Generator2
    {
        private static int _nextTemporary;

        private static readonly string ArrayMacros = @"#define INITIAL_ARRAY_CAPACITY 8
#define CAPACITY_GROW 1.5
#define _array(t) struct { t* data; unsigned int size; unsigned int internalSize; int dynamic; }
#define _initArray(a, s) do { \
  unsigned int capacity = s;
  if (s == 0) capacity = INITIAL_ARRAY_CAPACITY; \
  a.data = malloc(sizeof(*a.data) * capacity); \
  a.size = s; \
  a.internalSize = capacity; \
  a.dynamic = s == 0; \
} while (0);
#define _resize(a, i) do { \
  while (i >= a.internalSize) a.internalSize *= CAPACITY_GROW; \
  realloc(a.data, sizeof(*a.data) * a.internalSize); \
} while (0);
#define _assign(a, i, d) do { \
  if (i < 0 || (!a.dynamic && i >= a.internalSize)) { \
    printf(""index error: %d when size is %a\n"", i, a.internalSize); \
    _destroy(a); \
    exit(1); \
  }} \
  if (i >= a.internalSize) _resize(a, i);
  if (a.dynamic && i >= a.size) a.size = i; \
  a.data[i] = d; \
} while (0);
#define _destroy(a) free(a.data);
#define _checkIndex(
int _checkIndex(a, i) {
  if (i < 0 || i >= a.internalSize) return -1;
  return 0;
}
";

        private readonly Dictionary<PrimitiveType, string> _types = new Dictionary<PrimitiveType, string>
        {
            [PrimitiveType.Integer] = "int",
            [PrimitiveType.Real] = "float",
            [PrimitiveType.String] = "char *",
            [PrimitiveType.Boolean] = "int",
            [PrimitiveType.Void] = "void"
        };

        private readonly CodeBlock _code = new CodeBlock();
        private readonly Dictionary<Scope, List<string>> _freeable = new Dictionary<Scope, List<string>>();
        private HashSet<string> _helperFunctions = new HashSet<string>();
        private readonly Stack<Scope> _scopeStack = new Stack<Scope>();

        private readonly Dictionary<Scope, Dictionary<string, IVariable>> _temporaries =
            new Dictionary<Scope, Dictionary<string, IVariable>>();

        private readonly List<CFG> Cfgs;
        private bool HasArrays;

        public Generator2(List<CFG> cfgs)
        {
            Cfgs = cfgs;
        }

        private Scope CurrentScope => _scopeStack.Count > 0 ? _scopeStack.Peek() : null;

        public TemporaryVariable NextTemporary => new TemporaryVariable
        {
            Name = $"v{_nextTemporary++}",
            Scope = CurrentScope
        };

        public static CodeLine EmptyLine => CodeLine.Of("");

        public void AddCode(CodeLine c)
        {
            _code.Add(c);
        }

        public void AddCode(IEnumerable<CodeLine> c)
        {
            _code.Add(c);
        }

        public string Generate()
        {
            foreach (var cfg in Cfgs)
            {
                var function = cfg.Function;
                _scopeStack.Push(function.Scope);
                _freeable[function.Scope] = new List<string>();
                var signature = FunctionSignature(function);
                var body = new List<string>();

                _code.Add(CodeLine.Of($"{signature} {{", $"{cfg.Function}"));

                foreach (var block in cfg.Blocks)
                {
                    _code.Indent();
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

                        var ifBlock = new CodeBlock
                        {
                            Depth = _code.Depth
                        };
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
                        foreach (var t in _freeable[CurrentScope]) AddCode(CodeLine.Of($"_destroy({t});"));

                        if (_code.Lines[^1].Code.StartsWith("return"))
                        {
                            _code.Dedent();
                            continue;
                        }

                        AddCode(CodeLine.Of("return;"));
                    }

                    _code.Dedent();
                }

                _code.Add(CodeLine.Of("}", cfg.Function.ToString()));
                _code.Add(EmptyLine);
                _scopeStack.Pop();
            }

            var output = "";

            if (HasArrays) output += ArrayMacros;

            output += _code.ToString();

            return output; //string.Join("\n", _code);
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

        public CodeBlock ComputeArraySize(Node idNode, VarDeclarationNode node)
        {
            var output = new CodeBlock();
            var temp = NextTemporary;
            var v = new Variable
            {
                Name = idNode.Token.Content,
                Scope = CurrentScope
            };
            AddTemporary(v, temp);

            var at = (ArrayTypeNode) node.Type;

            var size = -1;
            var type = _types[at.SubType];
            var sizeStr = "";

            switch (at.Size)
            {
                case NoOpNode _:
                    temp.InternalSize = Constants.INITIAL_ARRAY_SIZE;
                    temp.Dynamic = true;
                    temp.Size = 0;
                    sizeStr = $"{temp.InternalSize}";
                    break;
                case ValueNode vn:
                    temp.Size = vn.Value;
                    temp.InternalSize = size;
                    sizeStr = $"{temp.Size}";
                    break;
                default:
                {
                    var (t, code) = ComputeTemporary(at.Size);
                    output.Add(code);
                    sizeStr = t.Representation; // SizeVariable(temp);
                    break;
                }
            }
            // TODO: skip this if value

            output.Add(CodeLine.Of($"_array({type}) {temp.Representation};"));
            output.Add(CodeLine.Of($"_initArray({temp.Representation}, {temp.Size});"));
            AddFreeable(temp);

            return output;
        }

        public CodeBlock VarDeclarationStatement(VarDeclarationNode node)
        {
            var output = new CodeBlock();

            foreach (var id in node.Ids)
                if (node.Type is SimpleTypeNode)
                {
                    var temp = NextTemporary;
                    var v = new Variable
                    {
                        Name = id.Token.Content,
                        Scope = CurrentScope
                    };
                    AddTemporary(v, temp);
                    output.Add(CodeLine.Of($"{VarSignature(node, temp.Representation)};", id.Token.Content,
                        id.Token.SourceInfo.LineRange.Line));
                }
                else
                {
                    HasArrays = true;
                    var type = ((ArrayTypeNode) node.Type).SubType;
                    var code = ComputeArraySize(id, node);
                    output.Add(code);
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
                output.Add(
                    CodeLine.Of($"{temp.Representation} = {temp2.Representation};", node.LValue.Id.Token.Content));
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

                /*if (args[i].PrimitiveType is PrimitiveType.Array)
                {
                    argStrings.Add($"sizeof({args[i].Representation})/sizeof(*{args[i].Representation})");
                }*/
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

        private string PrintfType(PrimitiveType type)
        {
            return type switch
            {
                PrimitiveType.Integer => "%d",
                PrimitiveType.Real => "%g",
                PrimitiveType.String => "%s",
                PrimitiveType.Boolean => "%d"
            };
        }

        public CodeBlock WriteStatement(WriteStatementNode node)
        {
            var args = new List<String>();
            var printfTypes = new List<string>();
            var result = new CodeBlock();

            var printString = new StringBuilder();

            foreach (var arg in node.Arguments)
            {
                if (arg is ValueNode vn)
                {
                    if (vn.Type.PrimitiveType == PrimitiveType.String)
                    {
                        printString.Append(vn.Value);
                        continue;
                    }
                    args.Add(GetValue(vn));
                }
                else
                {
                    var (t, code) = ComputeTemporary(arg);
                    args.Add(t.Representation);
                    result.Add(code);
                }

                printfTypes.Add(PrintfType(arg.Type.PrimitiveType));
            }

            printString.Append(string.Join(" ", printfTypes));
            var argsString = string.Join(", ", args);

            result.Add(CodeLine.Of($"printf(\"{printString}\\n\", {argsString});"));

            return result;
        }

        public CodeBlock ReadStatement(ReadStatementNode node)
        {
            var args = new List<IVariable>();
            var scanfTypes = new List<string>();
            var result = new CodeBlock();

            /* TODO: check if argument is arraydereference - if it is,
             * create temporary, pass that temporary as argument
             * and after scanf _{type}Assign it to array
             */
            var dereferences = new List<(IVariable, IVariable)>();

            foreach (var arg in node.Variables)
            {
                var (t, code) = ComputeTemporary(arg);
                if (t is TemporaryArrayDereference)
                {
                    var t2 = NextTemporary;
                    result.Add(CodeLine.Of($"{VarSignature(arg, t2.Representation)};"));
                    dereferences.Add((t, t2));
                    args.Add(t2);
                }
                else
                {
                    args.Add(t);
                }

                scanfTypes.Add(PrintfType(arg.Type.PrimitiveType));
                result.Add(code);
            }

            var readString = string.Join(" ", scanfTypes);
            var argsString = string.Join(", ", args.Select(a => $"&{a.Representation}"));

            result.Add(CodeLine.Of($"scanf(\"{readString}\", {argsString});"));

            foreach (var (a, b) in dereferences)
                result.Add(CodeLine.Of(
                    $"_assign({a.Name}, {((TemporaryArrayDereference) a).Index}, {b.Representation});"));
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
                _temporaries[variable.Scope] = new Dictionary<string, IVariable>();

            _temporaries[variable.Scope][variable.Name] = temporary;
        }

        public void AddFreeable(IVariable temporary)
        {
            if (!_freeable.ContainsKey(temporary.Scope)) _freeable[temporary.Scope] = new List<string>();

            _freeable[temporary.Scope].Add(temporary.Name);
        }

        public IVariable GetTemporary(IVariable variable)
        {
            if (!_temporaries.ContainsKey(CurrentScope)) return null;

            return _temporaries[CurrentScope].TryGetValueOrDefault(variable.Name);
        }

        private string Operator(OperatorType op)
        {
            return op switch
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
        }

        public (IVariable, CodeBlock) ComputeTemporary(Node node)
        {
            switch (node)
            {
                case ValueOfNode vof: return ComputeTemporary(vof.LValue); // TODO
                case ValueNode n:
                {
                    return (new Literal
                    {
                        Value = n
                    }, new CodeBlock());
                    //var t = NextTemporary;
                    //return (t, new CodeBlock(CodeLine.Of($"{VarSignature(n, t.Name)} = {GetValue(n)};"))); // TODO
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
                            if (existing != null) return (existing, new CodeBlock());

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
                                Index = index.Representation
                            };
                            code.Add(CodeLine.Of($"_checkIndex({existing.Representation}, {t.Index});"));
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

                        /*if (arg.Type.PrimitiveType is PrimitiveType.Array)
                        {
                            args.Add(new TemporaryVariable
                            {
                                Name = SizeVariable(argTemp)
                            });
                        }*/
                    }

                    var argString = string.Join(", ", args.Select(a => a.Representation));

                    var t = NextTemporary;

                    code.Add(CodeLine.Of($"{VarSignature(n, t.Representation)} = {n.Id.Token.Content}({argString});"));
                    return (t, code); // TODO
                }
                case SizeNode n:
                {
                    var (arrTemp, code) = ComputeTemporary(n.LValue);

                    return (new TemporaryVariable
                    {
                        Name = SizeVariable(arrTemp)
                    }, code);
                }
                case NoOpNode n:
                {
                    return (null, new CodeBlock());
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
                    Index = index.Representation
                };
                output.Add(CodeLine.Of($"_checkIndex({existing.Representation}, {t.Index});"));
                output.Add(code); // TODO ?
                return (t, output);
            }

            if (node is ValueNode vn)
            {
                var t = NextTemporary;
                output.Add(CodeLine.Of($"{VarSignature(vn, t.Representation)} = {GetValue(vn)};"));
                return (t, output);
            }


            if (node is CallNode cn) return ComputeTemporary(cn);

            if (node is SizeNode sn)
            {
                var t = NextTemporary;
                var sizeVar = (TemporaryVariable) GetTemporary(sn.LValue.Variable);
                output.Add(CodeLine.Of($"int {t.Representation} = {sizeVar.Representation}.size"));
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
            var output = new CodeBlock();

            var (left, leftCode) = ComputeTemporary(node.Left);
            output.Add(leftCode);
            var (right, rightCode) = ComputeTemporary(node.Right);
            output.Add(rightCode);

            var t = NextTemporary;
            output.Add(CodeLine.Of(
                $"{VarSignature(node, t.Representation)} = {left.Representation} {Operator(node.Op)} {right.Representation};"));

            return (t, output);
        }

        public string VarSignature(Node node, string name)
        {
            switch (node.Type)
            {
                case SimpleTypeNode st:
                {
                    var type = _types[st.PrimitiveType];
                    return $"{type} {name}";
                }
                case ArrayTypeNode at:
                {
                    var size = -1;
                    var type = _types[at.SubType];

                    return $"_array({type}) {name}";
                    //return $"{type} *{name}";
/*                if (!(at.Size is NoOpNode) && !(at.Size is ValueNode))
                {
                    var (t, code) = ComputeTemporary(at.Size);
                    return $"{type} {name}[{t.Representation}]{(at.SubType == PrimitiveType.String ? "[255]" : "")}";
                    // evaluate the expression, output the temporary code and return the result temp
                }
                if (at.Size is ValueNode vn)
                {
                    size = vn.Value;
                }
                
                return $"{type} {name}[{(size > 0 ? size.ToString() : "")}]{(at.SubType == PrimitiveType.String ? "[255]" : "")}";*/
                }
                default:
                    throw new Exception($"invalid type, got {node.Type} from {node}");
            }
        }

        public string FunctionSignature(Function function)
        {
            var variable = function.Variable;
            var name = variable.Name;

            if (name.Equals("$main$")) return "int main()";

            var parameters = new List<string>();
            var nodeParameters = ((FunctionOrProcedureDeclarationNode) variable.Node).Parameters;
            for (var i = 0; i < nodeParameters.Count; i++)
            {
                var par = (ParameterNode) nodeParameters[i];
                var temp = NextTemporary;
                temp.Reference = par.Reference;
                AddTemporary(par.Variable, temp);
                parameters.Add(VarSignature(par, temp.Representation));

                /*if (par.Type.PrimitiveType == PrimitiveType.Array)
                {
                    parameters.Add($"size_t {SizeVariable(temp)}");
                }*/
            }

            var code = $"{VarSignature(variable.Node, name)} ({string.Join(", ", parameters)})";

            return code;
        }

        public string SizeVariable(IVariable v)
        {
            return $"{v.Representation}.size";
        }

        private string TypeStr(PrimitiveType type)
        {
            return type switch
            {
                PrimitiveType.Boolean => "int ",
                PrimitiveType.Integer => "int ",
                PrimitiveType.Real => "float ",
                PrimitiveType.String => "char *",
                _ => throw new Exception()
            };
        }
    }
}