using Common.Errors;

namespace Common
{
    public static class Context
    {
        public static Text Source { get; set; }
        public static IErrorService ErrorService { get; set; }
    }
}