using System;
using System.Collections.Generic;
using System.Net.Mime;
using Common;

namespace Common.Errors
{
    public class ErrorService : IErrorService
    {
        private static Text Source => Context.Source;
        private static List<Error> _errors = new List<Error>();

        public bool Add(ErrorType type, Token token, string message, bool critical = false)
        {
            _errors.Add(Error.Of(
                type,
                token,
                message
            ));

            if (critical)
            {
                Throw();
            }
            return true;
        }

        public void Throw()
        {
            if (!HasErrors()) return;
            
            Console.WriteLine($"\nErrors:\n=======");
            foreach (var error in _errors)
            {
                Console.WriteLine();
                var errorLine = error.Token.SourceInfo.LineRange.Line;
                Console.WriteLine($"{error.Message} on line {errorLine}:");
                for (var i = Math.Max(0, errorLine - 2); i < Math.Min(Source.Lines.Count, errorLine + 3); i++)
                {
                    Console.WriteLine($"{i}: {Source.Lines[i]}");
                }

            }
            Environment.Exit(1);
        }
        
        public bool HasErrors() => _errors.Count > 0;
    }
}