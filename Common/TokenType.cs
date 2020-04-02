namespace Common
{
    public enum TokenType
    {
        Unknown,
        Keyword,
        Identifier,
        EOF,
        Separator,
        OpenParen,
        CloseParen,
        OpenBlock,
        CloseBlock,
        Quote,
        Assignment,
        Comma,
        Dot,
        Colon,
        Operator,
        Number,
        IntegerValue,
        RealValue,
        BooleanValue,
        StringValue
    }
}