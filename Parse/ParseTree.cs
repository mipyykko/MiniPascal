using System;
using System.Collections.Generic;
using System.Linq;
using AST;
using Common;
using Scan;

namespace Parse
{
    public static class ParseTree
    {
        static Scanner Scanner => Context.Scanner;

        public static Node Program(dynamic[] p)
        {
            return new ProgramNode
            {
                //Token = p[0],
                Id = new IdentifierNode
                {
                    Type = new SimpleTypeNode
                    {
                        PrimitiveType = PrimitiveType.String
                    },
                    Token = p[0]
                },
                Block = p[1]
            };
        }

        public static Node BlockStatement(dynamic[] p)
        {
            return new StatementListNode
            {
                Left = p[0],
                Right = p[1]
            };
        }

        public static Node AssignOrCallStatement(dynamic[] p)
        {
            Node id = p[0];
            IdNode n = p[1];

            n.Id = id;

            return n;
        }

        public static Node CallOrVariable(dynamic[] p)
        {
            Node id = p[0];
            IdNode n = p[1];

            n.Id = id;

            return n;
        }
        
        public static Node AssignmentStatement(dynamic[] p)
        {
            return new AssignmentNode
            {
                Expression = p[0]
            };
        }

        public static Node CallStatement(dynamic[] p)
        {
            return new CallNode
            {
                Arguments = p[0]
            };
        }

        public static Node VarDeclaration(dynamic[] p)
        {
            var ids = (List<IdentifierNode>) p[0];
            var type = p[1];
            
            foreach (var id in ids)
            {
                id.Type = type;
            }
            
            return new VarDeclarationNode
            {
                Ids = ids
            };
        }

        public static List<IdentifierNode> Ids(dynamic[] p)
        {
            var nodeList = new List<IdentifierNode>();
            nodeList.Add(p[0]);
                
            if (p.Length > 1 && p[1] is List<IdentifierNode> ids)
            {
                ids.ForEach(id => nodeList.Add(id));
            }

            return nodeList;
        }

        public static Node SimpleType(dynamic[] p)
        {
            return new SimpleTypeNode
            {
                Token = p[0]
            };
        }

        static Node NoOpStatement => new NoOpNode();

        public static Node ArrayType(dynamic[] p)
        {
            var typeCont = p[0];
            var type = typeCont[0] is SimpleTypeNode ? typeCont[0] : typeCont[1];
            var size = typeCont[0] is SimpleTypeNode ? NoOpStatement : typeCont[0];
            
            return new ArrayTypeNode
            {
                Type = type,
                Size = size
            };
        }

        public static Node Expr(dynamic[] p)
        {
            if (p.Length < 2) return p[0];

            return new BinaryOpNode
            {
                Left = p[0],
                Token = p[1], // TODO: op?
                Right = p[2]
            };
        }

        public static Node SignTerm(dynamic[] p)
        {
            if (p.Length < 2) return p[0];

            return new ExpressionNode
            {
                Sign = ((Token) p[0]).Content[0],
                Expression = p[1]
            };
        }

        public static Node SimpleExprOrTerm(dynamic[] p)
        {
            var term = p[0];

            if (p.Length < 2)
            {
                return term;
            }

            return new BinaryOpNode
            {
                Left = term,
                Token = p[1],
                Right = p[2]
            };
        }

        public static Node FactorOptSize(dynamic[] p)
        {
            if (p.Length < 2) return p[0];

            return new SizeNode
            {
                Expression = p[0]
            };
        }

        public static Node Unary(dynamic[] p)
        {
            return new UnaryOpNode
            {
                Token = p[0],
                Expression = p[1]
            };
        }

        public static Node Variable(dynamic[] p)
        {
            IdentifierNode node = p[0];
            if (p.Length < 2) return node;

            node.IndexExpression = p[1];

            return node;
        }

        public static Node Literal(dynamic[] p)
        {
            return new LiteralNode
            {
                Token = p[0]
            };
        }
        
        
        
        
    }
}