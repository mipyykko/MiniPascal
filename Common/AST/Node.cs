using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Common.Util;

namespace Common.AST
{
    public abstract class Node
    {
        public virtual string Name => "Node";
        
        public dynamic Value { get; set; }
        public abstract Token Token { get; set; }
        public Node Type { get; set; }
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
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);
    }

    public class ProgramNode : Node
    {
        public override string Name => "Program";

        public Node Id;
        public Node DeclarationBlock;
        public Node MainBlock;
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {Id} {string.Join(", ", DeclarationBlock)} {MainBlock}";
        }

        public override string AST(int depth = 0)
        {
            // TODO: add declarationblock
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Id.AST(depth + 1)}" +
                   $"{DeclarationBlock.AST(depth + 1)}" +
                   $"{MainBlock.AST(depth + 1)}{Spaces(depth)}]\n";
        }
    }
    public class StatementListNode : Node
    {
        public override string Name => "StatementList";

        public Node Left;
        public Node Right;
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

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

    public class ScopeStatementListNode : StatementListNode
    {
        public override string Name => "ScopeStatementListNode";
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);
    }

    public class DeclarationListNode : Node
    {
        public override string Name => "DeclarationList";

        public Node Left;
        public Node Right;
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

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

        public Node IndexExpression = new NoOpNode();
        public Node Expression;
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

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

        public List<Node> Arguments; // ExpressionNode
        public FunctionOrProcedureDeclarationNode Function;
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {Id}({string.Join(", ", Arguments)})";
        }

        public override string AST(int depth = 0)
        {
            var sb = new StringBuilder($"{Spaces(depth)}[{Name}\n{Id.AST(depth + 1)}");

            foreach (var a in Arguments)
            {
                sb.Append(a.AST(depth + 1));
            }

            sb.Append($"{Spaces(depth)}]\n");

            return sb.ToString();
        }
    }

    public class BinaryOpNode : Node
    {
        public override string Name => "BinaryOp";

        public Node Left;
        public Node Right;

        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {Left}{Token.Content}{Right}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Spaces(depth+1)}[{Token.Content}]\n" +
                   $"{Left.AST(depth + 1)}{Right.AST(depth + 1)}{Spaces(depth)}]\n";
        }
    }

    public class UnaryOpNode : Node
    {
        public override string Name => "UnaryOp";

        public Node Expression;

        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {Token.Content}{Expression}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Spaces(depth+1)}[{Token.Content}]\n" +
                   $"{Expression.AST(depth + 1)}{Spaces(depth)}]\n";
        }
    }

    public class ExpressionNode : Node
    {
        public override string Name => "Expression";

        public char Sign = '\0';
        public Node Expression;
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

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

        public Node Expression;
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {Expression}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Expression.AST(depth + 1)}{Spaces(depth)}]\n";
        }
    }

    public class IdentifierNode : Node
    {
        public override string Name => "Identifier";

        public Node IndexExpression = new NoOpNode();
        public IdentifierNode ReferenceNode;
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {Token.Content}{(IndexExpression is NoOpNode ? "" : $"[{IndexExpression}]")}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" +
                   $"{Spaces(depth+1)}[{Token.Content}" +
                   $"{(IndexExpression is NoOpNode ? "" : $"\n{IndexExpression.AST(depth + 2)}{Spaces(depth + 1)}")}]\n" +
                   (Type != null ? $"{Type.AST(depth + 1)}" : "") +
                   $"{Spaces(depth)}]\n";
        }
    }

    public class LiteralNode : Node
    {
        public override string Name => "Literal";
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {Token.Content}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n" + 
                   (Type != null ? $"{Type.AST(depth + 1)}" :  "") +
                   $"{Spaces(depth + 1)}[{Token.Content}]\n" +
                   $"{Spaces(depth)}]\n";
        }
    }

    public class IfNode : Node
    {
        public override string Name => "If";

        public Node Expression;
        public Node TrueBranch;
        public Node FalseBranch = new NoOpNode();
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

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

        public Node Expression;
        public Node Statement;
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);


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

        public List<Node> Ids; // IdentifierNode
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {string.Join(", ", Ids)}";
        }

        public override string AST(int depth = 0)
        {
            var sb = new StringBuilder($"{Spaces(depth)}[{Name}\n");

            foreach (var a in Ids)
            {
                sb.Append(a.AST(depth + 1));
            }

            sb.Append((Type != null ? $"{Type.AST(depth + 1)}" :  ""));
            sb.Append($"{Spaces(depth)}]\n");

            return sb.ToString();
        }
    }

    public abstract class FunctionOrProcedureDeclarationNode : Node
    {
        public Node Id;
        public Node Statement;
        public List<Node> Parameters; // ParameterNode
    }

    public class ProcedureDeclarationNode : FunctionOrProcedureDeclarationNode
    {
        public override string Name => "ProcedureDeclaration";
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {Id}({string.Join(", ", Parameters)}) {Statement}";
        }

        public override string AST(int depth = 0)
        {
            var sb = new StringBuilder($"{Spaces(depth)}[{Name}\n" +
                                       $"{Id.AST(depth + 1)}");
            
            foreach (var s in Parameters)
            {
                sb.Append(s.AST(depth + 1));
            }
            
            sb.Append($"{Statement.AST(depth + 1)}{Spaces(depth)}]\n");

            return sb.ToString();
        }
    }

    public class FunctionDeclarationNode : FunctionOrProcedureDeclarationNode
    {
        public override string Name => "FunctionDeclaration";

        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {Id}({string.Join(", ", Parameters)}): {Type} {Statement}";
        }

        public override string AST(int depth = 0)
        {
            var sb = new StringBuilder($"{Spaces(depth)}[{Name}\n" +
                                        (Type != null ? $"{Type.AST(depth + 1)}" :  "") +
                                        $"{Id.AST(depth + 1)}");
            
            foreach (var s in Parameters)
            {
                sb.Append(s.AST(depth + 1));
            }
            
            sb.Append($"{Statement.AST(depth + 1)}{Spaces(depth)}]\n");

            return sb.ToString();
        }
    }
    
    public class ParameterNode : Node
    {
        public override string Name => "Parameter";

        public Node Id;
        public bool Reference { get; set; }
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

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

        public PrimitiveType PrimitiveType;
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {(Token?.Content ?? "")}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name} {PrimitiveType}]\n";
        }
    }

    public class SimpleTypeNode : TypeNode
    {
        public override string Name => "SimpleType";

        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {(Token?.Content ?? "")}";
        }

        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n{Spaces(depth+1)}[{PrimitiveType}]\n{Spaces(depth)}]\n";
        }
    }

    public class ArrayTypeNode : TypeNode
    {
        public override string Name => "ArrayType";

        public Node Size;
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string ToString()
        {
            return $"{Name} {(Token?.Content ?? "")}[{Size}]";
        }
        public override string AST(int depth = 0)
        {
            return $"{Spaces(depth)}[{Name}\n{Spaces(depth+1)}[{PrimitiveType}]\n" +
                   $"{(Size is NoOpNode ? "" : Size.AST(depth + 1))}{Spaces(depth)}]\n";
        }
    }

    public class ReturnStatementNode : ExpressionNode
    {
        public override string Name => "Return";
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

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
        
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

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

        public List<IdentifierNode> Variables;

        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override string AST(int depth = 0)
        {
            var sb = new StringBuilder($"{Spaces(depth)}[{Name}\n");

            foreach (var a in Variables)
            {
                sb.Append(a.AST(depth + 1));
            }

            sb.Append($"{Spaces(depth)}]\n");

            return sb.ToString();
        }
    }

    public class WriteStatementNode : Node
    {
        public override string Name => "WriteStatement";

        public List<Node> Arguments; // ExpressionNode

        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);
        
        public override string AST(int depth = 0)
        {
            var sb = new StringBuilder($"{Spaces(depth)}[{Name}\n");

            foreach (var a in Arguments)
            {
                sb.Append(a.AST(depth + 1));
            }

            sb.Append($"{Spaces(depth)}]\n");

            return sb.ToString();
        }

    }
}