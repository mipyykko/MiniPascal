using System;
using System.Collections.Generic;
using Common;
using Common.AST;
using Common.Symbols;

namespace ScopeAnalyze
{
    public class SemanticAnalyzer
    {
        public static List<CFG> Analyze(Node n)
        {
            var symbols = new SymbolTable();
            var scope = new Scope
            {
                ScopeType = ScopeType.Program,
                SymbolTable = symbols
            };

            symbols.AddSymbol(new BuiltinFunctionVariable
            {
                Name = "$main$",
                Scope = scope
            });
            symbols.AddSymbol(new BuiltinFunctionVariable
            {
                Name = "writeln",
                Scope = scope
            });
            symbols.AddSymbol(new BuiltinFunctionVariable
            {
                Name = "read",
                Scope = scope
            });
            symbols.AddSymbol(new BuiltinVariable
            {
                Name = "true",
                PrimitiveType = PrimitiveType.Boolean,
                SubType = PrimitiveType.Void,
                Scope = scope
            });
            symbols.AddSymbol(new BuiltinVariable
            {
                Name = "false",
                PrimitiveType = PrimitiveType.Boolean,
                SubType = PrimitiveType.Void,
                Scope = scope
            });


            var visitors = new Visitor[]
            {
                new ScopeVisitor(scope),
                new BuiltinVisitor(scope),
                new TypeVisitor(scope),
                new ExpressionVisitor(scope),
            };

            foreach (var v in visitors) {n.Accept(v);}

            var cfgVisitor = new CfgVisitor(scope, new List<CFG>());
            var cfg = n.Accept(cfgVisitor);

            Console.WriteLine(string.Join("\n", cfg));
            return cfg;
        }
    }
}