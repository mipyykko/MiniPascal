namespace Common.Errors
{
    public class Error
    {
        public ErrorType ErrorType;
        public readonly string Message;
        public readonly Token Token;

        private Error(ErrorType type, Token token, string message)
        {
            ErrorType = type;
            Token = token;
            Message = message;
        }

        public static Error Of(ErrorType type, string message) => new Error(type, null, message);
        public static Error Of(ErrorType type, Token token, string message) => new Error(type, token, message);

        public override string ToString()
        {
            return Message;
        }

    }
}