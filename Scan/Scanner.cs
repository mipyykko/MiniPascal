using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;

namespace Scan
{
    public class Scanner
    {
        private Text Source => Context.Source;

        private int _startPos;
        private int _startLinePos;

        private char Current => Source.Current;
        private char Peek => Source.Peek;
        private bool IsExhausted => Source.IsExhausted;

        private (int Start, int End) TokenRange(string token)
        {
            return (_startPos, _startPos + Math.Max(0, token.Length - 1));
        }

        private (int Line, int Start, int End) TokenLineRange(string token)
        {
            return (Source.Line, _startLinePos, _startLinePos + Math.Max(0, token.Length - 1));
        }

        private SourceInfo GetSourceInfo(string token)
        {
            return SourceInfo.Of(
                TokenRange(token),
                TokenLineRange(token)
            );
        }

        public Token GetNextToken()
        {
            Source.SkipSpacesAndComments();

            if (IsExhausted) return Token.Of(TokenType.EOF, "EOF", GetSourceInfo(""));

            _startPos = Source.Pos;
            _startLinePos = Source.LinePos;
            var (tokenType, token) = GetTrivialToken();

            switch (tokenType)
            {
                case TokenType.Number:
                {
                    var (numberTokenType, numberContents) = GetNumberContents();
                    return Token.Of(numberTokenType, numberContents, GetSourceInfo(numberContents));
                }
                case TokenType.Quote:
                {
                    var stringContents = GetStringContents();
                    return Token.Of(TokenType.StringValue, stringContents, GetSourceInfo(stringContents));
                }
                case TokenType.Unknown when char.IsLetter(Current):
                {
                    var atom = GetAtom();
                    var kw = Token.KeywordTypes.TryGetValueOrDefault(atom);

                    if (kw != KeywordType.Unknown) return Token.Of(TokenType.Keyword, kw, atom, GetSourceInfo(atom));
                    /*if (new[] {"true", "false"}.Includes(atom.ToLower()))
                        return Token.Of(TokenType.BooleanValue, atom, GetSourceInfo(atom));*/

                    return Token.Of(TokenType.Identifier, atom, GetSourceInfo(atom));
                }
                case TokenType.Unknown:
                    Source.Advance();
                    return GetNextToken();
                default:
                    return Token.Of(tokenType, token, GetSourceInfo(token));
            }
        }

        private (TokenType, string) GetTrivialToken()
        {
            if (Text.IsDigit(Current)) return (TokenType.Number, $"{Current}");

            foreach (var token in Token.TrivialTokenTypes.Keys.Where(token =>
                Source.Pos + token.Length <= Source.End &&
                Source.Range(Source.Pos, token.Length).ToLower().Equals(token.ToLower())))
            {
                Source.Advance(token.Length);
                return (Token.TrivialTokenTypes[token], token);
            }

            return (TokenType.Unknown, $"{Current}");
        }

        private (TokenType, string) GetNumberContents()
        {
            var n = new StringBuilder("");
            var type = TokenType.IntegerValue;

            while (!Source.IsExhausted && Text.IsDigit(Current))
            {
                n.Append(Current);
                Source.Advance();
            }

            // int value

            if (!Current.Equals('.')) return (type, n.ToString());

            Source.Advance();
            n.Append('.');

            type = TokenType.RealValue;

            while (!Source.IsExhausted && Text.IsDigit(Current))
            {
                n.Append(Current);
                Source.Advance();
            }

            if (!char.ToLower(Current).Equals('e')) return (type, n.ToString());

            // exponent

            n.Append("e");
            Source.Advance();

            if (Current.Equals('+') || Current.Equals('-'))
            {
                n.Append(Current);
                Source.Advance();
            }

            while (!Source.IsExhausted && Text.IsDigit(Current))
            {
                n.Append(Current);
                Source.Advance();
            }

            return (type, n.ToString());
        }

        private readonly Dictionary<char, string> Literals = new Dictionary<char, string>()
        {
            ['n'] = "\n",
            ['t'] = "\t",
            ['\\'] = "\\",
            ['"'] = "\""
        };

        private string GetStringContents()
        {
            var str = new StringBuilder();

            while (!Source.IsExhausted)
            {
                var peekedLiteral = Literals.TryGetValueOrDefault(Peek);

                switch (Current)
                {
                    case '"':
                        Source.Advance();
                        return str.ToString();
                    case '\\' when peekedLiteral == null && Text.IsDigit(Peek):
                        Source.Advance();
                        var (tokenType, number) = GetNumberContents();
                        if (tokenType != TokenType.IntegerValue)
                        {
                            // TODO: error
                        }

                        try
                        {
                            int numberValue = short.Parse(number);
                            str.Append(Convert.ToChar(numberValue));
                        }
                        catch
                        {
                            /*ErrorService.Add(
                                ErrorType.SyntaxError,
                                Token.Of(
                                    TokenType.Unknown,
                                    SourceInfo.Of(TokenRange($"{Current}"), TokenLineRange($"{Current}"))),
                                $"unknown special character \\{number}"
                            );*/
                        }

                        break;
                    case '\\' when peekedLiteral == null:
                        /*ErrorService.Add(
                            ErrorType.SyntaxError,
                            Token.Of(
                                TokenType.Unknown,
                                SourceInfo.Of(TokenRange($"{Current}"), TokenLineRange($"{Current}"))),
                            $"unknown special character \\{Peek}"
                        );*/
                        Source.Advance();
                        break;
                    case '\\':
                        str.Append(peekedLiteral);
                        Source.Advance(2);
                        break;
                    default:
                        str.Append(Current);
                        Source.Advance();
                        break;
                }
            }

            /*ErrorService.Add(
                ErrorType.UnterminatedStringTerminal,
                Token.Of(
                    TokenType.Unknown,
                    SourceInfo.Of(TokenRange($"{Current}"), TokenLineRange($"{Current}"))),
                $"unterminated string terminal",
                true);*/
            return "";
        }

        private string GetAtom()
        {
            var kw = new StringBuilder("");

            while (!Source.IsExhausted && " \t\n\r".IndexOf(Current) < 0 && (char.IsLetter(Current) ||
                                                                             char.IsDigit(Current) || Current == '_'))
            {
                kw.Append(Current);
                Source.Advance();
            }

            return kw.ToString();
        }
    }
}