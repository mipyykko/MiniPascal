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
            if (p[0] is ErrorNode) return p[0];
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
            if (p[0] is ErrorNode) return p[0];

            Node id = p[0];
            var n = p[1];

            if (n is AssignmentNode an)
            {
                if (an.LValue is ArrayDereferenceNode adn)
                {
                    adn.LValue.Id = id;
                }
                an.LValue.Id = id;
            }
            else
            {
                n.Id = id;
            }
            // if (n is IdNode) n.Id = id;

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
            if (p[0] is ErrorNode) return p[0];
            
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
                    return VariableOrArrayDeference(p);
                    /*new VariableNode
                    {
                        Id = id,
                        Token = id.Token,
                        IndexExpression = n ?? NoOpStatement,
                    };*/
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
            if (p[0] is ErrorNode) return p[0];
            
            var expr = p[0];

            return new AssignmentNode
            {
                LValue = new VariableNode(),    
                Expression = expr
            };
        }

        public static Node AssignmentToArrayStatement(dynamic[] p)
        {
            if (p[0] is ErrorNode) return p[0];

            var index = p[0];
            var expr = p[1];

            return new AssignmentNode
            {
                LValue = new ArrayDereferenceNode
                {
                    LValue = new VariableNode(),
                    Expression = index
                },
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
            if (p[0] is ErrorNode) return p[0];
            
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
            if (p[0] is ErrorNode) return p[0];
            
            var ids = UnwrapTreeNode(p[0]);
            var type = p[1];

            var variables = new List<Node>();

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
            if (p[0] is ErrorNode) return p[0];
            
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
            if (p[0] is ErrorNode) return p[0];
            
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
            if (p[0] is ErrorNode) return p[0];
            
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
            if (p[0] is ErrorNode) return p[0];
            
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
            if (p[0] is ErrorNode) return p[0];
            
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

        private static OperatorType OperatorTypeFromToken(string op) =>
            op.ToLower() switch
            {
                "+" => OperatorType.Add,
                "-" => OperatorType.Sub,
                "*" => OperatorType.Mul,
                "/" => OperatorType.Div,
                "%" => OperatorType.Mod,
                "and" => OperatorType.And,
                "or" => OperatorType.Or,
                "not" => OperatorType.Not,
                "=" => OperatorType.Eq,
                "<>" => OperatorType.Neq,
                "<" => OperatorType.Lt,
                "<=" => OperatorType.Leq,
                ">=" => OperatorType.Geq,
                ">" => OperatorType.Gt,
                _ => throw new Exception($"unknown operator {op}")
            };

        public static Node WrapLValue(Node node)
        {
            if (node is LValueNode lv)
            {
                return new ValueOfNode
                {
                    LValue = lv,
                    Type = lv.Type,
                    Token = lv.Token
                };
            }

            return node;
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
            if (p[0] is ErrorNode) return p[0];
            
            if (p.Length < 2 || p[1] == null) return p[0];

            var Left = WrapLValue(p[0]);
            var Right = p.Length < 3 ? WrapLValue(p[3]) : Expr(p.Skip(2).ToArray());

            return new BinaryOpNode
            {
                Left = Left, // WrapLValue(p[0]),
                Op = OperatorTypeFromToken(p[1].Content),
                Token = p[1], 
                Right = Right, // WrapLValue(p[2]),
                Type = new SimpleTypeNode() // TODO (?)
            };
        }

        public static Node ValueOf(Node node)
        {
            if (!(node is LValueNode lvn)) return node;

            return new ValueOfNode
            {
                LValue = lvn
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
            if (p[0] is ErrorNode) return p[0];
            
            if (p.Length < 2 || p[1] == null) return p[0];

            return new UnaryOpNode
            {
                Token = p[0],
                Op = OperatorTypeFromToken(p[0].Content),
                Expression = WrapLValue(p[1]),
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
            if (p[0] is ErrorNode) return p[0];

            var term = p[0];

            return p[1] == null ? (Node) term : Expr(p);

            /*return new BinaryOpNode
            {
                Left = term,
                Token = p[1],
                Right = p[2]
            };*/
        }

        /**
         * Expects
         *
         * 0 VariableNode
         * 1 Token of TokenType.Identifier with content "size" or null
         */
        public static Node FactorOptSize(dynamic[] p)
        {
            if (p[0] is ErrorNode) return p[0];

            if (p.Length < 2 || p[1] == null) return p[0];

            return new SizeNode
            {
                LValue = p[0],
                Token = p[1],
                Type = new SimpleTypeNode 
                {
                    PrimitiveType = PrimitiveType.Integer
                }
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
            if (p[0] is ErrorNode) return p[0];
            
            return new UnaryOpNode
            {
                Token = p[0],
                Op = OperatorTypeFromToken(p[0].Content),
                Expression = WrapLValue(p[1])
            };
        }

        public static Node VariableOrArrayDeference(dynamic[] p)
        {
            if (p[0] is ErrorNode) return p[0];

            return p[1] == null ? Variable(p) : ArrayDereference(p);
        }
        /**
         * Expects
         *
         * 0 IdentifierNode
         * 1 IndexExpression or null
         */
        public static Node Variable(dynamic[] p)
        {
            if (p[0] is ErrorNode) return p[0];
            
            // TODO: check if we should always return variable
            IdentifierNode node = p[0];
            // if (p.Length < 2 || p[1] == null) return node;

            return new VariableNode
            {
                Token = node.Token,
                Id = node,
                // IndexExpression = p[1] ?? NoOpStatement
            };
        }

        public static Node ArrayDereference(dynamic[] p)
        {
            IdentifierNode node = p[0];
            var expression = p[1];

            return new ArrayDereferenceNode
            {
                Token = expression.Token,
                LValue = (LValueNode) Variable(p),
                Expression = expression
            };
        }

        private static readonly Dictionary<TokenType, Type> _valueTypes = new Dictionary<TokenType, Type>
        {
            [TokenType.IntegerValue] = typeof(IntegerValueNode),
            [TokenType.RealValue] = typeof(RealValueNode),
            [TokenType.StringValue] = typeof(StringValueNode),
            [TokenType.BooleanValue] = typeof(BooleanValueNode)
        };
        
        /**
         * Expects
         *
         * 0 Token of some value type
         */
        public static Node Literal(dynamic[] p)
        {
            if (p[0] is ErrorNode) return p[0];
            
            var token = (Token) p[0];

            dynamic value = token.Type switch
            {
                TokenType.IntegerValue => int.Parse(token.Content),
                TokenType.RealValue => double.Parse(token.Content),
                TokenType.StringValue => token.Content,
                TokenType.BooleanValue => token.Content.ToLower() == "true"
            };

            var node = (ValueNode) Activator.CreateInstance(_valueTypes[token.Type]);

            node.Token = token;
            node.Type = new SimpleTypeNode
            {
                PrimitiveType = token.Type switch
                {
                    TokenType.IntegerValue => PrimitiveType.Integer,
                    TokenType.RealValue => PrimitiveType.Real,
                    TokenType.StringValue => PrimitiveType.String,
                    TokenType.BooleanValue => PrimitiveType.Boolean,
                    _ => PrimitiveType.Void
                }
            };
            node.Value = value;

            return node;
            /*return new LiteralNode
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
            };*/
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
            if (p[0] is ErrorNode) return p[0];
            
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
            if (p[0] is ErrorNode) return p[0];
            
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
            if (p[0] is ErrorNode) return p[0];
            
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
            if (p[0] is ErrorNode) return p[0];
            
            return new AssertStatementNode
            {
                Expression = p[0]
            };
        }

        public static Node Error(dynamic[] p)
        {
            return new ErrorNode
            {
                Token = p[0]
            };
        }
    }
}