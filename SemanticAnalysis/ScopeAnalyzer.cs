using System.Collections.Generic;
using Common;
using Common.AST;
using Common.Symbols;

namespace ScopeAnalyze
{
    public class ScopeAnalyzer
    {
        public static void Analyze(Node n)
        {
            var symbols = new SymbolTable();
            symbols.AddSymbol(new BuiltinFunctionVariable
            {
                Name = "$main$"
            });
            symbols.AddSymbol(new BuiltinFunctionVariable
            {
                Name = "writeln",
            });
            symbols.AddSymbol(new BuiltinFunctionVariable
            {
                Name = "read"
            });
            symbols.AddSymbol(new BuiltinVariable
            {
                Name = "true",
                PrimitiveType = PrimitiveType.Boolean,
                SubType = PrimitiveType.Void
            });
            symbols.AddSymbol(new BuiltinVariable
            {
                Name = "false",
                PrimitiveType = PrimitiveType.Boolean,
                SubType = PrimitiveType.Void
            });

            var scope = new Scope
            {
                ScopeType = ScopeType.Program,
                SymbolTable = symbols
            };

            var visitors = new Visitor[]
            {
                new ScopeVisitor(scope),
                new BuiltinVisitor(scope),
                new TypeVisitor(scope),
                new ExpressionVisitor(scope),
                new CfgVisitor(scope, new List<dynamic>())
            };

            foreach (var v in visitors) n.Accept(v);
        }
    }
}