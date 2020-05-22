using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using Common.AST;

namespace Common.Symbols
{

    public class SymbolTable
    {
        private readonly Dictionary<string, IVariable> _symbols = new Dictionary<string, IVariable>();

        public bool AddSymbol(IVariable variable)
        {
            var name = variable.Name.ToLower();

            if (_symbols.ContainsKey(name) &&
                !(_symbols[name] is BuiltinFunctionVariable) &&
                !(_symbols[name] is BuiltinVariable))
                return false;
            _symbols[variable.Name.ToLower()] = variable;

            return true;
        }

        public bool UpdateSymbol(IVariable variable)
        {
            _symbols[variable.Name.ToLower()] = variable;

            return true;
        }

        public IVariable GetSymbol(string id)
        {
            var _id = id.ToLower();

            if (!_symbols.ContainsKey(_id)) return null;

            var variable = _symbols[_id];

            if (variable is Variable v && v.ReferenceNode != null)
            {
                var refNode = (IdentifierNode) v.ReferenceNode;

                return refNode.Scope.SymbolTable.GetSymbol(refNode.Token.Content);
            }

            return _symbols[_id];
        }

        public void AddReference(string id, Node node)
        {
            var _id = id.ToLower();
            var variable = _symbols[_id];

            if (!(variable is Variable)) throw new Exception($"can't add reference to variable {id}");

            ((Variable) variable).ReferenceNode = node;
        }

        public override string ToString()
        {
            return string.Join("\n", _symbols.Keys.Select(k => $"{k}: {_symbols[k]}"));
        }
    }
}