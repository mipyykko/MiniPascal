using Common.Symbols;

namespace Common
{
    public enum ScopeType
    {
        Unknown,
        Main,
        Function,
        Procedure,
        IfThen,
        IfElse,
        While
    }
    
    public class Scope
    {
        public Scope Parent;
        public SymbolTable SymbolTable;
        public ScopeType ScopeType;

        public int Level = 0;

        public override string ToString()
        {
            return $"{ScopeType} {(Parent != null ? Parent.ToString() : "")} -> {SymbolTable}";
        }
    }
}