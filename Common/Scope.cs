using System;
using Common.AST;
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
        While,
        Program
    }

    public class Scope
    {
        public Scope Parent;
        public SymbolTable SymbolTable;
        public ScopeType ScopeType;
        public Function Function;
        public Node Node;

        public int Level = 0;

        public IVariable GetSymbol(string id)
        {
            var symbol = SymbolTable.GetSymbol(id);

            if (symbol == null)
            {
                if (Parent != null) return Parent.GetSymbol(id);

                throw new Exception($"{id} not found");
            }

            return symbol;
        }

        public string ScopePath()
        {
            return $"{(Parent != null ? Parent.ScopePath() : "")}_{ScopeType}";
        }
        
        public override string ToString()
        {
            return $"{(Parent != null ? $"->{Parent}" : "")}{ScopeType}";
        }
    }
}