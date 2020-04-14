using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.AST;
using Scan;
using static Common.Util;

namespace Parse
{
    public static class ParseTree
    {
        /**
         * Temporary structure for building the tree
         */
        public class TreeNode
        {
            public Node Left;
            public TreeNode Right;
        }

        private static List<Node> UnwrapTreeNode(TreeNode t)
        {
            var ret = new List<Node>();

            while (true)
            {
                if (t.Left == null || t.Left is NoOpNode) return ret;
                ret.Add(t.Left);
                if (t.Right == null) return ret;
                
                t = t.Right;
            }
        }

        public static TreeNode ListBuilder(dynamic[] p)
        {
            return new TreeNode
            {
                Left = p.Length > 0 ? p[0] : null,
                Right = p.Length > 1 ? p[1] : null
            };
        }

        private static Node NoOpStatement => new NoOpNode();
        
        public static dynamic Program(dynamic[] p)
        {
            var id = p[0];
            var declaration = p[1] is DeclarationListNode ? p[1] : NoOpStatement;
            var main = declaration is NoOpNode ? p[1] : p[2];
            
            return new ProgramNode
            {
                //Token = p[0],
                Id = id,
                DeclarationBlock = declaration,
                MainBlock = main
            };
        }

        public static Node Identifier(dynamic[] p)
        {
            return new IdentifierNode
            {
                Token = p[0]
            };
        }

        public static Node DeclarationBlockStatement(dynamic[] p)
        {
            return new DeclarationListNode
            {
                Left = p.Length > 0 ? p[0] : NoOpStatement,
                Right = p.Length > 1 && p[1] != null ? p[1] : NoOpStatement
            };
        }
        
        public static Node BlockStatement(dynamic[] p)
        {
            return new StatementListNode
            {
                Left = p[0] != null ? p[0] : NoOpStatement,
                Right = p.Length > 1 && p[1] != null ? p[1] : NoOpStatement
            };
        }

        public static Node AssignOrCallStatement(dynamic[] p)
        {
            Node id = p[0];
            var n = p[1];

            if (n is IdNode)
            {
                n.Id = id;
            }

            return n;
        }

        public static Node CallOrVariable(dynamic[] p)
        {
            var id = (IdentifierNode) p[0];
            var n = p[1] is TreeNode ? UnwrapTreeNode(p[1]) : p[1];

            if (n == null) return id;
            
            if (n is List<Node>)
            {
                return new CallNode
                {
                    Id = id,
                    Arguments = n
                };
            }
        
            id.IndexExpression = n;
            return id;
        }
        
        public static Node AssignmentStatement(dynamic[] p)
        {
            var index = p.Length < 2 || p[1] == null ? NoOpStatement : p[0];
            var expr = index is NoOpNode ? p[0] : p[1];

            return new AssignmentNode
            {
                IndexExpression = index,
                Expression = expr
            };
        }

        public static Node CallStatement(dynamic[] p)
        {
            var arguments = UnwrapTreeNode(p[0]);

            return new CallNode
            {
                Arguments = arguments
            };
        }

        public static Node VarDeclaration(dynamic[] p)
        {
            var ids = UnwrapTreeNode(p[0]);
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

        public static Node FunctionDeclaration(dynamic[] p)
        {
            var id = p[0];
            var parameters = UnwrapTreeNode(p[1]);
            var type = p[2];
            var block = p[3];

            return new FunctionDeclarationNode
            {
                Id = id,
                Parameters = parameters,
                Statement = block,
                Type = type
            };
        }

        public static Node ProcedureDeclaration(dynamic[] p)
        {
            var id = p[0];
            var parameters = UnwrapTreeNode(p[1]);
            var block = p[2];

            return new ProcedureDeclarationNode
            {
                Id = id,
                Parameters = parameters,
                Statement = block,
            };
        }

        public static Node Parameter(dynamic[] p)
        {
            var reference = p[0] is Token t && t.KeywordType == KeywordType.Var;

            if (reference)
            {
                p = p.Skip(1).ToArray();
            }

            var id = (IdentifierNode) p[0];
            var type = p[1];

            id.Type = type;

            return new ParameterNode
            {
                Type = type,
                Id = id,
                Reference = reference
            };
        }

        public static Node SimpleType(dynamic[] p)
        {
            return new SimpleTypeNode
            {
                Token = p[0]
            };
        }

        public static Node ArrayType(dynamic[] p)
        {
            var type = p[0] is SimpleTypeNode ? p[0] : p[1];
            var size = p[0] is SimpleTypeNode ? NoOpStatement : p[0];
            
            return new ArrayTypeNode
            {
                Type = type,
                Size = size
            };
        }

        public static Node Expr(dynamic[] p)
        {
            if (p.Length < 2 || p[1] == null) return p[0];

            return new BinaryOpNode
            {
                Left = p[0],
                Token = p[1], // TODO: op?
                Right = p[2]
            };
        }

        public static Node SignTerm(dynamic[] p)
        {
            if (p.Length < 2 || p[1] == null) return p[0];

            return new UnaryOpNode
            {
                Token = p[0],
                Expression = p[1]
            };
            /*return new ExpressionNode
            {
                Sign = ((Token) p[0]).Content[0],
                Expression = p[1]
            };*/
        }

        public static Node SimpleExprOrTerm(dynamic[] p)
        {
            var term = p[0];

            if (p[1] == null)
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
            if (p.Length < 2 || p[1] == null) return p[0];

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
            if (p.Length < 2 || p[1] == null) return node;

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

        public static Node IfStatement(dynamic[] p)
        {
            return new IfNode
            {
                Expression = p[0],
                TrueBranch = p[1],
                FalseBranch = p.Length > 2 ? p[2] : NoOpStatement
            };
        }

        public static Node WhileStatement(dynamic[] p)
        {
            return new WhileNode
            {
                Expression = p[0],
                Statement = p[1]
            };
        }

        public static Node ReturnStatement(dynamic[] p)
        {
            return new ReturnStatementNode
            {
                Expression = p[0] ?? NoOpStatement
            };
        }
    }
}