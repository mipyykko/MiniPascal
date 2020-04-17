using Common;
using Common.AST;
using Common.Symbols;

namespace ScopeAnalyze
{
    public class ScopeAnalyzer
    {
        public void Analyze(Node n)
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
            
            var scope = new Scope
            {
                ScopeType = ScopeType.Main,
                SymbolTable =  symbols
            };
            
            // first run: allocate scopes and put variables and functions in correct scopes
            var v1 = new ScopeVisitor(scope);
            var v2 = new TypeVisitor(scope);
            
            // var v2 = new SecondVisitor();

            n.Accept(v1);
            n.Accept(v2);
        } 
    }
}