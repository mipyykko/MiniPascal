using System.Collections.Generic;
using Common.AST;

namespace Common.Symbols
{
    public abstract class IVariable
    {
        public string Name;
        public string ResolvedName => $"{Scope?.ScopePath()}_{Name}";
        public Scope Scope;
        public PrimitiveType PrimitiveType;
        public PrimitiveType SubType;
        public Node Node;

        public virtual string Representation => $"{Scope?.ScopePath()}_{Name}"; // Name
    }

    public class Variable : IVariable
    {
        public Node ReferenceNode;
        public int Size = -1;
        public int InternalSize = -1;

        public override string ToString()
        {
            return $"Variable {Name}: {(PrimitiveType == PrimitiveType.Array ? $"{PrimitiveType} of {SubType}" : PrimitiveType.ToString())}";
        }
    }
    
    public class Formal : Variable
    {
        
    }

    public class Literal : Variable
    {
        public ValueNode Value;

        public override string Representation => $"{Value.Value}";
    }
    
    public class TemporaryVariable : Variable
    {
        public bool Reference = false;
        public bool Dynamic;
        
        public override string Representation => $"{(Reference ? "*" : "")}{Name}";
    }

    public class TemporaryArrayDereference : TemporaryVariable
    {
        public string Index = "";

        public override string Representation => $"{Name}.data[{Index}]";
    }

    public class BuiltinVariable : Variable
    {
        public override string ToString()
        {
            return $"BuiltinVariable {Name}: {(PrimitiveType == PrimitiveType.Array ? $"{PrimitiveType} of {SubType}" : PrimitiveType.ToString())}";
        }
    }

    public abstract class FunctionVariable : IVariable
    {
        public List<Node> Parameters;
        
    }
    
    public class UserFunctionVariable : FunctionVariable
    {
        public override string ToString()
        {
            return $"UserFunctionVariable {Name}: {(PrimitiveType == PrimitiveType.Array ? $"{PrimitiveType} of {SubType}" : PrimitiveType.ToString())}";
        }
    }

    public class BuiltinFunctionVariable : FunctionVariable
    {
        public override string ToString()
        {
            return $"BuiltinFunctionVariable {Name}: {(PrimitiveType == PrimitiveType.Array ? $"{PrimitiveType} of {SubType}" : PrimitiveType.ToString())}";
        }
    }

}