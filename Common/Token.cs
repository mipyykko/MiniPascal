using System.Collections.Generic;

namespace Common
{
    public class Token
    {
        public static readonly Dictionary<string, TokenType> TrivialTokenTypes = new Dictionary<string, TokenType>
        {
            [":="] = TokenType.Assignment,
            ["."] = TokenType.Dot,
            [";"] = TokenType.Separator,
            [","] = TokenType.Comma,
            ["("] = TokenType.OpenParen,
            [")"] = TokenType.CloseParen,
            ["["] = TokenType.OpenBlock,
            ["]"] = TokenType.CloseBlock,
            ["\""] = TokenType.Quote,
            [":"] = TokenType.Colon,
            ["="] = TokenType.Operator,
            ["<>"] = TokenType.Operator,
            ["<="] = TokenType.Operator,
            ["<"] = TokenType.Operator,
            [">="] = TokenType.Operator,
            [">"] = TokenType.Operator,
            ["+"] = TokenType.Operator,
            ["-"] = TokenType.Operator,
            ["*"] = TokenType.Operator,
            ["/"] = TokenType.Operator,
            ["or"] = TokenType.Operator,
            ["and"] = TokenType.Operator,
            ["%"] = TokenType.Operator
        };
        
        public static readonly Dictionary<string, KeywordType> KeywordTypes = new Dictionary<string, KeywordType>
        {
            ["and"] = KeywordType.And,
            ["or"] = KeywordType.Or,
            ["not"] = KeywordType.Not,
            ["if"] = KeywordType.If,
            ["then"] = KeywordType.Then,
            ["else"] = KeywordType.Else,
            ["of"] = KeywordType.Of,
            ["while"] = KeywordType.While,
            ["do"] = KeywordType.Do,
            ["begin"] = KeywordType.Begin,
            ["end"] = KeywordType.End,
            ["var"] = KeywordType.Var,
            ["array"] = KeywordType.Array,
            ["procedure"] = KeywordType.Procedure,
            ["function"] = KeywordType.Function,
            ["program"] = KeywordType.Program,
            ["assert"] = KeywordType.Assert,
            ["return"] = KeywordType.Return
        };

        public static PrimitiveType TokenToPrimitiveType(TokenType tt) =>
            tt switch
            {
                TokenType.IntegerValue => PrimitiveType.Integer,
                TokenType.RealValue => PrimitiveType.Real,
                TokenType.StringValue => PrimitiveType.String,
                TokenType.BooleanValue => PrimitiveType.Boolean,
                _ => PrimitiveType.Void
        };

        public static PrimitiveType TokenContentToPrimitiveType(string s) =>
            s switch
            {
                "integer" => PrimitiveType.Integer,
                "real" => PrimitiveType.Real,
                "string" => PrimitiveType.String,
                "boolean" => PrimitiveType.Boolean,
                _ => PrimitiveType.Void
            };
        
        public TokenType Type { get; private set; }
        public KeywordType KeywordType { get; private set; }
        public string Content { get; private set; }
        public SourceInfo SourceInfo { get; private set; }

        public Token(TokenType type, KeywordType kw, string content, SourceInfo sourceInfo)
        {
            Type = type;
            KeywordType = kw;
            Content = content;
            SourceInfo = sourceInfo;
        }

        public static Token Of(TokenType type, KeywordType kw, string content, SourceInfo sourceInfo)
        {
            return new Token(type, kw, content, sourceInfo);
        }

        public static Token Of(TokenType type, SourceInfo sourceInfo)
        {
            return Of(type, KeywordType.Unknown, "", sourceInfo);
        }

        public static Token Of(TokenType type, string content, SourceInfo sourceInfo)
        {
            return Of(type, KeywordType.Unknown, content, sourceInfo);
        }

        public override string ToString()
        {
            return $"{Type} {SourceInfo.SourceRange} {SourceInfo.LineRange} {KeywordType} \"{Content}\"";
        }
    }
}