using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.AST;
using Common.Symbols;

namespace ScopeAnalyze
{
    public abstract class AnalyzeVisitor : Visitor
    {
        protected AnalyzeVisitor(Scope scope)
        {
            Scopes.Push(scope);
        }

        private readonly Stack<Scope> Scopes = new Stack<Scope>();

        protected void CreateScope(ScopeType type)
        {
            var parent = Scopes.Any() ? CurrentScope : null;
            var scope = new Scope
            {
                Parent = parent,
                ScopeType = type,
                SymbolTable = new SymbolTable()
            };

            Console.WriteLine($"Entering {scope}");

            Scopes.Push(scope);
        }

        protected void EnterScope(Scope scope)
        {
            Scopes.Push(scope);
        }

        protected void ExitScope()
        {
            if (!Scopes.Any()) return;

            Console.WriteLine($"Exiting {CurrentScope}");
            Scopes.Pop();
        }

        protected Scope CurrentScope => Scopes.Peek();

        protected IVariable GetVariable(string id, Scope s = null)
        {
            if (s == null) s = CurrentScope;

            while (s != null)
            {
                var sym = s.SymbolTable.GetSymbol(id);
                if (sym is Variable) return sym;
                s = s.Parent;
            }

            return null;
        }

        protected bool CheckVariable(string id, Scope s = null)
        {
            return GetVariable(id, s) != null;
        }

        protected IVariable GetFunctionOrProcedure(string id, Scope s = null)
        {
            if (s == null) s = CurrentScope;

            while (s != null)
            {
                var sym = s.SymbolTable.GetSymbol(id);
                if (sym is Function) return sym;
                s = s.Parent;
            }

            return null;
        }

        protected bool CheckFunctionOrProcedure(string id, Scope s = null)
        {
            return GetFunctionOrProcedure(id, s) != null;
        }
    }
}