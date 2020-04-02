using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using AST;
using Common;
using Scan;
using Common;
using StatementType = Common.StatementType;

namespace Parse
{
    public class Parser
    {
        private Text Source => Context.Source;
        private readonly Scanner _scanner;
        private Token _inputToken;

        private void NextToken() => _inputToken = _scanner.GetNextToken();
        private KeywordType InputTokenKeywordType => _inputToken.KeywordType;
        private TokenType InputTokenType => _inputToken.Type;
        private string InputTokenContent => _inputToken.Content;

        private static Node NoOpStatement = new NoOpNode();

        public Parser(Scanner scanner)
        {
            _scanner = scanner;
        }

        #region Matching

        private dynamic Match(dynamic match)
        {
            //Console.WriteLine($"trying to match {match}");
            try
            {
                return match switch
                {
                    _ when match is string op => MatchContent(op),
                    _ when match is string[] ops => MatchContent(ops),
                    _ when match is TokenType tt => MatchTokenType(tt),
                    _ when match is TokenType[] tts => MatchTokenType(tts),
                    _ when match is KeywordType kwt => MatchKeywordType(kwt),
                    _ when match is KeywordType[] kwts => MatchKeywordType(kwts),
                    StatementType.BlockStatement => BlockStatement(),
                    StatementType.Statement => Statement(),
                    StatementType.StatementList => StatementList(),
                    StatementType.Expression => Expression(),
                    StatementType.SimpleExpression => SimpleExpression(),
                    StatementType.Arguments => Arguments(),
                    StatementType.Term => Term(),
                    StatementType.Factor => Factor(),
                    StatementType.VarDeclaration => VarDeclaration(),
                    StatementType.ProcedureDeclaration => ProcedureDeclaration(),
                    StatementType.FunctionDeclaration => FunctionDeclaration(),
                    StatementType.AssignmentStatement => AssignmentStatementOrCall(),
                    StatementType.CallStatement => AssignmentStatementOrCall(),
                    StatementType.ReturnStatement => ReturnStatement(),
                    StatementType.ReadStatement => ReadStatement(),
                    StatementType.WriteStatement => WriteStatement(),
                    StatementType.AssertStatement => AssertStatement(),
                    StatementType.IfStatement => IfStatement(),
                    StatementType.WhileStatement => WhileStatement(),
                    StatementType.ParameterDeclaration => ParameterDeclaration(),
                    StatementType.Variable => Variable(),
                    StatementType.VariableList => VariableList(),
                    StatementType.Type => Type(),
                    _ => throw new Exception()
                    /*ErrorService.Add(
                    ErrorType.InvalidOperation,
                    _inputToken,
                    $"tried to match unknown token {match}")*/
                };
            }
            catch (SyntaxErrorException)
            {
                var error = true;
                dynamic result = null;
                while (error)
                {
                    try
                    {
                        error = false;
                        result = match switch
                        {
                            _ when match is TokenType => _inputToken,
                            _ when match is TokenType[] => _inputToken,
                            _ when match is KeywordType => _inputToken,
                            _ when match is KeywordType[] => _inputToken,
                            _ when match is string => _inputToken,
                            _ when match is string[] => _inputToken,
                            _ => NoOpStatement
                        };
                    }
                    catch (SyntaxErrorException)
                    {
                        error = true;
                    }
                }

                return result;
            }
        }

        private dynamic[] MatchSequence(params dynamic[] seq)
        {
            return seq.Select(Match).ToArray();
        }

        private Token MatchTokenType(TokenType[] ttl, bool advance = true)
        {
            var matchedToken = _inputToken;
            if (ttl.Any(tt => InputTokenType == tt))
            {
                Console.WriteLine($"matched {matchedToken}");
                if (advance) NextToken();
                return matchedToken;
            }

            // UnexpectedTokenError(ttl);
            return matchedToken;
        }

        private Token MatchTokenType(TokenType tt, bool advance = true) =>
            MatchTokenType(new[] {tt}, advance);

        private Token MatchKeywordType(KeywordType[] kwtl, bool advance = true)
        {
            var matchedToken = _inputToken;
            if (kwtl.Any(kwt => InputTokenKeywordType == kwt))
            {
                Console.WriteLine($"matched {matchedToken}");
                if (advance) NextToken();
                return matchedToken;
            }

            // UnexpectedKeywordError(kwtl);
            return matchedToken;
        }

        private Token MatchKeywordType(KeywordType kwt, bool advance = true) =>
            MatchKeywordType(new[] {kwt}, advance);

/*        private Token MatchOperatorType(OperatorType[] ops, bool advance = true)
        {
            var matchedToken = _inputToken;
            var operatorType = Token.OperatorToOperatorType.TryGetValueOrDefault(InputTokenContent);
            if (ops.Any(op => operatorType == op))
            {
                if (advance) NextToken();
                return matchedToken;
            }

            UnexpectedOperatorError(ops);
            return matchedToken;
        }
        
        private Token MatchOperatorType(OperatorType op, bool advance = true) =>
            MatchOperatorType(new[] {op}, advance);*/

        private Token MatchContent(string s, bool advance = true)
        {
            var matchedToken = _inputToken;
            if (InputTokenContent.Equals(s))
            {
                Console.WriteLine($"matched {matchedToken}");
                if (advance) NextToken();
                return matchedToken;
            }

            // UnexpectedContentError(s);
            return matchedToken;
        }

        private Token MatchContent(string[] sl, bool advance = true)
        {
            var matchedToken = _inputToken;
            if (sl.Any(InputTokenContent.Equals))
            {
                Console.WriteLine($"matched {matchedToken}");
                if (advance) NextToken();
                return matchedToken;
            }

            // UnexpectedContentError(sl);
            return matchedToken;
        }

        private string[] ExpectedTypes =
        {
            "integer",
            "real",
            "boolean",
            "string",
        };
        
        private PrimitiveType Type()
        {
            var tt = MatchContent(ExpectedTypes);
            if (tt != null) return Token.TokenContentToPrimitiveType(tt.Content);

            return PrimitiveType.Void;
        }
        
        #endregion Matching

        public Node Program()
        {
            NextToken();

            MatchSequence(
                KeywordType.Program,
                TokenType.Identifier,
                TokenType.Separator,
                StatementType.BlockStatement,
                TokenType.Dot
            ).Deconstruct(
                out Token token,
                out Token id,
                out Token __,
                out Node block,
                out Token ___);

            return new ProgramNode
            {
                Token = token,
                Id = new IdentifierNode
                {
                    Type = PrimitiveType.String,
                    Token = id
                },
                Block = block
            };
        }

        public Node BlockStatement()
        {
            MatchSequence(
                KeywordType.Begin,
                StatementType.Statement,
                StatementType.StatementList
            ).Deconstruct(
                out Token _,
                out Node left,
                out Node right
            );

            return new StatementListNode
            {
                Left = left,
                Right = right
            };
        }

        public KeywordType[] StatementFirstKeywords =
        {
            KeywordType.Begin,
            KeywordType.If,
            KeywordType.While,
            KeywordType.Return,
            KeywordType.Assert,
            KeywordType.Var,
            KeywordType.Function,
            KeywordType.Procedure
        };

        public Node Statement()
        {
            switch (InputTokenType)
            {
                case TokenType.Keyword:
                    switch (InputTokenKeywordType)
                    {
                        case KeywordType.Begin:
                        case KeywordType.If:
                        case KeywordType.While:
                            return StructuredStatement();
                        case KeywordType.Return:
                        case KeywordType.Assert:
                            return SimpleStatement();
                        case KeywordType.Var:
                        case KeywordType.Function:
                        case KeywordType.Procedure:
                            return DeclarationStatement();
                        default:
                            throw new Exception();
                    }
                case TokenType.Identifier:
                    return SimpleStatement();
                default:
                    throw new Exception();
            }
        }

        public Node StatementList()
        {
            if (InputTokenKeywordType == KeywordType.End)
            {
                MatchKeywordType(KeywordType.End);
                return NoOpStatement;
            }

            MatchTokenType(
                TokenType.Separator
            );

            if (InputTokenType == TokenType.Identifier ||
                StatementFirstKeywords.Includes(InputTokenKeywordType))
            {
                MatchSequence(
                    StatementType.Statement,
                    StatementType.StatementList
                ).Deconstruct(
                    out Node left,
                    out Node right
                );

                return new StatementListNode
                {
                    Left = left,
                    Right = right
                };
            }

            MatchKeywordType(
                KeywordType.End
            );

            return NoOpStatement;
        }

        public Node SimpleStatement()
        {
            switch (InputTokenType)
            {
                case TokenType.Identifier:
                    return AssignmentStatementOrCall(); // handles read, write
                case TokenType.Keyword:
                    switch (InputTokenKeywordType)
                    {
                        case KeywordType.Return:
                            return ReturnStatement();
                        case KeywordType.Assert:
                            return AssertStatement();
                        default:
                            throw new Exception();
                    }
                default:
                    throw new Exception();
            }
        }

        public Node StructuredStatement()
        {
            switch (InputTokenKeywordType)
            {
                case KeywordType.Begin:
                    return BlockStatement();
                case KeywordType.If:
                    return IfStatement();
                case KeywordType.While:
                    return WhileStatement();
                default:
                    throw new Exception();
            }
        }

        public Node DeclarationStatement()
        {
            switch (InputTokenKeywordType)
            {
                case KeywordType.Var:
                    return VarDeclaration();
                case KeywordType.Procedure:
                    return ProcedureDeclaration();
                case KeywordType.Function:
                    return FunctionDeclaration();
                default:
                    throw new Exception();
            }
        }

        public Node AssignmentStatementOrCall()
        {
            var id = MatchTokenType(TokenType.Identifier);

            if (InputTokenType == TokenType.Assignment)
            {
                MatchSequence(
                    TokenType.Assignment,
                    StatementType.Expression
                ).Deconstruct(
                    out Token token,
                    out Node expression
                );

                return new AssignmentNode
                {
                    Token = token,
                    Id = new IdentifierNode
                    {
                        Token = id
                    },
                    Expression = expression
                };
            }

            MatchSequence(
                TokenType.OpenParen,
                StatementType.Arguments,
                TokenType.CloseParen
            ).Deconstruct(
                out Token _,
                out List<ExpressionNode> arguments,
                out Token __
            );

            return new CallNode
            {
                Token = id, // TODO: the same?
                Id = new IdentifierNode
                {
                    Token = id
                },
                Arguments = arguments
            };
        }

        public Node ReturnStatement()
        {
            MatchKeywordType(KeywordType.Return);

            if (SimpleExpressionFirstTokens.Includes(InputTokenType))
            {
                var expression = Match(StatementType.Expression);

            }

            // TODO
            return NoOpStatement;
        }

        public Node AssertStatement()
        {
            MatchSequence(
                KeywordType.Assert,
                TokenType.OpenParen,
                StatementType.Expression, // TODO: boolean expression
                TokenType.CloseParen
            ).Deconstruct(
                out Token _,
                out Token __,
                out Node expression,
                out Token ___
            );

            // TODO
            return NoOpStatement;
        }

        public Node ReadStatement()
        {
            MatchSequence(
                "read",
                TokenType.OpenParen,
                StatementType.VariableList,
                TokenType.CloseParen
            ).Deconstruct(
                out Token token,
                out Token _,
                out List<Node> variables,
                out Token __
            );

            // TODO
            return NoOpStatement;
        }

        public Node WriteStatement()
        {
            MatchSequence(
                "writeln",
                TokenType.OpenParen,
                StatementType.Arguments,
                TokenType.CloseParen
            ).Deconstruct(
                out Token token,
                out Token _,
                out List<Node> arguments,
                out Token __
            );

            // TODO
            return NoOpStatement;
        }
        

        public List<Node> VariableList()
        {
            var vars = new List<Node>();
            while (InputTokenType != TokenType.CloseParen)
            {
                var variable = Match(StatementType.Variable);

                vars.Add((IdentifierNode) variable);
                
                if (InputTokenType == TokenType.Colon)
                {
                    Match(TokenType.Colon);
                }
            }

            // TODO: requires at least one
            return vars;
        }

        public Node Variable()
        {
            var id = Match(TokenType.Identifier);

            if (InputTokenType != TokenType.OpenBlock)
            {
                return new IdentifierNode
                {
                    Token = id,
                    IndexExpression = NoOpStatement
                };
            }

            MatchSequence(
                TokenType.OpenBlock,
                StatementType.Expression, // TODO: IntegerExpression
                TokenType.CloseBlock
            ).Deconstruct(
                out Token _,
                out Node expression,
                out Token __
            );

            return new IdentifierNode
            {
                Token = id,
                IndexExpression = expression
            };

        }
        private String[] RelationalOperators =
        {
            "<",
            "<=",
            ">=",
            ">",
            "<>",
            "="
        };

        public Node Expression()
        {
            var expression = Match(StatementType.SimpleExpression);

            if (InputTokenType == TokenType.Operator && RelationalOperators.Includes(InputTokenContent))
            {
                MatchSequence(
                    TokenType.Operator,
                    StatementType.SimpleExpression
                ).Deconstruct(
                    out Token op,
                    out Node right
                );

                return new BinaryOpNode
                {
                    Token = op,
                    Left = expression,
                    Right = right,
                };
            }

            return expression;
        }

        public TokenType[] SimpleExpressionFirstTokens =
        {
            TokenType.OpenParen,
            TokenType.Identifier,
            TokenType.IntegerValue,
            TokenType.RealValue,
            TokenType.Quote,
            TokenType.Operator // TODO: only signs + - and not
        };


        public String[] AddingOperators =
        {
            "+", "-", "or"
        };

        public Node SimpleExpression()
        {
            var sign = '+';
            if (InputTokenType == TokenType.Operator &&
                (InputTokenContent.Equals("+") ||
                 InputTokenContent.Equals("-")))
            {
                sign = InputTokenContent[0];
                NextToken();
            }

            var term = Match(StatementType.Term);

            if (!AddingOperators.Includes(InputTokenContent))
            {
                return new ExpressionNode
                {
                    // TODO: Token?
                    Sign = sign,
                    Expression = term
                };
            }

            MatchSequence(
                TokenType.Operator,
                StatementType.Term
            ).Deconstruct(
                out Token op,
                out Node term2
            );

            return new BinaryOpNode
            {
                Token = op,
                Left = term,
                Right = term2
            };
        }

        public String[] MultiplyingOperators =
        {
            "*", "/", "%", "and"
        };

        public Node Term()
        {
            var factor = Match(StatementType.Factor);

            if (MultiplyingOperators.Includes(InputTokenContent))
            {
                MatchSequence(
                    TokenType.Operator,
                    StatementType.Term // Factor in the grammar?
                ).Deconstruct(
                    out Token op,
                    out Node factor2
                );

                return new BinaryOpNode
                {
                    Token = op,
                    Left = factor,
                    Right = factor2
                };
            }

            return new ExpressionNode
            {
                // TODO: Token?
                Expression = factor
            };
        }

        public Node Factor()
        {
            switch (InputTokenType)
            {
                case TokenType.Identifier:
                    return FactorCallOrVariable();
                case TokenType.IntegerValue:
                case TokenType.RealValue:
                case TokenType.StringValue:
                case TokenType.BooleanValue:
                    return Literal();
                case TokenType.Operator when InputTokenContent.Equals("not"):
                    MatchSequence(
                        TokenType.Operator,
                        StatementType.Factor
                    ).Deconstruct(
                        out Token token,
                        out Node factor2
                    );

                    return new UnaryOpNode
                    {
                        Token = token,
                        Expression = factor2
                    };
                case TokenType.OpenParen:
                    MatchSequence(
                        TokenType.OpenParen,
                        StatementType.Expression,
                        TokenType.CloseParen
                    ).Deconstruct(
                        out Token _,
                        out Node expression,
                        out Token __);

                    if (InputTokenType != TokenType.Dot)
                    {
                        return new ExpressionNode
                        {
                            // TODO: Token?
                            Expression = expression
                        };
                    }

                    MatchSequence(
                        TokenType.Dot,
                        "size" // TODO: PredefinedId.Size
                    );

                    return new SizeNode // TODO: or just expressionnode with the size token?
                    {
                        Expression = expression
                    };
                default:
                    throw new Exception();
            }
        }

        public Node FactorCallOrVariable()
        {
            var id = Match(TokenType.Identifier);

            switch (InputTokenType)
            {
                case TokenType.OpenParen:
                {
                    MatchSequence(
                        TokenType.OpenParen,
                        StatementType.Arguments,
                        TokenType.CloseParen
                    ).Deconstruct(
                        out Token _,
                        out List<ExpressionNode> arguments,
                        out Token __
                    );

                    var call = new CallNode
                    {
                        Token = id, // TODO: same?
                        Id = new IdentifierNode
                        {
                            Token = id
                        },
                        Arguments = arguments
                    };

                    if (InputTokenType != TokenType.Dot) return call;

                    MatchSequence(
                        TokenType.Dot,
                        "size" // PredefinedId.Size
                    );

                    return new SizeNode
                    {
                        Expression = call
                    };

                }
                case TokenType.OpenBlock:
                {
                    MatchSequence(
                        TokenType.OpenBlock,
                        StatementType.Expression, // TODO: integer expression?
                        TokenType.CloseBlock
                    ).Deconstruct(
                        out Token _,
                        out Node expression,
                        out Token __
                    );

                    var variable = new IdentifierNode
                    {
                        Token = id,
                        IndexExpression = expression
                    };

                    if (InputTokenType != TokenType.Dot) return variable;

                    MatchSequence(
                        TokenType.Dot,
                        "size" // TODO: PredefinedId.Size
                    );

                    return new SizeNode
                    {
                        Expression = variable
                    };
                }
                default:
                    return new IdentifierNode
                    {
                        Token = id,
                        IndexExpression = NoOpStatement
                    };
            }
        }

        public Node Literal()
        {
            var type = InputTokenType;
            var token = MatchTokenType(type);

            return new LiteralNode
            {
                Token = token,
                Type = Token.TokenToPrimitiveType(type)
            };
        }

        public Node IfStatement()
        {
            MatchSequence(
                KeywordType.If,
                StatementType.Expression, // TODO: BooleanExpression
                KeywordType.Then,
                StatementType.Statement
            ).Deconstruct(
                out Token token,
                out Node expression,
                out Token _,
                out Node statement
            );

            if (InputTokenType == TokenType.Separator)
            {
                Match(TokenType.Separator); // TODO: against language spec, though
            }
            
            if (InputTokenKeywordType != KeywordType.Else)
            {
                return new IfNode
                {
                    Token = token,
                    Expression = expression,
                    TrueBranch = statement
                };
            }

            MatchSequence(
                KeywordType.Else,
                StatementType.Statement
            ).Deconstruct(
                out Token token2,
                out Node statement2
            );

            return new IfNode
            {
                Token = token,
                Expression = expression,
                TrueBranch = statement,
                FalseBranch = statement2
            };
        }

        public Node WhileStatement()
        {
            MatchSequence(
                KeywordType.While,
                StatementType.Expression, // TODO: BooleanExpression
                KeywordType.Do,
                StatementType.Statement
            ).Deconstruct(
                out Token token,
                out Node expression,
                out Token _,
                out Node statement
            );

            return new WhileNode
            {
                Token = token,
                Expression = expression,
                Statement = statement
            };
        }

        public Node VarDeclaration()
        {
            MatchSequence(
                KeywordType.Var,
                TokenType.Identifier
            ).Deconstruct(
                out Token token,
                out Token id
            );


            var idNodes = new[]
            {
                new IdentifierNode
                {
                    Token = id
                }
            }.ToList();

            while (InputTokenType == TokenType.Comma)
            {
                MatchSequence(
                    TokenType.Comma,
                    TokenType.Identifier
                ).Deconstruct(
                    out Token _,
                    out Token id2
                );
                idNodes.Add(new IdentifierNode
                {
                    Token = id2
                });
            }

            MatchSequence(
                TokenType.Colon,
                StatementType.Type
            ).Deconstruct(
                out Token _,
                out PrimitiveType type
            );

            return new VarDeclarationNode
            {
                Token = token,
                Ids = idNodes,
                Type = type
            };
        }

        public Node ProcedureDeclaration()
        {
            MatchSequence(
                KeywordType.Procedure,
                TokenType.Identifier,
                TokenType.OpenParen,
                StatementType.ParameterDeclaration,
                TokenType.CloseParen,
                TokenType.Separator,
                StatementType.BlockStatement,
                TokenType.Separator
            ).Deconstruct(
                out Token token,
                out Token id,
                out Token _,
                out List<Node> parameters,
                out Token __,
                out Token ___,
                out Node statement,
                out Token ____
            );

            return new ProcedureDeclarationNode
            {
                Token = token,
                Id = new IdentifierNode
                {
                    Token = id,
                },
                Parameters = parameters,
                Statement = statement
            };
        }

        public Node FunctionDeclaration()
        {
            MatchSequence(
                KeywordType.Function,
                TokenType.Identifier,
                TokenType.OpenParen,
                StatementType.ParameterDeclaration,
                TokenType.CloseParen,
                TokenType.Colon,
                StatementType.Type,
                TokenType.Separator,
                StatementType.BlockStatement,
                TokenType.Separator
            ).Deconstruct(
                out Token token,
                out Token id,
                out Token _,
                out List<ParameterNode> parameters,
                out Token __,
                out Token ___,
                out PrimitiveType type,
                out Token ____,
                out Node statement,
                out Token _____
            );
         
            return new FunctionDeclarationNode
            {
                Token = token,
                Type = type,
                Id = new IdentifierNode
                {
                    Token = id,
                },
                Parameters = parameters,
                Statement = statement
            };
        }

        public List<ParameterNode> ParameterDeclaration()
        {
            var parameters  = new List<ParameterNode>();

            while (InputTokenType != TokenType.CloseParen)
            {
                var reference = false;
                
                if (InputTokenKeywordType == KeywordType.Var)
                {
                    Match(KeywordType.Var);
                    reference = true;
                }
                
                MatchSequence(
                    TokenType.Identifier,
                    TokenType.Colon,
                    StatementType.Type
                ).Deconstruct(
                    out Token id,
                    out Token _,
                    out PrimitiveType type
                );

                parameters.Add(
                    new ParameterNode
                    {
                        // TODO: Token?
                        Id = new IdentifierNode
                        {
                            Token = id,
                            Type = type
                        },
                        Reference = reference
                    }
                );

                if (InputTokenType == TokenType.Comma)
                {
                    Match(TokenType.Comma);
                }
            }
            return parameters;
        }

        public List<ExpressionNode> Arguments()
        {
            var args = new List<ExpressionNode>();
            
            while (InputTokenType != TokenType.CloseParen)
            {
                var expression = Match(StatementType.Expression);
                args.Add(
                    new ExpressionNode
                    {
                        // TODO: token?
                        Expression = expression
                    }
                );
                if (InputTokenType == TokenType.Comma)
                {
                    Match(TokenType.Comma);
                }
            }

            return args;
        }
    }
}