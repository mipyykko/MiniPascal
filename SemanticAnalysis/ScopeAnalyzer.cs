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
            symbols.AddSymbol(new BuiltinFunction
            {
                Name = "writeln",
            });
            symbols.AddSymbol(new BuiltinFunction
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
                ScopeType = ScopeType.Main,
                SymbolTable = symbols
            };

            var v1 = new ScopeVisitor(scope);
            var v2 = new BuiltinVisitor(scope);
            var v3 = new TypeVisitor(scope);
            var v4 = new ExpressionVisitor(scope);

            // var v2 = new SecondVisitor();

            n.Accept(v1);
            n.Accept(v2);
            n.Accept(v3);
            n.Accept(v4);
        }
    }
}