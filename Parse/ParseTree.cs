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

            if (t == null) return ret;

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

        /**
         * Expects
         *
         * 0: IdentifierNode for program id
         * 1: DeclarationListNode for declarations or StatementListNode for statements
         * 2: StatementListNode if 1 is DeclarationListNode; otherwise null
         */
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

        /**
         * Expects
         *
         * 0: Token of TokenType.Identifier 
         */
        public static Node Identifier(dynamic[] p)
        {
            return new IdentifierNode
            {
                Token = p[0],
                Type = new SimpleTypeNode
                {
                    PrimitiveType = PrimitiveType.Void
                } // TODO
            };
        }

        /**
         * Expects
         *
         * 0: ProcedureDeclarationNode, FunctionDeclarationNode or null 
         * 1: ProcedureDeclarationNode, FunctionDeclarationNode or null 
         */
        public static Node DeclarationBlockStatement(dynamic[] p)
        {
            return new DeclarationListNode
            {
                Left = p.Length > 0 ? p[0] : NoOpStatement,
                Right = p.Length > 1 && p[1] != null ? p[1] : NoOpStatement
            };
        }

        /**
         * Expects
         *
         * 0: any statement node or null
         * 1: StatementListNode or null
         */
        public static Node BlockStatement(dynamic[] p)
        {
            return new StatementListNode
            {
                Left = p[0] != null ? p[0] : NoOpStatement,
                Right = p.Length > 1 && p[1] != null ? p[1] : NoOpStatement
            };
        }

        /**
         * Expects
         *
         * 0 IdentifierNode
         * 1 AssignmentStatementNode or CallNode
         */
        public static Node AssignOrCallStatement(dynamic[] p)
        {
            Node id = p[0];
            var n = p[1];

            if (n is IdNode) n.Id = id;

            return n;
        }

        /**
         * Expects
         *
         * 0 IdentifierNode
         * 1 TreeNode containing the arguments, an expression for variable index, or null
         */
        public static Node CallOrVariable(dynamic[] p)
        {
            var id = (IdentifierNode) p[0];
            var n = p[1] is TreeNode ? UnwrapTreeNode(p[1]) : p[1];

            switch (n)
            {
                // if (n == null) return id;
                case CallNode _:
                    n.Id = id;

                    return n;
                case List<Node> _:
                    return new CallNode
                    {
                        Id = id,
                        Token = id.Token,
                        Arguments = n
                    };
                default:
                    return new VariableNode
                    {
                        Id = id,
                        Token = id.Token,
                        IndexExpression = n ?? NoOpStatement,
                    };
            }
        }

        /**
         * Expects
         *
         * 0 Integer expression for variable index or expression to assign
         * 1 Expression to assign or null if no index
         */
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

        /**
         * Expects
         *
         * 0 TreeNode of arguments
         */
        public static Node CallStatement(dynamic[] p)
        {
            var arguments = UnwrapTreeNode(p[0]);

            return new CallNode
            {
                Arguments = arguments
            };
        }

        /**
         * Expects
         *
         * 0 TreeNode of IdentifierNodes for variable ids
         * 1 TypeNode for types to be assigned to all given variables
         */
        public static Node VarDeclaration(dynamic[] p)
        {
            var ids = UnwrapTreeNode(p[0]);
            var type = p[1];

            foreach (var id in ids) id.Type = type;

            return new VarDeclarationNode
            {
                Type = type,
                Ids = ids
            };
        }

        /**
         * Expects
         *
         * 0 IdentifierNode
         * 1 TreeNode of ParameterNodes for parameters
         * 2 TypeNode for return type
         * 3 StatementList
         */
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

        /**
         * Expects
         *
         * 0 IdentifierNode
         * 1 TreeNode of ParameterNodes for parameters
         * 2 StatementList
         */
        public static Node ProcedureDeclaration(dynamic[] p)
        {
            var id = p[0];
            var parameters = UnwrapTreeNode(p[1]);
            var block = p[2];

            return new FunctionDeclarationNode // ProcedureDeclarationNode
            {
                Id = id,
                Parameters = parameters,
                Statement = block,
                Type = new SimpleTypeNode
                {
                    PrimitiveType = PrimitiveType.Void
                }
            };
        }

        /**
         * Expects
         *
         * 0 Token of KeywordType.Var if it's a reference parameter, or IdentifierNode 
         * 1 IdentifierNode or TypeNode
         * 2 TypeNode or null
         */
        public static Node Parameter(dynamic[] p)
        {
            var reference = p[0] is Token t && t.KeywordType == KeywordType.Var;

            if (reference) p = p.Skip(1).ToArray();

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

        /**
         * Expects
         *
         * 0 Token of TokenType.Identifier
         */
        public static Node SimpleType(dynamic[] p)
        {
            var token = (Token) p[0];
            var type = token.Content switch
            {
                "integer" => PrimitiveType.Integer,
                "real" => PrimitiveType.Real,
                "boolean" => PrimitiveType.Boolean,
                "string" => PrimitiveType.String,
                _ => PrimitiveType.Void
            };

            return new SimpleTypeNode
            {
                Token = p[0],
                PrimitiveType = type
            };
        }

        /**
         * Expects
         *
         * 0 IntegerExpression of array size or TypeNode
         * 1 TypeNode if 0 is size or null
         */
        public static Node ArrayType(dynamic[] p)
        {
            var type = p[0] is SimpleTypeNode ? p[0] : p[1];
            var size = p[0] is SimpleTypeNode ? NoOpStatement : p[0];

            return new ArrayTypeNode
            {
                Type = type,
                Size = size,
                PrimitiveType = PrimitiveType.Array,
                SubType = type.PrimitiveType
            };
        }

        /**
         * Expects
         *
         * 0 Expression
         * 1 Token of TokenType.Operator or null
         * 2 Expression or null
         */
        public static Node Expr(dynamic[] p)
        {
            if (p.Length < 2 || p[1] == null) return p[0];

            return new BinaryOpNode
            {
                Left = p[0],
                Token = p[1], // TODO: op?
                Right = p[2],
                Type = new SimpleTypeNode() // TODO (?)
            };
        }

        /**
         * Expects
         *
         * 0 Token of TokenType.Operator or Expression
         * 1 Expression or null
         */
        public static Node SignTerm(dynamic[] p)
        {
            if (p.Length < 2 || p[1] == null) return p[0];

            return new UnaryOpNode
            {
                Token = p[0],
                Expression = p[1],
                Type = new SimpleTypeNode() // TODO ?
            };
            /*return new ExpressionNode
            {
                Sign = ((Token) p[0]).Content[0],
                Expression = p[1]
            };*/
        }

        /**
         * Expects
         *
         * 0 Expression
         * 1 Token of TokenType.Operator or null
         * 2 Expression or null
         */
        public static Node SimpleExprOrTerm(dynamic[] p)
        {
            var term = p[0];

            if (p[1] == null) return term;

            return new BinaryOpNode
            {
                Left = term,
                Token = p[1],
                Right = p[2]
            };
        }

        /**
         * Expects
         *
         * 0 VariableNode
         * 1 Token of TokenType.Identifier with content "size" or null
         */
        public static Node FactorOptSize(dynamic[] p)
        {
            if (p.Length < 2 || p[1] == null) return p[0];

            return new SizeNode
            {
                Variable = p[0],
                Token = p[1]
            };
        }

        /**
         * Expects
         *
         * 0: Token of TokenType.Operator
         * 1: Expression
         */
        public static Node Unary(dynamic[] p)
        {
            return new UnaryOpNode
            {
                Token = p[0],
                Expression = p[1]
            };
        }

        /**
         * Expects
         *
         * 0 IdentifierNode
         * 1 IndexExpression or null
         */
        public static Node Variable(dynamic[] p)
        {
            // TODO: check if we should always return variable
            IdentifierNode node = p[0];
            if (p.Length < 2 || p[1] == null) return node;

            return new VariableNode
            {
                Token = node.Token,
                Id = node,
                IndexExpression = p[1] ?? NoOpStatement
            };
        }

        /**
         * Expects
         *
         * 0 Token of some value type
         */
        public static Node Literal(dynamic[] p)
        {
            var token = (Token) p[0];

            return new LiteralNode
            {
                Token = token,
                Type = new SimpleTypeNode
                {
                    PrimitiveType = token.Type switch
                    {
                        TokenType.IntegerValue => PrimitiveType.Integer,
                        TokenType.RealValue => PrimitiveType.Real,
                        TokenType.StringValue => PrimitiveType.String,
                        TokenType.BooleanValue => PrimitiveType.Boolean,
                        _ => PrimitiveType.Void
                    }
                }
            };
        }

        /**
         * Expects
         *
         * 0 Boolean expression
         * 1 StatementList
         * 2 StatementList or null
         */
        public static Node IfStatement(dynamic[] p)
        {
            return new IfNode
            {
                Expression = p[0],
                TrueBranch = p[1],
                FalseBranch = p.Length > 2 && p[2] != null ? p[2] : NoOpStatement
            };
        }

        /**
         * Expects
         *
         * 0 Boolean expression
         * 1 StatementList
         */
        public static Node WhileStatement(dynamic[] p)
        {
            return new WhileNode
            {
                Expression = p[0],
                Statement = p[1]
            };
        }

        /**
         * Expects
         *
         * 0 Expression or null
         */
        public static Node ReturnStatement(dynamic[] p)
        {
            return new ReturnStatementNode
            {
                Expression = p[0] ?? NoOpStatement
            };
        }

        public static Node WriteStatement(dynamic[] p)
        {
            // will be created in semantic analysis
            return NoOpStatement;
        }

        public static Node ReadStatement(dynamic[] p)
        {
            // will be created in semantic analysis
            return NoOpStatement;
        }

        public static Node AssertStatement(dynamic[] p)
        {
            return new AssertStatementNode
            {
                Expression = p[0]
            };
        }
    }
}