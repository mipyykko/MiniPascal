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

    public class Generator
    {
        private static int _nextTemporary;

        private static readonly string AssertionMacro = @"#define _assert(e, l) do { \
  if (!(e)) { \
    printf(""assertion error on line %d\n"", l); \
    abort(); \
  } \
} while (0);

";
        private static readonly string ArrayMacros = @"#define INITIAL_ARRAY_CAPACITY 8
#define CAPACITY_GROW 1.5
#define _array(t, n) struct { t* data; unsigned int size; unsigned int internalSize; int dynamic; } _array_##n
#define _initArray(a, s) do { \
  unsigned int capacity = s; \
  if (s == 0) capacity = INITIAL_ARRAY_CAPACITY; \
  a.data = malloc(sizeof(*a.data) * capacity); \
  a.size = s; \
  a.internalSize = capacity; \
  a.dynamic = s == 0; \
} while (0);
#define _resize(a, i) do { \
  while (i >= a.internalSize) a.internalSize *= CAPACITY_GROW; \
  char* tmp = realloc(a.data, sizeof(*a.data) * a.internalSize); \
} while (0);
#define _indexError(a, i, l) do { \
  printf(""index error on line %d: %d when size is %d\n"", i, l, a.internalSize); \
  _destroy(a); \
  exit(1); \
} while (0);
#define _assign(a, i, d, l) do { \
  if (i < 0 || (!a.dynamic && i >= a.internalSize)) _indexError(a, i, l); \
  if (i >= a.internalSize) _resize(a, i); \
  if (a.dynamic && i >= a.size) a.size = i; \
  a.data[i] = d; \
} while (0);
#define _destroy(a) free(a.data);
#define _checkIndex(a, i, l) do { \
  if (i < 0 || i >= a.internalSize) _indexError(a, i, l); \
} while (0);
";

        private readonly Dictionary<PrimitiveType, string> _types = new Dictionary<PrimitiveType, string>
        {
            [PrimitiveType.Integer] = "int",
            [PrimitiveType.Real] = "float",
            [PrimitiveType.String] = "char *",
            [PrimitiveType.Boolean] = "int",
            [PrimitiveType.Void] = "void"
        };

        private readonly List<CodeBlock> _code = new List<CodeBlock>();
        private readonly Dictionary<Scope, List<string>> _freeable = new Dictionary<Scope, List<string>>();
        
        private HashSet<string> _includes = new HashSet<string>();
        private HashSet<string> _arrayDefinitions = new HashSet<string>();
        private HashSet<string> _forwardDeclarations = new HashSet<string>();
        
        private readonly Stack<Scope> _scopeStack = new Stack<Scope>();

        private readonly Dictionary<Scope, Dictionary<string, IVariable>> _temporaries =
            new Dictionary<Scope, Dictionary<string, IVariable>>();

        private readonly List<CFG> Cfgs;
        private bool HasArrays;
        private bool HasAssert;
        
        public Generator(List<CFG> cfgs)
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

        public string Generate()
        {
            foreach (var cfg in Cfgs)
            {
                var function = cfg.Function;
                _scopeStack.Push(function.Scope);
                _freeable[function.Scope] = new List<string>();
                var signature = FunctionSignature(function);
                if (!signature.Equals("main")) _forwardDeclarations.Add($"{signature};");
                
                var cfgCodeBlock = new CodeBlock();
                cfgCodeBlock.Add(CodeLine.Of($"{signature} {{", $"{cfg.Function}"));
                cfgCodeBlock.Indent();
                
                foreach (var block in cfg.Blocks)
                {
                    var blockCodeBlock = new CodeBlock
                    {
                        Depth = cfgCodeBlock.Depth
                    };
                    blockCodeBlock.Dedent();
                    blockCodeBlock.Add(CodeLine.Of($"L{block.Index}:;"));
                    blockCodeBlock.Indent();
                    
                    foreach (dynamic statement in block.Statements)
                    {
                        var c = CreateStatement(statement);
                        blockCodeBlock.Add(c);
                    }

                    if (block.Child is BranchBlock bb)
                    {
                        var expression = bb.Expression;
                        var (t, code) = ComputeTemporary(expression);

                        var ifBlock = new CodeBlock
                        {
                            Depth = blockCodeBlock.Depth
                        };
                        ifBlock.Add(code);
                        ifBlock.Add(CodeLine.Of($"if ({t.Name}) goto L{bb.TrueBlock.Index};"));
                        ifBlock.Add(CodeLine.Of($"goto L{bb.FalseBlock.Index};", "else/end while"));

                        blockCodeBlock.Add(ifBlock);
                    }
                    else if (block.Child != null)
                    {
                        if (block.Child.Index != block.Index + 1)
                        {
                            blockCodeBlock.Add(CodeLine.Of($"goto L{block.Child.Index};"));
                        }
                    }
                    else if (blockCodeBlock.Lines.Count > 0 && !blockCodeBlock.Lines[^1].Code.StartsWith("return"))
                    {
                        // TODO: default value, main returns 0?
                        blockCodeBlock.Add(CodeLine.Of("return;"));
                    }

                    /*if (blockCodeBlock.Lines.Count == 0 || blockCodeBlock.Lines[^1].Code.StartsWith("return"))
                        {
                            // BlockCodeBlock.Dedent();
                            _code.Add(blockCodeBlock);
                            continue;
                        }

                        blockCodeBlock.Add(CodeLine.Of("return;"));
                    }*/

                    cfgCodeBlock.Add(blockCodeBlock);
                }

                cfgCodeBlock.Dedent();
                cfgCodeBlock.Add(CodeLine.Of("}", cfg.Function.ToString()));
                _code.Add(cfgCodeBlock);
                _scopeStack.Pop();
            }

            var output = "";

            if (HasArrays) _includes.Add("stdlib.h");
            output += string.Join("\n", _includes.Select(s => $"#include <{s}>")) + "\n\n";

            if (HasArrays) output += ArrayMacros;
            if (HasAssert) output += AssertionMacro;
            output += string.Join("\n", _arrayDefinitions) + "\n\n";

            output += string.Join("\n", _forwardDeclarations) + "\n\n";
            
            output += string.Join("\n\n", _code);

            return output; //string.Join("\n", _code);
        }

        private CodeBlock GetFreeable()
        {
            var code = new CodeBlock();
            foreach (var t in _freeable[CurrentScope]) code.Add(CodeLine.Of($"_destroy({t});"));

            return code;
        }

        private string SanitizeType(string s) => s.Replace("*", "s");

        public CodeBlock ComputeArraySize(TemporaryVariable temp, VarDeclarationNode node)
        {
            var output = new CodeBlock();

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
            _arrayDefinitions.Add($"typedef _array({type}, {SanitizeType(type)});");
            output.Add(CodeLine.Of($"_array_{SanitizeType(type)} {temp.Representation};"));
            output.Add(CodeLine.Of($"_initArray({temp.Representation}, {sizeStr});"));
            AddFreeable(temp);

            return output;
        }

        public CodeBlock CreateStatement(VarDeclarationNode node)
        {
            var output = new CodeBlock();

            foreach (var id in node.Ids)
            {
                var temp = NextTemporary;
                var v = new Variable
                {
                    Name = id.Token.Content,
                    Scope = CurrentScope
                };
                
                AddTemporary(v, temp);

                if (node.Type is SimpleTypeNode)
                {
                    output.Add(CodeLine.Of($"{VarSignature(node, temp.Representation)};", id.Token.Content,
                        id.Token.SourceInfo.LineRange.Line));
                }
                else
                {
                    HasArrays = true;
                    var code = ComputeArraySize(temp, node);
                    output.Add(code);
                }
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

        public CodeBlock CreateStatement(AssignmentNode node)
        {
            var output = new CodeBlock();
            var (temp, code) = StoreOrLoad(node.LValue);

            if (node.Expression is ValueNode vn)
            {
                output.Add(code);
                output.Add(CodeLine.Of($"{temp.Representation} = {GetValue(vn)};", $"{node.LValue.Id.Token.Content} = {vn.Token.Content}"));
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

        public CodeBlock CreateStatement(CallNode node)
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

            code.Add(CodeLine.Of($"{node.Variable.Representation}({argString});"));
            return code; // TODO
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

        public CodeBlock CreateStatement(WriteStatementNode node)
        {
            _includes.Add("stdio.h");

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

        public CodeBlock CreateStatement(ReadStatementNode node)
        {
            _includes.Add("stdio.h");

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
                    $"_assign({a.Name}, {((TemporaryArrayDereference) a).Index}, {b.Representation}, {node.Token.Line});"));
            return result;
        }

        public CodeBlock CreateStatement(AssertStatementNode node)
        {
            HasAssert = true;
            
            var (t, code) = ComputeTemporary(node.Expression);

            code.Add(CodeLine.Of($"_assert({t.Representation}, {node.Expression.Token.Line});"));

            return code;
        }

        public CodeBlock CreateStatement(ReturnStatementNode node)
        {
            var code = GetFreeable();

            if (node.Expression is NoOpNode)
            {
                code.Add(CodeLine.Of("return;"));
                return code;
            }
            
            var (t, expressionCode) = ComputeTemporary(node.Expression);

            code.Add(expressionCode);
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
                            return StoreOrLoad(adn);
                            /*var existing = (TemporaryVariable) GetTemporary(adn.LValue.Variable);
                            var (index, code) = ComputeTemporary(adn.Expression);

                            var t = new TemporaryArrayDereference
                            {
                                Name = existing.Name,
                                Scope = existing.Scope,
                                PrimitiveType = existing.SubType,
                                Index = index.Representation
                            };
                            code.Add(CodeLine.Of($"_checkIndex({existing.Representation}, {t.Index}, {adn.Token.Line});"));
                            return (t, code);*/
                        }
                    }

                    break;
                }
                case CallNode n:
                {
                    var code = new CodeBlock();
                    var t = NextTemporary;

                    code.Add(CodeLine.Of($"{VarSignature(n, t.Representation)};"));

                    // TODO: find some more reasonable way than the lines[0] thing as it outputs a bit wrong
                    var callCode = CreateStatement(n);
                    code.Add(CodeLine.Of($"{t.Representation} = {callCode.Lines[0]}"));
                    /*var args = new List<IVariable>();

                    foreach (var arg in n.Arguments)
                    {
                        var (argTemp, argCode) = ComputeTemporary(arg);
                        code.Add(argCode);
                        args.Add(argTemp);

                    }

                    var argString = string.Join(", ", args.Select(a => a.Representation));

                    var t = NextTemporary;

                    code.Add(CodeLine.Of($"{VarSignature(n, t.Representation)} = {n.Variable.Representation}({argString});"));*/
                    return (t, code); // TODO
                }
                case SizeNode n:
                {
                    return StoreOrLoad(n);
                    /*var (arrTemp, code) = ComputeTemporary(n.LValue);

                    return (new TemporaryVariable
                    {
                        Name = SizeVariable(arrTemp)
                    }, code);*/
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
                output.Add(CodeLine.Of($"_checkIndex({existing.Representation}, {t.Index}, {adn.Token.Line});"));
                output.Add(code); // TODO ?
                return (t, output);
            }

            if (node is ValueNode vn)
            {
                // var t = NextTemporary;
                // output.Add(CodeLine.Of($"{VarSignature(vn, t.Representation)} = {GetValue(vn)};"));
                return (new Literal
                {
                    Value = vn
                }, output);
            }


            if (node is CallNode cn) return ComputeTemporary(cn);

            if (node is SizeNode sn)
            {
                var (t, code) = ComputeTemporary(sn.LValue);

                return (new TemporaryVariable
                {
                    Name = SizeVariable(t)
                }, code);
                /*var t = NextTemporary;
                var sizeVar = (TemporaryVariable) GetTemporary(sn.LValue.Variable);
                output.Add(CodeLine.Of($"int {t.Representation} = {sizeVar.Representation}.size"));
                return (t, output);*/
            }

            return ComputeTemporary(node);
            /*var variable = ((LValueNode) node).Variable;
            var tempVar = GetTemporary(variable);

            return (tempVar, output);*/
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

                    return $"_array_{SanitizeType(type)} {name}";

                    //$"_array({type}) {name}";
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
            if (variable.Name.Equals("$main$")) return "int main()";

            var name = variable.Representation; // Name;


            var parameters = new List<string>();
            var nodeParameters = ((FunctionDeclarationNode) variable.Node).Parameters;
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

        public static string SizeVariable(IVariable v) => $"{v.Representation}.size";
    }
}