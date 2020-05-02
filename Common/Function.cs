using Common.Symbols;

namespace Common
{
    public class Function
    {
        public FunctionVariable Variable;
        public Scope Scope;

        public override string ToString()
        {
            return $"Function {Scope}: {Variable}";
        }
    }
}