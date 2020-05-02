using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Symbols;
using static Common.Util;

namespace Common.AST
{
    public abstract class Node
    {
        public virtual string Name => "Node";

        // public dynamic Value { get; set; }
        public Token Token { get; set; }
        public TypeNode Type { get; set; }
        public Scope Scope { get; set; }

        public override string ToString()
        {
            return $"{Name}";
        }

        public abstract dynamic Accept(Visitor visitor);

        public virtual string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}]\n";
        }
    }

    public class NoOpNode : Node
    {
        public override string Name => "NoOp";

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }
    }

    public class ProgramNode : Node
    {
        public override string Name => "Program";

        public Node Id { get; set; }
        public Node DeclarationBlock { get; set; }
        public Node MainBlock { get; set; }
        public Function Function { get; set; }
        
        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Id} {string.Join(", ", DeclarationBlock)} {MainBlock}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Id.AST(depth + 1)}" +
                   $"{DeclarationBlock.AST(depth + 1)}" +
                   $"{MainBlock.AST(depth + 1)}{Spaces(depth)}]\n";
        }
    }

    public abstract class BranchNode : Node
    {
        public Node Left { get; set; }
        public Node Right { get; set; }        
    }
    
    public class StatementListNode : BranchNode
    {
        public override string Name => "StatementList";

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Left} {Right}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Left.AST(depth + 1)}{Right.AST(depth + 1)}{Spaces(depth)}]\n";
        }
    }

    public class DeclarationListNode : BranchNode
    {
        public override string Name => "DeclarationList";

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Left} {Right}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Left.AST(depth + 1)}{Right.AST(depth + 1)}{Spaces(depth)}]\n";
        }
    }

    public abstract class IdNode : Node
    {
        public Node Id;
    }

    public class AssignmentNode : IdNode
    {
        public override string Name => "Assignment";

        public Node IndexExpression { get; set; } = new NoOpNode();
        public Node Expression { get; set; }

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Id}{(IndexExpression is NoOpNode ? "" : $"[{IndexExpression}]")} {Expression}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   (Type != null ? $"{Type.AST(depth + 1)}" : "") +
                   $"{Id.AST(depth + 1)}{Expression.AST(depth + 1)}{Spaces(depth)}]\n";
        }
    }

    public class CallNode : IdNode
    {
        public override string Name => "Call";

        public List<Node> Arguments { get; set; } // ExpressionNode
        public FunctionOrProcedureDeclarationNode Function { get; set; }

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Id}({string.Join(", ", Arguments)})";
        }

        public override string AST(int depth = 0)
        {
            var sb = new StringBuilder($"{Spaces(depth)}[{Name}\n" +
                                       $"{Id.AST(depth + 1)}" +
                                       (Type != null ? $"{Type.AST(depth + 1)}" : ""));

            foreach (var a in Arguments) sb.Append(a.AST(depth + 1));

            sb.Append($"{Spaces(depth)}]\n");

            return sb.ToString();
        }
    }

    public class VariableNode : IdNode
    {
        public override string Name => "Variable";

        public Node IndexExpression { get; set; } = new NoOpNode();
        public VariableNode ReferenceNode { get; set; }

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Id}{(IndexExpression is NoOpNode ? "" : $"[{IndexExpression}]")}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Id.AST(depth + 1)}" +
                   $"{IndexExpression.AST(depth + 1)}" +
                   //$"{(IndexExpression is NoOpNode ? "" : $"\n{IndexExpression.AST(depth + 2)}{Spaces(depth + 1)}")}" +
                   (Type != null ? $"{Type.AST(depth + 1)}" : "") +
                   $"{Spaces(depth)}]\n";
        }
    }

    public class BinaryOpNode : BranchNode
    {
        public override string Name => "BinaryOp";

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Left}{Token.Content}{Right}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name} {Type}\n" +
                   $"{Spaces(depth + 1)}[{Token.Content}]\n" +
                   $"{Left.AST(depth + 1)}{Right.AST(depth + 1)}{Spaces(depth)}]\n";
        }
    }

    public class UnaryOpNode : Node
    {
        public override string Name => "UnaryOp";

        public Node Expression { get; set; }

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Token.Content}{Expression}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Spaces(depth + 1)}[{Token.Content}]\n" +
                   $"{Expression.AST(depth + 1)}{Spaces(depth)}]\n";
        }
    }

    public class ExpressionNode : Node
    {
        public override string Name => "Expression";

        public char Sign = '\0';
        public Node Expression { get; set; }

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Sign}{Expression}";
        }

        public override string AST(int depth = 0)
        {
            return $"{(Sign != '\0' ? $"{Spaces(depth)}[{Sign}]\n" : "")}{Expression.AST(depth)}";
        }
    }

    public class SizeNode : Node
    {
        public override string Name => "Size";

        public VariableNode Variable { get; set; }

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Variable}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Variable.AST(depth + 1)}{Spaces(depth)}]\n";
        }
    }

    public class IdentifierNode : Node
    {
        public override string Name => "Identifier";

        /*public Node IndexExpression = new NoOpNode();
        public IdentifierNode ReferenceNode;*/

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Token.Content}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Spaces(depth + 1)}[{Token.Content}]\n" +
                   $"{Spaces(depth)}]\n";
        }
    }

    public class LiteralNode : Node
    {
        public override string Name => "Literal";

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Token.Content}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   (Type != null ? $"{Type.AST(depth + 1)}" : "") +
                   $"{Spaces(depth + 1)}[{Token.Content}]\n" +
                   $"{Spaces(depth)}]\n";
        }
    }

    public class IfNode : Node
    {
        public override string Name => "If";

        public Node Expression { get; set; }
        public Node TrueBranch { get; set; }
        public Node FalseBranch { get; set; } = new NoOpNode();

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Expression} {TrueBranch} {FalseBranch}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Expression.AST(depth + 1)}" +
                   $"{TrueBranch.AST(depth + 1)}" +
                   (FalseBranch is NoOpNode ? "" : $"{FalseBranch.AST(depth + 1)}") +
                   $"{Spaces(depth)}]\n";
        }
    }

    public class WhileNode : Node
    {
        public override string Name => "While";

        public Node Expression { get; set; }
        public Node Statement { get; set; }

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }


        public override string ToString()
        {
            return $"{Name} {Expression} {Statement}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Expression.AST(depth + 1)}" +
                   $"{Statement.AST(depth + 1)}" +
                   $"{Spaces(depth)}]\n";
        }
    }

    public class VarDeclarationNode : Node
    {
        public override string Name => "VarDeclaration";

        public List<Node> Ids { get; set; } // IdentifierNode

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {string.Join(", ", Ids)}";
        }

        public override string AST(int depth = 0)
        {
            var sb = new StringBuilder($"{Spaces(depth)}[{Name}\n");

            foreach (var a in Ids) sb.Append(a.AST(depth + 1));

            sb.Append(Type != null ? $"{Type.AST(depth + 1)}" : "");
            sb.Append($"{Spaces(depth)}]\n");

            return sb.ToString();
        }
    }

    public abstract class FunctionOrProcedureDeclarationNode : IdNode
    {
        public Node Statement { get; set; }
        public List<Node> Parameters { get; set; } // ParameterNode
        public Function Function { get; set; }
    }

    public class ProcedureDeclarationNode : FunctionOrProcedureDeclarationNode
    {
        public override string Name => "ProcedureDeclaration";

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Id}({string.Join(", ", Parameters)}) {Statement}";
        }

        public override string AST(int depth = 0)
        {
            var sb = new StringBuilder($"{Spaces(depth)}[{Name}\n" +
                                       $"{Id.AST(depth + 1)}");

            foreach (var s in Parameters) sb.Append(s.AST(depth + 1));

            sb.Append($"{Statement.AST(depth + 1)}{Spaces(depth)}]\n");

            return sb.ToString();
        }
    }

    public class FunctionDeclarationNode : FunctionOrProcedureDeclarationNode
    {
        public override string Name => "FunctionDeclaration";

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Id}({string.Join(", ", Parameters)}): {Type} {Statement}";
        }

        public override string AST(int depth = 0)
        {
            var sb = new StringBuilder($"{Spaces(depth)}[{Name}\n" +
                                       (Type != null ? $"{Type.AST(depth + 1)}" : "") +
                                       $"{Id.AST(depth + 1)}");

            foreach (var s in Parameters) sb.Append(s.AST(depth + 1));

            sb.Append($"{Statement.AST(depth + 1)}{Spaces(depth)}]\n");

            return sb.ToString();
        }
    }

    public class ParameterNode : Node
    {
        public override string Name => "Parameter";

        public Node Id { get; set; }
        public bool Reference { get; set; }

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {(Reference ? "var " : "")}{Id} {Type}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name} {(Reference ? "Ref" : "")}\n" +
                   $"{Id.AST(depth + 1)}" +
                   $"{Spaces(depth)}]\n";
        }
    }

    public abstract class TypeNode : Node
    {
        public override string Name => "Type";

        public PrimitiveType PrimitiveType { get; set; }

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Token?.Content ?? ""}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name} {PrimitiveType}]\n";
        }
    }

    public class SimpleTypeNode : TypeNode
    {
        public override string Name => "SimpleType";

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Token?.Content ?? ""} {PrimitiveType}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n{Spaces(depth + 1)}[{PrimitiveType}]\n{Spaces(depth)}]\n";
        }
    }

    public class ArrayTypeNode : TypeNode
    {
        public override string Name => "ArrayType";

        public PrimitiveType SubType { get; set; }
        public Node Size { get; set; } = new NoOpNode();

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Name} {Token?.Content ?? ""}[{(Size is NoOpNode ? "" : Size.ToString())}]";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n{Spaces(depth + 1)}[{SubType}]\n" +
                   $"{(Size is NoOpNode ? "" : Size.AST(depth + 1))}{Spaces(depth)}]\n";
        }
    }

    public class ReturnStatementNode : ExpressionNode
    {
        public override string Name => "Return";

        public Function Function { get; set; }
        
        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}" +
                   $"{(Expression is NoOpNode ? "" : $"\n{Expression.AST(depth + 1)}{Spaces(depth)}")}" +
                   "]\n";
        }
    }

    public class AssertStatementNode : ExpressionNode
    {
        public override string Name => "Assert";

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Expression.AST(depth + 1)}" +
                   $"{Spaces(depth)}]\n";
        }
    }

    public class ReadStatementNode : Node
    {
        public override string Name => "ReadStatement";

        public List<Node> Variables { get; set; }

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string AST(int depth = 0)
        {
            var sb = new StringBuilder($"{Spaces(depth)}[{Name}\n");

            foreach (var a in Variables) sb.Append(a.AST(depth + 1));

            sb.Append($"{Spaces(depth)}]\n");

            return sb.ToString();
        }
    }

    public class WriteStatementNode : Node
    {
        public override string Name => "WriteStatement";

        public List<Node> Arguments { get; set; } // ExpressionNode

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string AST(int depth = 0)
        {
            var sb = new StringBuilder($"{Spaces(depth)}[{Name}\n");

            foreach (var a in Arguments) sb.Append(a.AST(depth + 1));

            sb.Append($"{Spaces(depth)}]\n");

            return sb.ToString();
        }
    }

    public class ErrorNode : Node
    {
        public override string Name => "Error";

        public override dynamic Accept(Visitor visitor)
        {
            return visitor.Visit(this);
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}]\n";
        }
    }
}