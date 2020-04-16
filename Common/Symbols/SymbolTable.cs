using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using Common.AST;

namespace Common.Symbols
{
    public abstract class IVariable
    {
        public string Name;
        public PrimitiveType PrimitiveType;
        public PrimitiveType SubType;
        public Node Node;
    }

    public class Variable : IVariable
    {
        public Node ReferenceNode;
        public int Size = -1; 
    }

    public class UserFunction : IVariable
    {
        public List<Node> Parameters;
    }

    public class BuiltinFunction : IVariable
    {
        public List<Node> Parameters;
    }
    
    public enum SymbolType
    {
        Unknown,
        Variable,
        Reference,
        UserFunction,
        BuiltinFunction
    }
    
    public class Symbol
    {
        public string Name;
        public SymbolType SymbolType;
        public PrimitiveType PrimitiveType;
        public int Size;
        public (Scope, string) Reference;

        public static Symbol Of(string name, SymbolType symbolType, PrimitiveType primitiveType, int size, (Scope, string) reference) =>
            new Symbol
            {
                Name = name,
                SymbolType = symbolType,
                PrimitiveType = primitiveType,
                Size = size,
                Reference = reference
            };

        public static Symbol Of(string name, SymbolType symbolType, PrimitiveType primitiveType) =>
            Symbol.Of(name, symbolType, primitiveType, -1, (null, ""));
        
        public static Symbol Of(string name, SymbolType symbolType, PrimitiveType primitiveType, int size) => 
            Symbol.Of(name, symbolType, primitiveType, size, (null, ""));
        
        public static Symbol Of(string name, SymbolType symbolType, PrimitiveType primitiveType, (Scope, string) reference) => 
            Symbol.Of(name, symbolType, primitiveType, -1, reference);

        public override string ToString()
        {
            return $"{Name}: {SymbolType}";
        }
    }

    public class SymbolTable
    {
        private readonly Dictionary<string, IVariable> _symbols = new Dictionary<string,IVariable>();

        public bool AddSymbol(IVariable variable)
        {
            if (_symbols.ContainsKey(variable.Name))
            {
                return false;
            }
            _symbols[variable.Name.ToLower()] = variable;

            return true;
        }

        public IVariable GetSymbol(string id)
        {
            var _id = id.ToLower();

            if (!_symbols.ContainsKey(_id))
            {
                return null;
            }
            
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
            var variable = _symbols[id];

            if (!(variable is Variable))
            {
                throw new Exception($"can't add reference to variable {id}");
            }

            ((Variable) variable).ReferenceNode = node;
        }
        
        public override string ToString()
        {
            return string.Join("\n", _symbols.Values);
        }
    }
}