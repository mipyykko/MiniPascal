using Common;

namespace Common.Errors
{
    public interface IErrorService
    {
        public bool Add(ErrorType type, Token token, string message, bool critical = false);
        public void Throw();
        public bool HasErrors();
    }
}